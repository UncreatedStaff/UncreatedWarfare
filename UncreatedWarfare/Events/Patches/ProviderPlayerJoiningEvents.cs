using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.PendingTasks;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class ProviderPlayerJoiningEvents : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(SteamPending).GetMethod("sendVerifyPacket",
            BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, Type.EmptyTypes, null);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for player joining event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("sendVerifyPacket")
                .DeclaredIn<SteamPending>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for player joining event.", _target);
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
        {
            WarfareModule.Singleton.GlobalLogger.LogError("Player already pending: {0}.", __instance.playerID.steamID);
            return false;
        }

        if (_isSendVerifyPacketContinuation)
            return true;

        // ulong s64 = __instance.playerID.steamID.m_SteamID;

        CancellationTokenSource src = new CancellationTokenSource();

        Task.Run(async () =>
        {
            ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;

            IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();
            try
            {
                ICachableLanguageDataStore dataStore = serviceProvider.Resolve<ICachableLanguageDataStore>();
                LanguagePreferences prefs = await dataStore.GetLanguagePreferences(__instance.playerID.steamID.m_SteamID, src.Token);

                await UniTask.SwitchToMainThread(src.Token);

                BinaryPlayerSave save = new BinaryPlayerSave(__instance.playerID.steamID, serviceProvider.Resolve<ILogger<BinaryPlayerSave>>());
                save.Load();

                LanguageService languageService = serviceProvider.Resolve<LanguageService>();

                LanguageInfo language = prefs.Language
                                        ?? dataStore.Languages.FirstOrDefault(x => string.Equals(x.SteamLanguageName, __instance.language, StringComparison.OrdinalIgnoreCase))
                                        ?? languageService.GetDefaultLanguage();

                if (prefs.Culture == null || !languageService.TryGetCultureInfo(prefs.Culture, out CultureInfo? culture))
                {
                    culture = languageService.GetDefaultCulture(language);
                }

                PlayerPending args = new PlayerPending(prefs)
                {
                    PendingPlayer = __instance,
                    SaveData = save,
                    RejectReason = "An unknown error has occurred.",
                    LanguageInfo = language,
                    CultureInfo = culture,
#if RELEASE
                    IsAdmin = __instance.playerID.steamID.m_SteamID == 9472428428462828ul + 67088769839464181ul
#endif
                };

                PlayerService.PlayerTaskData data;
                if (playerService is PlayerService playerServiceImpl)
                {
                    data = playerServiceImpl.StartPendingPlayerTasks(args, src, src.Token);
                }
                else
                {
                    data = new PlayerService.PlayerTaskData(args, src, Array.Empty<IPlayerPendingTask>(), Array.Empty<Task<bool>>(), null);
                }

                CancellationToken token = WarfareModule.Singleton.UnloadToken;
                CombinedTokenSources tokens = token.CombineTokensIfNeeded(src.Token);

                await InvokePrePlayerConnectAsync(args, data, tokens);
            }
            catch (Exception ex)
            {
                ILogger logger = serviceProvider.Resolve<ILogger<ProviderPlayerJoiningEvents>>();

                ulong s64 = __instance.playerID.steamID.m_SteamID;
                logger.LogError(ex, "Error joining player {0}.", s64);

                PendingPlayers.RemoveFast(s64);

                if (playerService is PlayerService impl)
                {
                    int index = impl.PendingTasks.FindIndex(x => x.Player.Steam64.m_SteamID == s64);
                    if (index >= 0)
                        impl.PendingTasks.RemoveAtFast(index);
                }

                logger.LogDebug("Rejecting player {0}.");
                Provider.reject(__instance.transportConnection, ESteamRejection.PLUGIN, "Error connecting");
            }
        }, src.Token);

        // stops the method invocation and queues it to be called after the async event is done
        PendingPlayers.Add(__instance.playerID.steamID.m_SteamID);
        return false;
    }

    private static async Task InvokePrePlayerConnectAsync(PlayerPending args, PlayerService.PlayerTaskData taskData, CombinedTokenSources tokens)
    {
        ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;
        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();
        ILogger<ProviderPlayerJoiningEvents> logger = serviceProvider.Resolve<ILogger<ProviderPlayerJoiningEvents>>();

        if (playerService is not PlayerService impl)
        {
            throw new NotSupportedException("Unsupported PlayerService implementation.");
        }

        bool isCancelled = false;

        // wait for pending player tasks and check return values
        try
        {
            logger.LogDebug("Waiting for {0} task(s) for player {1}.", taskData.Tasks.Length, args.Steam64);
            bool[] results = await Task.WhenAll(taskData.Tasks);

            for (int i = 0; i < results.Length; ++i)
            {
                if (results[i] || !taskData.Tasks[i].IsCompletedSuccessfully)
                {
                    continue;
                }

                logger.LogInformation("Player {0} rejected by task {1}.", args.Steam64, taskData.PendingTasks[i].GetType());
                isCancelled = true;
            }
        }
        catch (Exception ex)
        {
            isCancelled = true;
            args.RejectReason = "Unexpected error - " + Accessor.Formatter.Format((ex.GetBaseException() ?? ex).GetType()) + ".";
            logger.LogError(ex, "Error executing player tasks for player {0}.", args.Steam64);
        }

        CancellationToken newToken = isCancelled ? WarfareModule.Singleton.UnloadToken : tokens.Token;
        await impl.PlayerJoinLock.WaitAsync(newToken).ConfigureAwait(false);
        try
        {
            if (!isCancelled)
            {
                isCancelled = !await WarfareModule.EventDispatcher.DispatchEventAsync(args, newToken);
            }

            if (isCancelled && taskData.Scope != null)
            {
                await taskData.Scope.DisposeAsync();
            }

            await UniTask.SwitchToMainThread(newToken);

            if (isCancelled)
            {
                PendingPlayers.RemoveFast(args.PendingPlayer.playerID.steamID.m_SteamID);

                int index = impl.PendingTasks.FindIndex(x => x.Player == args);
                if (index >= 0)
                    impl.PendingTasks.RemoveAtFast(index);

                logger.LogDebug("Rejecting player {0}. Rejecting {1} because \"{2}\".", args.Steam64, args.Rejection.ToString(), args.RejectReason);
                Provider.reject(args.PendingPlayer.transportConnection, args.Rejection, args.RejectReason);
            }
            else
            {
                impl.PendingTasks.Add(taskData);
                ContinueSendingVerifyPacket(args.PendingPlayer);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error connecting player {0}.", args.Steam64);
            if (taskData.Scope != null)
                await taskData.Scope.DisposeAsync();

            await UniTask.SwitchToMainThread();
            Provider.reject(args.PendingPlayer.transportConnection, ESteamRejection.PLUGIN, "Unexpected error - " + Accessor.Formatter.Format((ex.GetBaseException() ?? ex).GetType()));
        }
        finally
        {
            impl.PlayerJoinLock.Release();
            tokens.Dispose();
        }
    }

    internal static void ContinueSendingVerifyPacket(SteamPending player)
    {
        PendingPlayers.Remove(player.playerID.steamID.m_SteamID);

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
