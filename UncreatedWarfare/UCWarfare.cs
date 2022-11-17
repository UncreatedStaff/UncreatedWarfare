#define USE_DEBUGGER
using JetBrains.Annotations;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public class UCWarfare : MonoBehaviour
{
    public static readonly TimeSpan RestartTime = new TimeSpan(1, 00, 0); // 9:00 PM EST
    public static readonly Version Version = new Version(2, 7, 1, 1);
    private readonly SystemConfig _config = new SystemConfig();
    public static UCWarfare I;
    internal static UCWarfareNexus Nexus;
    public Coroutine? StatsRoutine;
    public UCAnnouncer Announcer;
    internal DebugComponent Debugger;
    public event EventHandler? UCWarfareLoaded;
    public event EventHandler? UCWarfareUnloading;
    internal Projectiles.ProjectileSolver Solver;
    public HomebaseClientComponent? NetClient;
    public bool CoroutineTiming = false;
    private DateTime _nextRestartTime;
    public static int Season => Version.Major;
    public static bool IsLoaded => I is not null;
    public static SystemConfigData Config => I is null ? throw new SingletonUnloadedException(typeof(UCWarfare)) : I._config.Data;
    public static bool CanUseNetCall => IsLoaded && Config.TCPSettings.EnableTCPServer && I.NetClient != null && I.NetClient.IsActive;
    [UsedImplicitly]
    private void Awake()
    {
        if (I != null) throw new SingletonLoadException(ESingletonLoadType.LOAD, null, new Exception("Uncreated Warfare is already loaded."));
        I = this;
    }
    [UsedImplicitly]
    private void Start() => EarlyLoad();
    private void EarlyLoad()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Started loading - Uncreated Warfare version " + Version + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
              "Please stop using this plugin now.", ConsoleColor.Green);

        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        Logging.ExecuteOnMainThread = RunOnMainThread;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

        L.Log("Registering Commands: ", ConsoleColor.Magenta);

        OffenseManager.Init();

        CommandHandler.LoadCommands();

        DateTime loadTime = DateTime.Now;
        if (loadTime.TimeOfDay > RestartTime - TimeSpan.FromHours(2)) // don't restart if the restart would be in less than 2 hours
            _nextRestartTime = loadTime.Date + RestartTime + TimeSpan.FromDays(1);
        else
            _nextRestartTime = loadTime.Date + RestartTime;
        L.Log("Restart scheduled at " + _nextRestartTime.ToString("g"), ConsoleColor.Magenta);
        float seconds = (float)(_nextRestartTime - DateTime.Now).TotalSeconds;

        StartCoroutine(RestartIn(seconds));

        new PermissionSaver();

        TeamManager.SetupConfig();

        /* LOAD LOCALIZATION ASSETS */
        L.Log("Loading Localization and Color Data...", ConsoleColor.Magenta);
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Deaths.Localization.Reload();
        Data.Languages = JSONMethods.LoadLanguagePreferences();
        Data.LanguageAliases = JSONMethods.LoadLangAliases();
        Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
        Translation.ReadTranslations();

        CommandWindow.shouldLogDeaths = false;

        /* PATCHES */
        L.Log("Patching methods...", ConsoleColor.Magenta);
        try
        {
            Patches.DoPatching();
            LoadingQueueBlockerPatches.Patch();
        }
        catch (Exception ex)
        {
            L.LogError("Patching Error, perhaps Nelson changed something:");
            L.LogError(ex);
        }

        UCInventoryManager.OnLoad();

        if (Config.EnableSync)
            gameObject.AddComponent<ConfigSync>();
        gameObject.AddComponent<ActionLogger>();
        Debugger = gameObject.AddComponent<DebugComponent>();
        Data.Singletons = gameObject.AddComponent<SingletonManager>();


        if (Config.EnableSync)
            ConfigSync.Reflect();

        Data.RegisterInitialSyncs();

        InitNetClient();

        if (!Config.DisableDailyQuests)
            Quests.DailyQuests.EarlyLoad();

        ActionLogger.Add(EActionLogType.SERVER_STARTUP, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Data.Locale)}");
    }

    internal void InitNetClient()
    {
        if (NetClient != null)
        {
            Destroy(NetClient);
            NetClient = null;
        }
        if (Config.TCPSettings.EnableTCPServer)
        {
            L.Log("Attempting connection with Homebase...", ConsoleColor.Magenta);
            NetClient = gameObject.AddComponent<HomebaseClientComponent>();
            NetClient.OnClientVerified += Data.OnClientConnected;
            NetClient.OnClientDisconnected += Data.OnClientDisconnected;
            NetClient.OnSentMessage += Data.OnClientSentMessage;
            NetClient.OnReceivedMessage += Data.OnClientReceivedMessage;
            NetClient.ModifyVerifyPacketCallback += OnVerifyPacketMade;
            NetClient.Init(Config.TCPSettings.TCPServerIP, Config.TCPSettings.TCPServerPort, Config.TCPSettings.TCPServerIdentity);
        }
    }

    private void OnVerifyPacketMade(ref VerifyPacket packet)
    {
        packet = new VerifyPacket(packet.Identity, packet.SecretKey, packet.ApiVersion, packet.TimezoneOffset, Config.Currency, Config.RegionKey, Version);
    }
    public async Task LoadAsync()
    {
        await ToUpdate();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EventDispatcher.SubscribeToAll();

        Zone.OnLevelLoaded();

        try
        {
            /* DATA CONSTRUCTION */
            await Data.LoadVariables();
        }
        catch (Exception ex)
        {
            L.LogError("Startup error");
            L.LogError(ex);
            throw new SingletonLoadException(ESingletonLoadType.LOAD, null, ex);
        }
        await ToUpdate();

        /* START STATS COROUTINE */
        StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

        L.Log("Subscribing to events...", ConsoleColor.Magenta);
        SubscribeToEvents();

        F.CheckDir(Data.Paths.FlagStorage, out _, true);
        F.CheckDir(Data.Paths.StructureStorage, out _, true);
        F.CheckDir(Data.Paths.VehicleStorage, out _, true);
        ZonePlayerComponent.UIInit();

        Data.ZoneProvider.Reload();
        Data.ZoneProvider.Save();

        Solver = gameObject.AddComponent<Projectiles.ProjectileSolver>();

        Announcer = await Data.Singletons.LoadSingletonAsync<UCAnnouncer>();
        await ToUpdate();

        Data.ExtraPoints = JSONMethods.LoadExtraPoints();
        //L.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
        if (Data.Gamemode != null)
        {
            await Data.Gamemode.OnLevelReady();
            await ToUpdate();
        }

#if DEBUG
        if (Config.Debug && File.Exists(@"C:\orb.wav"))
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\orb.wav");
            player.Load();
            player.Play();
        }
#endif

        Debugger.Reset();

        UCPlayerData.ReloadToastIDs();

        /* BASIC CONFIGS */
        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
    }
    private IEnumerator<WaitForSecondsRealtime> RestartIn(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        ShutdownCommand.ShutdownAfterGameDaily();
    }
    private void SubscribeToEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Subscribe();
        StatsManager.LoadEvents();

        GameUpdateMonitor.OnGameUpdateDetected += EventFunctions.OnGameUpdateDetected;
        EventDispatcher.PlayerJoined += EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving += EventFunctions.OnPlayerDisconnected;
        Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
        ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
        BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
        UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
        UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning += EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned += EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned += EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
        Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
        Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
        Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
        Patches.OnPlayerGesture_Global += EventFunctions.OnPlayerGestureRequested;
        Patches.OnPlayerMarker_Global += EventFunctions.OnPlayerMarkedPosOnMap;
        DamageTool.damagePlayerRequested += EventFunctions.OnPlayerDamageRequested;
        BarricadeManager.onTransformRequested += EventFunctions.BarricadeMovedInWorkzone;
        BarricadeManager.onDamageBarricadeRequested += EventFunctions.OnBarricadeDamaged;
        StructureManager.onTransformRequested += EventFunctions.StructureMovedInWorkzone;
        StructureManager.onDamageStructureRequested += EventFunctions.OnStructureDamaged;
        BarricadeManager.onOpenStorageRequested += EventFunctions.OnEnterStorage;
        EventDispatcher.EnterVehicle += EventFunctions.OnEnterVehicle;
        EventDispatcher.VehicleSwapSeat += EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.ExitVehicle += EventFunctions.OnPlayerLeavesVehicle;
        EventDispatcher.LandmineExploding += EventFunctions.OnLandmineExploding;
        EventDispatcher.ItemDropRequested += EventFunctions.OnItemDropRequested;
        EventDispatcher.CraftRequested += EventFunctions.OnCraftRequested;
        VehicleManager.onDamageVehicleRequested += EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed += EventFunctions.OnConsume;
        EventDispatcher.BarricadeDestroyed += EventFunctions.OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed += EventFunctions.OnStructureDestroyed;
        PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice2;
    }
    private void UnsubscribeFromEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Unsubscribe();
        EventDispatcher.UnsubscribeFromAll();

        GameUpdateMonitor.OnGameUpdateDetected -= EventFunctions.OnGameUpdateDetected;
        ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
        EventDispatcher.PlayerJoined -= EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving -= EventFunctions.OnPlayerDisconnected;
        Provider.onCheckValidWithExplanation -= EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
        BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
        UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
        UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning -= EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned -= EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned -= EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
        Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
        Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
        Patches.OnPlayerTriedStoreItem_Global -= EventFunctions.OnTryStoreItem;
        Patches.OnPlayerGesture_Global -= EventFunctions.OnPlayerGestureRequested;
        Patches.OnPlayerMarker_Global -= EventFunctions.OnPlayerMarkedPosOnMap;
        DamageTool.damagePlayerRequested -= EventFunctions.OnPlayerDamageRequested;
        BarricadeManager.onTransformRequested -= EventFunctions.BarricadeMovedInWorkzone;
        BarricadeManager.onDamageBarricadeRequested -= EventFunctions.OnBarricadeDamaged;
        StructureManager.onTransformRequested -= EventFunctions.StructureMovedInWorkzone;
        BarricadeManager.onOpenStorageRequested -= EventFunctions.OnEnterStorage;
        StructureManager.onDamageStructureRequested -= EventFunctions.OnStructureDamaged;
        EventDispatcher.ItemDropRequested -= EventFunctions.OnItemDropRequested;
        EventDispatcher.LandmineExploding -= EventFunctions.OnLandmineExploding;
        EventDispatcher.EnterVehicle -= EventFunctions.OnEnterVehicle;
        EventDispatcher.VehicleSwapSeat -= EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.ExitVehicle -= EventFunctions.OnPlayerLeavesVehicle;
        VehicleManager.onDamageVehicleRequested -= EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop -= EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed -= EventFunctions.OnConsume;
        EventDispatcher.BarricadeDestroyed -= EventFunctions.OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed -= EventFunctions.OnStructureDestroyed;
        PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice2;
        StatsManager.UnloadEvents();
    }
    internal void UpdateLangs(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.OnLanguageChanged();
        EventDispatcher.InvokeUIRefreshRequest(player);
        UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(player);
        foreach (BarricadeRegion region in BarricadeManager.regions)
        {
            foreach (BarricadeDrop drop in region.drops)
            {
                if (drop.interactable is InteractableSign sign)
                {
                    if (VehicleSpawner.Loaded && VehicleSpawner.TryGetSpawnFromSign(sign, out Vehicles.VehicleSpawn spawn))
                        spawn.UpdateSign(player);
                    else if (sign.text.StartsWith(Signs.PREFIX))
                        Signs.BroadcastSignUpdate(drop);
                }
            }
        }
        if (ucplayer == null) return;
        if (Data.Is<TeamCTF>(out _))
        {
            CTFUI.SendFlagList(ucplayer);
        }
        else if (Data.Is<Invasion>(out _))
        {
            InvasionUI.SendFlagList(ucplayer);
        }
        else if (Data.Is<Insurgency>())
        {
            InsurgencyUI.SendCacheList(ucplayer);
        }
        if (Data.Is<ISquads>(out _))
        {
            if (ucplayer.Squad == null)
                SquadManager.SendSquadList(ucplayer);
            else
            {
                SquadManager.SendSquadMenu(ucplayer, ucplayer.Squad);
                SquadManager.UpdateMemberList(ucplayer.Squad);
                if (RallyManager.HasRally(ucplayer.Squad, out RallyPoint p))
                    p.ShowUIForPlayer(ucplayer);
            }
        }
        if (Data.Is<IFOBs>(out _))
            FOBManager.SendFOBList(ucplayer);
        if (Data.Gamemode.ShowXPUI)
            Points.UpdateXPUI(ucplayer);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
        {
            if (PlayerManager.OnlinePlayers[i].Player.TryGetComponent(out ZonePlayerComponent comp))
                comp.ReloadLang();
        }

        if (TraitManager.Loaded)
        {
            if (player.GetTeam() is 1 or 2 && !player.HasUIHidden && !(Data.Is(out IImplementsLeaderboard<BasePlayerStats, BaseStatTracker<BasePlayerStats>> lb) && lb.IsScreenUp))
                TraitManager.BuffUI.SendBuffs(player);
        }
    }

    private static Queue<MainThreadTask.MainThreadResult>? _threadActionRequests;
    private static Queue<LevelLoadTask.LevelLoadResult>? _levelLoadRequests;
    internal static Queue<MainThreadTask.MainThreadResult> ThreadActionRequests => _threadActionRequests ??= new Queue<MainThreadTask.MainThreadResult>(4);
    internal static Queue<LevelLoadTask.LevelLoadResult> LevelLoadRequests => _levelLoadRequests ??= new Queue<LevelLoadTask.LevelLoadResult>(4);
    public static MainThreadTask ToUpdate(CancellationToken token = default) => new MainThreadTask(false, token);
    public static MainThreadTask SkipFrame(CancellationToken token = default) => new MainThreadTask(true, token);
    public static LevelLoadTask ToLevelLoad(CancellationToken token = default) => new LevelLoadTask(token);
    public static bool IsMainThread => Thread.CurrentThread.IsGameThread();
    public static void RunOnMainThread(System.Action action) => RunOnMainThread(action, false, default);
    public static void RunOnMainThread(System.Action action, CancellationToken token) => RunOnMainThread(action, false, token);
    /// <param name="action">Method to be ran on the main thread in an update dequeue loop.</param>
    /// <param name="skipFrame">If this is called on the main thread it will queue it to be called next update or at the end of the current frame.</param>
    public static void RunOnMainThread(System.Action action, bool skipFrame, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (IsMainThread)
            action();
        else
        {
            MainThreadTask.MainThreadResult res = new MainThreadTask.MainThreadResult(new MainThreadTask(skipFrame, token));
            res.OnCompleted(action);
        }
    }
    /// <summary>Continues to run main thread operations in between spins so that calls to <see cref="ToUpdate"/> are not blocked.</summary>
    public static bool SpinWaitUntil(Func<bool> condition, int millisecondsTimeout = -1)
    {
        if (!IsMainThread)
            return SpinWait.SpinUntil(condition, millisecondsTimeout);

        uint stTime = 0;
        if (millisecondsTimeout != 0 && millisecondsTimeout != -1)
            stTime = (uint)Environment.TickCount;
        SpinWait spinWait = new SpinWait();
        while (!condition())
        {
            if (millisecondsTimeout == 0)
                return false;
            spinWait.SpinOnce();
            ProcessQueues();
            if (millisecondsTimeout != -1 && spinWait.NextSpinWillYield && millisecondsTimeout <= Environment.TickCount - stTime)
                return false;
        }
        return true;
    }
    [UsedImplicitly]
    private void Update()
    {
        ProcessQueues();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].Update();
    }
    private static void ProcessQueues()
    {
        if (_threadActionRequests != null)
        {
            while (_threadActionRequests.Count > 0)
            {
                MainThreadTask.MainThreadResult? res = null;
                try
                {
                    res = _threadActionRequests.Dequeue();
                    res.Task.Token.ThrowIfCancellationRequested();
                    res.continuation();
                }
                catch (OperationCanceledException) { L.LogDebug("Execution on update cancelled."); }
                catch (Exception ex)
                {
                    L.LogError("Error executing main thread operation.");
                    L.LogError(ex);
                }
                finally
                {
                    res?.Complete();
                }
            }
        }
        if (_levelLoadRequests != null && Level.isLoaded)
        {
            while (_levelLoadRequests.Count > 0)
            {
                LevelLoadTask.LevelLoadResult? res = null;
                try
                {
                    res = _levelLoadRequests.Dequeue();
                    res.Task.Token.ThrowIfCancellationRequested();
                    res.continuation();
                }
                catch (OperationCanceledException) { L.LogDebug("Execution on level load cancelled."); }
                catch (Exception ex)
                {
                    L.LogError("Error executing level load operation.");
                    L.LogError(ex);
                }
                finally
                {
                    res?.Complete();
                }
            }
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static void ForceUnload()
    {
        Nexus.UnloadNow();
        throw new SingletonUnloadedException(typeof(UCWarfare));
    }
    public async Task UnloadAsync()
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (StatsRoutine != null)
            {
                StopCoroutine(StatsRoutine);
                StatsRoutine = null;
            }
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);
            L.Log("Unloading Uncreated Warfare", ConsoleColor.Magenta);
            if (Data.Singletons is not null)
            {
                await Data.Singletons.UnloadSingletonAsync(Data.DeathTracker, false);
                Data.DeathTracker = null!;
                await Data.Singletons.UnloadSingletonAsync(Data.Gamemode);
                Data.Gamemode = null!;
                if (Announcer != null)
                {
                    await Data.Singletons.UnloadSingletonAsync(Announcer);
                    Announcer = null!;
                }

                await ToUpdate();
            }

            ThreadUtil.assertIsGameThread();
            if (Solver != null)
            {
                Destroy(Solver);
            }
            if (Maps.MapScheduler.Instance != null)
            {
                Destroy(Maps.MapScheduler.Instance);
                Maps.MapScheduler.Instance = null!;
            }

            if (Debugger != null)
                Destroy(Debugger);
            OffenseManager.Deinit();
            if (Data.DatabaseManager != null)
            {
                try
                {
                    await Data.DatabaseManager.CloseAsync();
                    Data.DatabaseManager.Dispose();
                }
                finally
                {
                    Data.DatabaseManager = null!;
                }
                await ToUpdate();
            }
            ThreadUtil.assertIsGameThread();
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            if (NetClient != null)
            {
                Destroy(NetClient);
                NetClient = null;
            }
            Logging.OnLogInfo -= L.NetLogInfo;
            Logging.OnLogWarning -= L.NetLogWarning;
            Logging.OnLogError -= L.NetLogError;
            Logging.OnLogException -= L.NetLogException;
            ConfigSync.UnpatchAll();
            try
            {
                LoadingQueueBlockerPatches.Unpatch();
                Patches.Unpatch();
            }
            catch (Exception ex)
            {
                L.LogError("Unpatching Error, perhaps Nelson changed something:");
                L.LogError(ex);
            }
            for (int i = 0; i < StatsManager.OnlinePlayers.Count; i++)
            {
                WarfareStats.IO.WriteTo(StatsManager.OnlinePlayers[i], StatsManager.StatsDirectory + StatsManager.OnlinePlayers[i].Steam64.ToString(Data.Locale) + ".dat");
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error unloading: ");
            L.LogError(ex);
        }
        if (Data.Singletons != null)
        {
            await Data.Singletons.UnloadAllAsync();
            await ToUpdate();
            ThreadUtil.assertIsGameThread();
        }
        L.Log("Warfare unload complete", ConsoleColor.Blue);
#if DEBUG
        profiler.Dispose();
        F.SaveProfilingData();
#endif
        await Task.Delay(1000);
    }
    public static Color GetColor(string key)
    {
        if (Data.Colors == null) return Color.white;
        if (Data.Colors.TryGetValue(key, out Color color)) return color;
        else if (Data.Colors.TryGetValue("default", out color)) return color;
        else return Color.white;
    }
    public static string GetColorHex(string key)
    {
        if (Data.ColorsHex == null) return @"ffffff";
        if (Data.ColorsHex.TryGetValue(key, out string color)) return color;
        else if (Data.ColorsHex.TryGetValue("default", out color)) return color;
        else return @"ffffff";
    }

    public static void ShutdownIn(string reason, ulong instigator, int seconds)
    {
        I.StartCoroutine(ShutdownIn2(reason, instigator, seconds));
    }
    public static void ShutdownNow(string reason, ulong instigator)
    {
        for (int i = 0; i < Provider.clients.Count; ++i)
            Provider.kick(Provider.clients[i].playerID.steamID, "Intentional Shutdown: " + reason);

        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay != null && bay.IsLoaded)
            bay.AbandonAllVehicles();

        if (CanUseNetCall)
        {
            ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
            I.StartCoroutine(ShutdownIn(reason, 4));
        }
        else
        {
            ShutdownInAwaitUnload(2, reason);
        }
    }
    private static IEnumerator<WaitForSeconds> ShutdownIn(string reason, float seconds)
    {
        yield return new WaitForSeconds(seconds / 2f);
        ShutdownInAwaitUnload(Mathf.RoundToInt(seconds / 2f), reason);
    }
    private static void ShutdownInAwaitUnload(int seconds, string reason)
    {
        Task.Run(async () =>
        {
            await ToUpdate();
            await Nexus.Unload();
            Provider.shutdown(seconds, reason);
        });
    }
    private static IEnumerator<WaitForSeconds> ShutdownIn2(string reason, ulong instigator, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
        yield return new WaitForSeconds(1f);
        ShutdownInAwaitUnload(2, reason);
    }
}

public class UCWarfareNexus : IModuleNexus
{
    public bool Loaded { get; private set; }

    void IModuleNexus.initialize()
    {
        CommandWindow.Log("Initializing UCWarfareNexus...");
        Thread.Sleep(1000);
        Data.LoadColoredConsole();
        Level.onPostLevelLoaded += OnLevelLoaded;
        UCWarfare.Nexus = this;
        GameObject go = new GameObject("UCWarfare " + UCWarfare.Version);
        go.AddComponent<Maps.MapScheduler>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<UCWarfare>();
    }

    private void Load()
    {
        Task.Run(LoadAsync);
    }

    private async Task LoadAsync()
    {
        try
        {
            await UCWarfare.I.LoadAsync().ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            Loaded = true;
        }
        catch (Exception ex)
        {
            if (UCWarfare.I != null)
                await UCWarfare.ToUpdate();
            L.LogError(ex);
            Loaded = false;
            if (UCWarfare.I != null)
            {
                try
                {
                    await UCWarfare.I.UnloadAsync().ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                }
                catch (Exception e)
                {
                    L.LogError("Unload error: ");
                    L.LogError(e);
                }

                UnityEngine.Object.Destroy(UCWarfare.I);
                UCWarfare.I = null!;
            }

            ShutdownCommand.ShutdownIn(10, "Uncreated Warfare failed to load: " + ex.GetType().Name);
            if (ex is SingletonLoadException)
                throw;
            else
                throw new SingletonLoadException(ESingletonLoadType.LOAD, null, ex);
        }
    }

    private IEnumerator Coroutine()
    {
        while (!Level.isLoaded)
            yield return null;
        Load();
    }

    private void OnLevelLoaded(int level)
    {
        if (level == Level.BUILD_INDEX_GAME)
        {
            UCWarfare.I.StartCoroutine(Coroutine());
        }
    }

    public void UnloadNow()
    {
        Task.Run(async () =>
        {
            await UCWarfare.ToUpdate();
            await Unload().ConfigureAwait(false);
            ShutdownCommand.ShutdownIn(10, "Uncreated Warfare unloading.");
        });
    }
    public async Task Unload()
    {
        try
        {
            await UCWarfare.I.UnloadAsync().ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (UCWarfare.I.gameObject != null)
            {
                UnityEngine.Object.Destroy(UCWarfare.I.gameObject);
            }
            UCWarfare.I = null!;
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            if (ex is SingletonLoadException)
                throw;
            else
                throw new SingletonLoadException(ESingletonLoadType.UNLOAD, null, ex);
        }
    }
    void IModuleNexus.shutdown()
    {
        Level.onPostLevelLoaded -= OnLevelLoaded;
        if (!UCWarfare.IsLoaded) return;
        Unload().Wait();
    }
}