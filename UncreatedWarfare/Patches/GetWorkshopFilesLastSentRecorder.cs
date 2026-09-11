using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using SDG.NetPak;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Patches;

/// <summary>
/// Clients keep a 60-second workshop item cache for each server so they don't spam workshop requests.
/// This keeps track of when the last one was sent so we don't have to wait a full 60 seconds to change the map.
/// </summary>
[UsedImplicitly]
internal sealed class GetWorkshopFilesLastSentRecorder : IHarmonyPatch
{
    internal static bool WasPatchSuccessful;
    internal static Queue<WorkshopInfoSent> LatestSentWorkshopInfo = new Queue<WorkshopInfoSent>();

    internal static TimeSpan GetTimeSinceOnlinePlayerQueried(IPlayerService playerService)
    {
        GameThread.AssertCurrent();

        if (playerService.IsEmpty)
            return TimeSpan.FromSeconds(MapSwitchService.WorkshopCacheInvalidateSeconds);

        if (!WasPatchSuccessful)
        {
            // fallback to using player join time if the patch fails somehow
            WarfarePlayer latestJoiner = playerService.OnlinePlayers.Aggregate((a, next) => a.JoinTime > next.JoinTime ? a : next);
            return DateTime.UtcNow - latestJoiner.JoinTime;
        }

        WorkshopInfoSent? latest = null;

        // Queues enumerate from head to tail, get the last relevant one
        foreach (WorkshopInfoSent item in LatestSentWorkshopInfo)
        {
            // player is no longer online, ignore
            if (item.Steam64.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            {
                if (!playerService.IsPlayerOnline(item.Steam64))
                    continue;
            }
            else
            {
                if (!playerService.OnlinePlayers.Any(x => x.Connection.Equals(item.Connection)))
                    continue;
            }

            latest = item;
        }

        if (latest == null)
        {
            return TimeSpan.FromSeconds(MapSwitchService.WorkshopCacheInvalidateSeconds);
        }

        return DateTime.UtcNow.AddSeconds(-MapSwitchService.WorkshopCachePaddingSeconds) - latest.TimeSentUtc;
    }

    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        WasPatchSuccessful = false;
        Type? type = typeof(Provider).Assembly.GetType("SDG.Unturned.ServerMessageHandler_GetWorkshopFiles", throwOnError: false, ignoreCase: false);
        if (type == null)
        {
            logger.LogError("Failed to find type: {0}.", "SDG.Unturned.ServerMessageHandler_GetWorkshopFiles");
            return;
        }

        _target = type.GetMethod("ReadMessage", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            WasPatchSuccessful = true;
            try
            {
                patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            }
            catch
            {
                WasPatchSuccessful = false;
                throw;
            }

            logger.LogDebug("Patched {0} for tracking last workshop message sent.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("ReadMessage")
                .DeclaredIn(type, isStatic: true)
                .WithParameter<ITransportConnection>("transportConnection")
                .WithParameter<NetPakReader>("reader")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        WasPatchSuccessful = false;
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for tracking last workshop message sent.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? sendMessageToClient = typeof(NetMessages).GetMethod(
            "SendMessageToClient",
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [ typeof(EClientMessage), typeof(ENetReliability), typeof(ITransportConnection), typeof(NetMessages.ClientWriteHandler) ],
            null
        );

        if (sendMessageToClient == null)
        {
            return ctx.Fail(new MethodDefinition("SendMessageToClient")
                .DeclaredIn(typeof(NetMessages), isStatic: true)
                .WithParameter<EClientMessage>("index")
                .WithParameter<ENetReliability>("reliability")
                .WithParameter<ITransportConnection>("transportConnection")
                .WithParameter<NetMessages.ClientWriteHandler>("callback")
                .ReturningVoid()
            );
        }

        int secondLastLdc = -1, lastLdc = -1;
        int ldcCt = 0;

        bool found = false;

        while (ctx.MoveNext())
        {
            if (PatchUtil.TryGetLdcI4Value(ctx.Instruction, out int ldValue))
            {
                if (ldcCt == 0)
                {
                    lastLdc = ldValue;
                    ldcCt = 1;
                }
                else
                {
                    secondLastLdc = lastLdc;
                    lastLdc = ldValue;
                    ldcCt = 2;
                }

                continue;
            }

            if (!ctx.Instruction.Calls(sendMessageToClient) || secondLastLdc != (int)EClientMessage.DownloadWorkshopFiles)
            {
                continue;
            }

            found = true;
            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(method.IsStatic ? (ushort)0 : (ushort)1)
                    .Invoke(OnSentDownloadWorkshopFilesMessageMethod);
            });

            ctx.LogDebug($"Inserted {nameof(OnSentDownloadWorkshopFilesMessage)} call.");
        }

        if (!found)
        {
            WasPatchSuccessful = false;
            return ctx.Fail("SendMessageToClient call not found in method.");
        }

        return ctx;
    }

    private static readonly MethodInfo OnSentDownloadWorkshopFilesMessageMethod = typeof(GetWorkshopFilesLastSentRecorder)
        .GetMethod(nameof(OnSentDownloadWorkshopFilesMessage), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingMethodException($"{nameof(OnSentDownloadWorkshopFilesMessage)} not found.");

    [UsedImplicitly]
    private static void OnSentDownloadWorkshopFilesMessage(ITransportConnection toPlayer)
    {
        CSteamID steamId;
        if (!toPlayer.TryGetSteamId(out ulong steam64))
        {
            steamId = CSteamID.Nil;
            WarfareModule.Singleton.GlobalLogger.LogConditional($"Sent workshop info to {toPlayer}.");
        }
        else
        {
            steamId = new CSteamID(steam64);
            WarfareModule.Singleton.GlobalLogger.LogConditional($"Sent workshop info to {steamId}.");
        }

        DateTime now = DateTime.UtcNow;
        WorkshopInfoSent sent = new WorkshopInfoSent
        {
            TimeSentUtc = now,
            Connection = toPlayer,
            Steam64 = steamId
        };

        const float maxCacheTime = MapSwitchService.WorkshopCacheInvalidateSeconds + MapSwitchService.WorkshopCachePaddingSeconds;

        // remove info older than the cache time
        while (LatestSentWorkshopInfo.TryPeek(out WorkshopInfoSent oldest) && (now - oldest.TimeSentUtc).TotalSeconds >= maxCacheTime)
        {
            _ = LatestSentWorkshopInfo.Dequeue();
        }

        LatestSentWorkshopInfo.Enqueue(sent);
    }

    internal class WorkshopInfoSent
    {
        public required DateTime TimeSentUtc;
        public required CSteamID Steam64;
        public required ITransportConnection Connection;
    }
}