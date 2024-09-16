using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management.Legacy;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class ProviderPlayerJoiningEvents : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(SteamPending).GetMethod("sendVerifyPacket",
            BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null);

        if (_target != null)
        {
            Patcher.Patch(_target, transpiler: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for player joining event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition("sendVerifyPacket")
                .DeclaredIn<SteamPending>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for player joining event.", Accessor.Formatter.Format(_target));
        _target = null;
    }


    private static readonly List<ulong> PendingPlayers = new List<ulong>(8);
    private static bool _isSendVerifyPacketContinuation;

    internal delegate void StartVerifying(SteamPending player, ref bool shouldDeferContinuation);

    //internal static ulong Accept = 0ul;

    // SDG.Provider.accept
    /// <summary>
    /// Allows us to defer accepting a player to check stuff with async calls.
    /// </summary>
    private static bool Prefix(SteamPending __instance)
    {
        ActionLog.Add(ActionLogType.TryConnect, $"Steam Name: {__instance.playerID.playerName}, Public Name: {__instance.playerID.characterName}, Private Name: {__instance.playerID.nickName}, Character ID: {__instance.playerID.characterID}.", __instance.playerID.steamID.m_SteamID);
        
        // this method could be recalled while the verify event is running if another player gets verified.
        if (PendingPlayers.Contains(__instance.playerID.steamID.m_SteamID))
            return false;

        if (_isSendVerifyPacketContinuation)
            return true;

        ulong s64 = __instance.playerID.steamID.m_SteamID;

        PendingAsyncData data = new PendingAsyncData(__instance);
        CancellationTokenSource? src = null;

        for (int i = 0; i < PlayerManager.PlayerConnectCancellationTokenSources.Count; ++i)
        {
            KeyValuePair<ulong, CancellationTokenSource> kvp = PlayerManager.PlayerConnectCancellationTokenSources[i];
            if (kvp.Key == s64)
            {
                src = kvp.Value;
                break;
            }
        }

        PlayerSave.TryReadSaveFile(s64, out PlayerSave? save);
        PlayerPending args = new PlayerPending
        {
            PendingPlayer = __instance,
            AsyncData = data,
            SaveData = save,
            RejectReason = "An unknown error has occurred.",
#if RELEASE
            IsAdmin = __instance.playerID.steamID.m_SteamID == 9472428428462828ul + 67088769839464181ul
#endif
        };

        CancellationToken token = WarfareModule.Singleton.UnloadToken;
        CombinedTokenSources tokens = token.CombineTokensIfNeeded(src?.Token ?? CancellationToken.None);

        Task task = InvokePrePlayerConnectAsync(args, tokens);
        if (task.IsCompleted)
        {
            return !args.IsActionCancelled;
        }

        // stops the method invocation and queues it to be called after the async event is done
        PendingPlayers.Add(__instance.playerID.steamID.m_SteamID);
        return false;
    }

    private static async Task InvokePrePlayerConnectAsync(PlayerPending args, CombinedTokenSources tokens)
    {
        await UCWarfare.I.PlayerJoinLock.WaitAsync(tokens.Token).ConfigureAwait(false);
        try
        {
            bool isCancelled = await WarfareModule.EventDispatcher.DispatchEventAsync(args, tokens.Token);

            await UniTask.SwitchToMainThread(tokens.Token);

            if (isCancelled)
            {
                PendingPlayers.Remove(args.PendingPlayer.playerID.steamID.m_SteamID);
                EventDispatcher2.PendingAsyncData.Remove(args.AsyncData);

                Provider.reject(args.PendingPlayer.transportConnection, args.Rejection, args.RejectReason);
            }
            else
            {
                EventDispatcher2.PendingAsyncData.Add(args.AsyncData);
                ContinueSendingVerifyPacket(args.PendingPlayer);
            }
        }
        finally
        {
            UCWarfare.I.PlayerJoinLock.Release();
            tokens.Dispose();
        }
    }

    internal static void ContinueSendingVerifyPacket(SteamPending player)
    {
        PendingPlayers.Remove(player.playerID.steamID.m_SteamID);

        // disconnected
        if (!player.transportConnection.TryGetIPv4Address(out _))
            return;

        _isSendVerifyPacketContinuation = true;
        try
        {
            player.sendVerifyPacket();
        }
        finally
        {
            _isSendVerifyPacketContinuation = false;
        }
    }
}
