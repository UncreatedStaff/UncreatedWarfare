#define USE_DEBUGGER
using JetBrains.Annotations;
using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SDG.Framework.Utilities;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public class UCWarfare : MonoBehaviour, IThreadQueueWaitOverride
{
    public static readonly TimeSpan RestartTime = new TimeSpan(1, 00, 0); // 9:00 PM EST
    public static readonly Version Version = new Version(3, 0, 1, 1);
    private readonly SystemConfig _config = UCWarfareNexus.Active ? new SystemConfig() : null!;
    private readonly List<UCTask> _tasks = UCWarfareNexus.Active ? new List<UCTask>(16) : null!;
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
    internal volatile bool ProcessTasks = true;
    private Task? _earlyLoadTask;
    private readonly CancellationTokenSource _unloadCancellationTokenSource = UCWarfareNexus.Active ? new CancellationTokenSource() : null!;
    public readonly SemaphoreSlim PlayerJoinLock = UCWarfareNexus.Active ? new SemaphoreSlim(0, 1) : null!;
    public float LastUpdateDetected = -1f;
    private static ConcurrentQueue<ThreadResult>? _threadRequests;
    internal static ConcurrentQueue<ThreadResult> ThreadQueueEntries => !IsLoaded ? null! : _threadRequests ??= new ConcurrentQueue<ThreadResult>();
    public bool FullyLoaded { get; private set; }
    public static CancellationToken UnloadCancel => IsLoaded ? I._unloadCancellationTokenSource.Token : CancellationToken.None;
    public static int Season => Version.Major;
    public static bool IsLoaded => I is not null;
    public static SystemConfigData Config => I is null ? throw new SingletonUnloadedException(typeof(UCWarfare)) : I._config.Data;
    public static bool CanUseNetCall => IsLoaded && Config.TCPSettings.EnableTCPServer && I.NetClient != null && I.NetClient.IsActive;
    [UsedImplicitly]
    private void Awake()
    {
        ThreadQueue.Queue = this;
        if (I != null) throw new SingletonLoadException(SingletonLoadType.Load, null, new Exception("Uncreated Warfare is already loaded."));
        I = this;
        FullyLoaded = false;
    }

    public static void SaveSystemConfig()
    {
        if (!IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));
        I._config.Save();
    }

    [UsedImplicitly]
    private void Start() => _earlyLoadTask = Task.Run( async () =>
    {
        try
        {
            await ToUpdate();
            await EarlyLoad(UnloadCancel).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            L.LogError("Error in early load!");
            L.LogError(ex);
            Provider.shutdown();
        }
    });
    private async Task EarlyLoad(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Started loading - Uncreated Warfare version " + Version + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
              "Please stop using this plugin now.", ConsoleColor.Green);

        L.IsBufferingLogs = true;

        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        Logging.ExecuteOnMainThread = RunOnMainThread;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

        L.Log("Registering Commands: ", ConsoleColor.Magenta);

        TeamManager.SetupConfig();

        OffenseManager.Init();
        bool set = false;
        FieldInfo? shouldLogBadMessagesField = typeof(Provider).Assembly.GetType("SDG.Unturned.NetMessages").GetField("shouldLogBadMessages", BindingFlags.Static | BindingFlags.NonPublic);
        if (shouldLogBadMessagesField != null && typeof(CommandLineFlag).IsAssignableFrom(shouldLogBadMessagesField.FieldType))
        {
            CommandLineFlag flag = (CommandLineFlag)shouldLogBadMessagesField.GetValue(null);
            if (flag != null)
            {
                flag.value = true;
                L.Log("Set command line flag: \"-LogBadMessages\".", ConsoleColor.Magenta);
                set = true;
            }
        }
        if (!set)
            L.LogWarning("Unable to set command line flag: \"-LogBadMessages\".");
        
        CommandHandler.LoadCommands();

        DateTime loadTime = DateTime.Now;
        if (!Config.DisableDailyRestart)
        {
            if (loadTime.TimeOfDay > RestartTime - TimeSpan.FromHours(2)) // don't restart if the restart would be in less than 2 hours
                _nextRestartTime = loadTime.Date + RestartTime + TimeSpan.FromDays(1);
            else
                _nextRestartTime = loadTime.Date + RestartTime;
            L.Log("Restart scheduled at " + _nextRestartTime.ToString("g"), ConsoleColor.Magenta);

            float seconds = (float)(_nextRestartTime - DateTime.Now).TotalSeconds;
            StartCoroutine(RestartIn(seconds));
        }
        else _nextRestartTime = DateTime.MaxValue;

        if (Config.EnableSync)
            gameObject.AddComponent<ConfigSync>();

        if (Config.EnableSync)
            ConfigSync.Reflect();

        Data.RegisterInitialSyncs();

        new PermissionSaver();
        await Data.LoadSQL(token).ConfigureAwait(false);
        await ItemIconProvider.DownloadConfig(token).ConfigureAwait(false);
        await TeamManager.ReloadFactions(token).ConfigureAwait(false);
        L.Log("Loading Moderation Data...", ConsoleColor.Magenta);
        Data.ModerationSql = new WarfareDatabaseInterface();
        await Data.ModerationSql.VerifyTables(token).ConfigureAwait(false);
        await ToUpdate(token);
        _ = TeamManager.Team1Faction;
        _ = TeamManager.Team2Faction;
        _ = TeamManager.AdminFaction;

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
        gameObject.AddComponent<ActionLog>();
        Debugger = gameObject.AddComponent<DebugComponent>();
        Data.Singletons = gameObject.AddComponent<SingletonManager>();

        InitNetClient();

        if (!Config.DisableDailyQuests)
            Quests.DailyQuests.EarlyLoad();

        ToastManager.Init();

        ActionLog.Add(ActionLogType.ServerStartup, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Data.AdminLocale)}");
    }
    internal void InitNetClient()
    {
        if (NetClient != null)
        {
            try
            {
                DestroyImmediate(NetClient);
                L.Log("Destroyed net client...", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                L.LogWarning("Error destroying net client.");
                L.LogError(ex);
                Destroy(NetClient);
            }
            finally
            {
                NetClient = null;
            }
        }
        if (Config.TCPSettings.EnableTCPServer)
        {
            NetClient = gameObject.AddComponent<HomebaseClientComponent>();
            NetClient.OnClientVerified += Data.OnClientConnected;
            NetClient.OnClientDisconnected += Data.OnClientDisconnected;
            NetClient.OnSentMessage += Data.OnClientSentMessage;
            NetClient.OnReceivedMessage += Data.OnClientReceivedMessage;
            NetClient.ModifyVerifyPacketCallback += OnVerifyPacketMade;
            NetClient.OnServerConfigRequested += Data.GetServerConfig;
            NetClient.Init(Config.TCPSettings.TCPServerIP, Config.TCPSettings.TCPServerPort, Config.TCPSettings.TCPServerIdentity);
            L.Log("Attempting connection with Homebase...", ConsoleColor.Magenta);
        }
    }
    private void OnVerifyPacketMade(ref VerifyPacket packet)
    {
        packet = new VerifyPacket(packet.Identity, packet.SecretKey, packet.ApiVersion, packet.TimezoneOffset, Config.Currency, Config.RegionKey, Version);
    }
    public async Task LoadAsync(CancellationToken token)
    {
        if (_earlyLoadTask != null && !_earlyLoadTask.IsCompleted)
        {
            await _earlyLoadTask.ConfigureAwait(false);
            await ToUpdate(token);
            _earlyLoadTask = null;
        }
        else await ToUpdate(token);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.SendUpdateServerConfig();
        EventDispatcher.SubscribeToAll();

        try
        {
            /* DATA CONSTRUCTION */
            await Data.LoadVariables(token);
        }
        catch (Exception ex)
        {
            L.LogError("Startup error");
            L.LogError(ex);
            throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
        }
        await ToUpdate(token);

        /* START STATS COROUTINE */
        StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

        L.Log("Subscribing to events...", ConsoleColor.Magenta);
        SubscribeToEvents();

        F.CheckDir(Data.Paths.FlagStorage, out _, true);
        F.CheckDir(Data.Paths.StructureStorage, out _, true);
        F.CheckDir(Data.Paths.VehicleStorage, out _, true);
        ZonePlayerComponent.UIInit();
            
        Solver = gameObject.AddComponent<Projectiles.ProjectileSolver>();

        Announcer = await Data.Singletons.LoadSingletonAsync<UCAnnouncer>(token: token);
        await KitSync.Init();
        await ToUpdate(token);

        Data.ExtraPoints = JSONMethods.LoadExtraPoints();
        //L.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
        if (Data.Gamemode != null)
        {
            await Data.Gamemode.OnLevelReady(token).ConfigureAwait(false);
            await ToUpdate(token);
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

        ToastManager.ReloadToastIds();

        /* BASIC CONFIGS */
        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        if (!Level.info.configData.Has_Global_Electricity)
        {
            L.LogWarning("Level does not have global electricity enabled, electrical grid effects will not work!");
            Data.UseElectricalGrid = false;
        }
        else Data.UseElectricalGrid = true;

        UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        FullyLoaded = true;
        PlayerJoinLock.Release();

        L.IsBufferingLogs = false;
        RunTask(async token =>
        {
            await Task.Delay(500, token).ConfigureAwait(false);
            await ToUpdate(token);
            L.FlushBadLogs();
        }, UnloadCancel);
    }
    private IEnumerator<WaitForSecondsRealtime> RestartIn(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (!Config.DisableDailyRestart)
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
    internal void UpdateLangs(UCPlayer player, bool uiOnly)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!uiOnly)
            player.OnLanguageChanged();

        EventDispatcher.InvokeUIRefreshRequest(player);
        if (!uiOnly) Signs.UpdateAllSigns(player);

        if (Data.Gamemode != null)
        {
            if (!uiOnly)
                Data.Gamemode.InvokeLanguageChanged(player);
        }

        Data.ShowAllUI(player);
    }

    public static MainThreadTask ToUpdate(CancellationToken token = default) => ThreadQueue.ToMainThread(false, token);
    public static MainThreadTask SkipFrame(CancellationToken token = default) => ThreadQueue.ToMainThread(true, token);
    public static LevelLoadTask ToLevelLoad(CancellationToken token = default) => new LevelLoadTask(token);

    // 'fire and forget' functions that will report errors once the task completes.

    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2, T3>(Func<T1, T2, T3, CancellationToken, Task> task, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, arg3, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2, T3>(Func<T1, T2, T3, Task> task, T1 arg1, T2 arg2, T3 arg3, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, arg3);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2>(Func<T1, T2, CancellationToken, Task> task, T1 arg1, T2 arg2, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T1, T2>(Func<T1, T2, Task> task, T1 arg1, T2 arg2, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, arg2);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T>(Func<T, CancellationToken, Task> task, T arg1, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1, token);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask<T>(Func<T, Task> task, T arg1, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(arg1);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Func<CancellationToken, Task> task, CancellationToken token = default, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task(default);
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Func<Task> task, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        Task t;
        try
        {
            L.LogDebug("Running task " + (ctx ?? member) + ".");
            t = task();
            RunTask(t, ctx, member, fp, awaitOnUnload, timeout);
        }
        catch (Exception e)
        {
            t = Task.FromException(e);
            if (string.IsNullOrEmpty(ctx))
                ctx = member;
            else
                ctx += " Member: " + member;
            RegisterErroredTask(t, ctx);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static void RunTask(Task task, string? ctx = null, [CallerMemberName] string member = "", [CallerFilePath] string fp = "", bool awaitOnUnload = false, int timeout = 180000)
    {
        if (!IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));

        member = fp + " :: " + member;

        if (string.IsNullOrEmpty(ctx))
            ctx = member;
        else
            ctx += " Member: " + member;
        if (task.IsCanceled)
        {
            L.LogDebug("Task cancelled: \"" + ctx + "\".");
            return;
        }
        if (task.IsFaulted)
        {
            RegisterErroredTask(task, ctx);
            return;
        }
        if (task.IsCompleted)
        {
            L.LogDebug("Task completed without awaiting: \"" + ctx + "\".");
            return;
        }
        L.LogDebug("Adding task \"" + ctx + "\".");
        I._tasks.Add(new UCTask(task, ctx, awaitOnUnload
#if DEBUG
            , timeout
#endif
        ));
    }
    private static void RegisterErroredTask(Task task, string? ctx)
    {
        AggregateException? ex = task.Exception;
        if (ex is null)
        {
            L.LogError("A registered task has failed without exception!" + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
        }
        else
        {
            if (ex.InnerExceptions.All(x => x is OperationCanceledException))
            {
                L.LogDebug("A registered task was cancelled." + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
                return;
            }
            L.LogError("A registered task has failed!" + (string.IsNullOrEmpty(ctx) ? string.Empty : (" Context: " + ctx)));
            L.LogError(ex);
        }
    }
    public static bool IsMainThread => Thread.CurrentThread.IsGameThread();
    bool IThreadQueue.IsMainThread => Thread.CurrentThread.IsGameThread();
    void IThreadQueue.RunOnMainThread(System.Action action) => RunOnMainThread(action, false, default);
    void IThreadQueue.RunOnMainThread(ThreadResult action) => ThreadQueueEntries.Enqueue(action);
    void IThreadQueueWaitOverride.SpinWaitUntil(Func<bool> condition, int millisecondsTimeout, CancellationToken token) => SpinWaitUntil(condition, millisecondsTimeout, token);
    public static bool RunOnMainThread(System.Action action) => RunOnMainThread(action, false, default);
    public static bool RunOnMainThread(System.Action action, CancellationToken token) => RunOnMainThread(action, false, token);

    /// <param name="skipFrame">If this is called on the main thread it will queue it to be called next update or at the end of the current frame.</param>
    public static bool RunOnMainThread(System.Action action, bool skipFrame, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (IsMainThread)
        {
            action();
            return false;
        }

        ThreadResult res = new MainThreadTask(skipFrame, token).GetAwaiter();
        res.OnCompleted(action);
        return true;
    }
    /// <summary>Continues to run main thread operations in between spins so that calls to <see cref="ToUpdate"/> are not blocked.</summary>
    public static bool SpinWaitUntil(Func<bool> condition, int millisecondsTimeout = -1, CancellationToken token = default)
    {
        if (!IsMainThread)
            return SpinWait.SpinUntil(condition, millisecondsTimeout);

        uint stTime = 0;
        if (millisecondsTimeout != 0 && millisecondsTimeout != -1)
            stTime = (uint)Environment.TickCount;
        SpinWait spinWait = new SpinWait();
        while (!condition())
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);
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
        if (LastUpdateDetected > 0 && (Provider.clients.Count == 0 || (Time.realtimeSinceStartup - LastUpdateDetected) > 3600))
            StartCoroutine(ShutdownIn("Unturned Update", 0f));
        
        ProcessQueues();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].Update();
        if (ProcessTasks)
        {
#if DEBUG
            DateTime now = _tasks.Count > 0 ? DateTime.UtcNow : default;
#endif
            for (int i = _tasks.Count - 1; i >= 0; --i)
            {
                UCTask task = _tasks[i];
#if DEBUG
                double sec = (now - task.StartTime).TotalSeconds;
#endif
                if (!task.Task.IsCompleted)
                {
#if DEBUG
                    if (task.TimeoutMs >= 0 && sec > task.TimeoutMs)
                    {
                        L.LogDebug($"Task not completed after a long time ({sec} seconds)." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
                    }
#endif
                    continue;
                }
                if (task.Task.IsCanceled)
                {
                    L.LogDebug("Task cancelled." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
                }
                else if (task.Task.IsFaulted)
                {
                    _tasks.RemoveAtFast(i);
                    RegisterErroredTask(task.Task, task.Context);
                    return;
                }
                if (task.Task.IsCompleted)
                {
                    _tasks.RemoveAtFast(i);
#if DEBUG
                    L.LogDebug("Task complete in " + sec.ToString("0.#", Data.AdminLocale) + " seconds." + (string.IsNullOrEmpty(task.Context) ? string.Empty : (" Context: " + task.Context)));
#endif
                    return;
                }
            }
        }
    }
    private static void ProcessQueues()
    {
        if (_threadRequests != null)
        {
            List<ThreadResult>? threads = null;
            while (_threadRequests.TryDequeue(out ThreadResult result))
            {
                try
                {
                    if (result.Condition != null && !result.Condition())
                    {
                        (threads ??= ListPool<ThreadResult>.claim()).Add(result);
                        continue;
                    }

                    ThreadQueue.FulfillThreadTask(result);
                }
                catch (OperationCanceledException)
                {
                    L.LogDebug("Execution on update cancelled.");
                }
                catch (Exception ex)
                {
                    L.LogError("Error executing queued thread operation.");
                    L.LogError(ex);
                }
            }
            if (threads != null)
                ListPool<ThreadResult>.release(threads);
        }
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static void ForceUnload()
    {
        Nexus.UnloadNow();
        throw new SingletonUnloadedException(typeof(UCWarfare));
    }
    internal async Task LetTasksUnload(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        while (_tasks.Count > 0)
        {
            UCTask task = _tasks[0];
#if DEBUG
            using IDisposable profiler2 = ProfilingUtils.StartTracking("Unloading task: " + (task.Context ?? "<no context>") + ".");
#endif
            if (task.AwaitOnUnload && !task.Task.IsCompleted)
            {
                L.LogDebug("Letting task \"" + (task.Context ?? "null") + "\" finish for up to 10 seconds before unloading...");
                try
                {
                    await Task.WhenAny(task.Task, Task.Delay(10000, token));
                }
                catch
                {
                    RegisterErroredTask(task.Task, task.Context);
                    goto cont;
                }
                if (!task.Task.IsCompleted)
                {
                    L.LogWarning("Task \"" + (task.Context ?? "null") + "\" did not complete after 10 seconds of waiting.");
                }
                else L.LogDebug("  ... Done");
            }
            cont:
            _tasks.RemoveAt(0);
        }
    }

    public async Task UnloadAsync(CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        IDisposable profiler = ProfilingUtils.StartTracking();
        IDisposable profiler2;
#endif
        FullyLoaded = false;
        try
        {
            ProcessTasks = false;
            if (StatsRoutine != null)
            {
                StopCoroutine(StatsRoutine);
                StatsRoutine = null;
            }
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            L.Log("Unloading Uncreated Warfare", ConsoleColor.Magenta);
            await LetTasksUnload(token).ConfigureAwait(false);
            if (Data.Singletons is not null)
            {
#if DEBUG
                profiler2 = ProfilingUtils.StartTracking("Unload DeathTracker");
#endif
                await Data.Singletons.UnloadSingletonAsync(Data.DeathTracker, false, token: token);
                Data.DeathTracker = null!;
#if DEBUG
                profiler2.Dispose();
                profiler2 = ProfilingUtils.StartTracking("Unload UCAnnouncer.");
#endif
                if (Announcer != null)
                {
                    await Data.Singletons.UnloadSingletonAsync(Announcer, token: token);
                    Announcer = null!;
                }
#if DEBUG
                profiler2.Dispose();
                profiler2 = ProfilingUtils.StartTracking("Unload Gamemode");
#endif
                if (Data.Gamemode != null)
                {
                    Data.Gamemode.IsPendingCancel = true;
                    await Data.Singletons.UnloadSingletonAsync(Data.Gamemode, token: token);
                    Data.Gamemode = null!;
                }
#if DEBUG
                profiler2.Dispose();
#endif
            }

            await LetTasksUnload(token).ConfigureAwait(false);
            await ToUpdate(token);

#if DEBUG
            profiler2 = ProfilingUtils.StartTracking("Destroy GameObjects");
#endif
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
#if DEBUG
            profiler2.Dispose();
#endif
            OffenseManager.Deinit();
#if DEBUG
            profiler2 = ProfilingUtils.StartTracking("Unload MySQL");
#endif
            if (Data.DatabaseManager != null)
            {
                CancellationToken token2 = new CancellationTokenSource(5000).Token;
                token2.CombineIfNeeded(token);
                try
                {
                    await Data.DatabaseManager.CloseAsync(token2);
                    Data.DatabaseManager.Dispose();
                }
                catch (OperationCanceledException) when (token2.IsCancellationRequested)
                {
                    L.LogWarning("Timed out closing local MySql connection.");
                }
                finally
                {
                    Data.DatabaseManager = null!;
                }
            }
            if (Data.RemoteSQL != null)
            {
                CancellationToken token2 = new CancellationTokenSource(5000).Token;
                token2.CombineIfNeeded(token);
                try
                {
                    await Data.RemoteSQL.CloseAsync(token2);
                    Data.RemoteSQL.Dispose();
                }
                catch (OperationCanceledException) when (token2.IsCancellationRequested)
                {
                    L.LogWarning("Timed out closing remote MySql connection.");
                }
                finally
                {
                    Data.RemoteSQL = null!;
                }
            }
#if DEBUG
            profiler2.Dispose();
#endif
            await LetTasksUnload(token).ConfigureAwait(false);
            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
#if DEBUG
            profiler2 = ProfilingUtils.StartTracking("Stopping Coroutines");
#endif
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
#if DEBUG
            profiler2.Dispose();
            profiler2 = ProfilingUtils.StartTracking("Unsubscribing from events.");
#endif
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
#if DEBUG
            profiler2.Dispose();
#endif
            CommandWindow.shouldLogDeaths = true;
#if DEBUG
            profiler2 = ProfilingUtils.StartTracking("Destroying NetClient.");
#endif
            if (NetClient != null)
            {
                Destroy(NetClient);
                NetClient = null;
            }
#if DEBUG
            profiler2.Dispose();
#endif
            Logging.OnLogInfo -= L.NetLogInfo;
            Logging.OnLogWarning -= L.NetLogWarning;
            Logging.OnLogError -= L.NetLogError;
            Logging.OnLogException -= L.NetLogException;
#if DEBUG
            profiler2 = ProfilingUtils.StartTracking("Harmony Cleanup.");
#endif
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
#if DEBUG
            profiler2.Dispose();
            profiler2 = ProfilingUtils.StartTracking("Saving Stats.");
#endif
            for (int i = 0; i < StatsManager.OnlinePlayers.Count; i++)
            {
                WarfareStats.IO.WriteTo(StatsManager.OnlinePlayers[i], StatsManager.StatsDirectory + StatsManager.OnlinePlayers[i].Steam64.ToString(Data.AdminLocale) + ".dat");
            }
#if DEBUG
            profiler2.Dispose();
#endif
        }
        catch (Exception ex)
        {
            L.LogError("Error unloading: ");
            L.LogError(ex);
        }

#if DEBUG
        profiler2 = ProfilingUtils.StartTracking("Unload remaining Singletons.");
#endif
        if (Data.Singletons != null)
        {
            await Data.Singletons.UnloadAllAsync(token);
            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }

        Data.NilSteamPlayer = null!;
#if DEBUG
        profiler2.Dispose();
#endif
        L.Log("Warfare unload complete", ConsoleColor.Blue);
#if DEBUG
        profiler.Dispose();
        F.SaveProfilingData();
#endif
        await Task.Delay(100, token);
    }
    public static Color GetColor(string key)
    {
        if (Data.Colors == null) return Color.white;
        if (Data.Colors.TryGetValue(key, out Color color)) return color;
        if (JSONMethods.DefaultColors.TryGetValue(key, out string color2)) return color2.Hex();
        if (Data.Colors.TryGetValue("default", out color)) return color;
        return Color.white;
    }
    public static string GetColorHex(string key)
    {
        if (Data.ColorsHex == null) return "ffffff";
        if (Data.ColorsHex.TryGetValue(key, out string color)) return color;
        if (JSONMethods.DefaultColors.TryGetValue(key, out color)) return color;
        if (Data.ColorsHex.TryGetValue("default", out color)) return color;
        return "ffffff";
    }
    public static void ShutdownIn(string reason, ulong instigator, int seconds)
    {
        I.StartCoroutine(ShutdownIn2(reason, instigator, seconds));
    }
    public static void ShutdownNow(string reason, ulong instigator)
    {
        for (int i = 0; i < Provider.clients.Count; ++i)
            Provider.kick(Provider.clients[i].playerID.steamID, "Intentional Shutdown: " + reason);

        VehicleSpawner? bay = Data.Singletons.GetSingleton<VehicleSpawner>();
        if (bay != null && bay.IsLoaded)
        {
            bay.AbandonAllVehicles(false);
        }

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
            Provider.shutdown(seconds + 60, reason);
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
    private readonly struct UCTask
    {
        public readonly Task Task;
        public readonly string? Context;
        public readonly bool AwaitOnUnload;
#if DEBUG
        public readonly int TimeoutMs;
        public readonly DateTime StartTime;
#endif
        public UCTask(Task task, string context, bool awaitOnUnload
#if DEBUG
            , int timeout
#endif
            )
        {
            Task = task;
            Context = context;
            AwaitOnUnload = awaitOnUnload;
#if DEBUG
            TimeoutMs = timeout;
            StartTime = DateTime.UtcNow;
#endif
        }
    }

    public static bool IsNerd(ulong s64)
    {
        return Config.Nerds != null && Config.Nerds.Contains(s64);
    }
}

public class UCWarfareNexus : IModuleNexus
{
    public static bool Active { get; private set; }
    public bool Loaded { get; private set; }

    void IModuleNexus.initialize()
    {
        CommandWindow.Log("Initializing UCWarfareNexus...");
        Active = true;
        try
        {
            L.Init();
        }
        catch (Exception ex)
        {
            Logging.LogException(ex);
        }

        L.Log("Initializing UniTask...", ConsoleColor.Magenta);
        PlayerLoopHelper.Init();

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
            await UCWarfare.I.LoadAsync(UCWarfare.UnloadCancel).ConfigureAwait(false);
            await UCWarfare.ToUpdate(UCWarfare.UnloadCancel);
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
                    await UCWarfare.I.UnloadAsync(CancellationToken.None).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(CancellationToken.None);
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
                throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
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
    public async Task Unload(bool shutdown = true)
    {
        try
        {
            await UCWarfare.I.UnloadAsync(CancellationToken.None).ConfigureAwait(false);
            await UCWarfare.ToUpdate(CancellationToken.None);
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
                throw new SingletonLoadException(SingletonLoadType.Unload, null, ex);
        }
        finally
        {
            if (shutdown)
            {
                Provider.shutdown(0);
            }
        }
    }
    void IModuleNexus.shutdown()
    {
        Level.onPostLevelLoaded -= OnLevelLoaded;
        if (!UCWarfare.IsLoaded) return;
        Unload(false).Wait(10000);
    }
}

[Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
internal sealed class OperationTestAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public float? ArgumentSingle { get; }
    public double? ArgumentDouble { get; }
    public decimal? ArgumentDecimal { get; }
    public long? ArgumentInt64 { get; }
    public ulong? ArgumentUInt64 { get; }
    public int? ArgumentInt32 { get; }
    public uint? ArgumentUInt32 { get; }
    public short? ArgumentInt16 { get; }
    public ushort? ArgumentUInt16 { get; }
    public sbyte? ArgumentInt8 { get; }
    public byte? ArgumentUInt8 { get; }
    public bool? ArgumentBoolean { get; }
    public string? ArgumentString { get; }
    public Type? ArgumentType { get; }
    public Type[]? IgnoreExceptions { get; set; }
    /// <summary>Just run it, check exceptions only.</summary>
    public OperationTestAttribute() { }
    public OperationTestAttribute(long arg) { ArgumentInt64 = arg; }
    public OperationTestAttribute(ulong arg) { ArgumentUInt64 = arg; }
    public OperationTestAttribute(int arg) { ArgumentInt32 = arg; }
    public OperationTestAttribute(uint arg) { ArgumentUInt32 = arg; }
    public OperationTestAttribute(short arg) { ArgumentInt16 = arg; }
    public OperationTestAttribute(ushort arg) { ArgumentUInt16 = arg; }
    public OperationTestAttribute(sbyte arg) { ArgumentInt8 = arg; }
    public OperationTestAttribute(byte arg) { ArgumentUInt8 = arg; }
    public OperationTestAttribute(bool arg) { ArgumentBoolean = arg; }
    public OperationTestAttribute(float arg) { ArgumentSingle = arg; }
    public OperationTestAttribute(double arg) { ArgumentDouble = arg; }
    public OperationTestAttribute(decimal arg) { ArgumentDecimal = arg; }
    public OperationTestAttribute(string arg) { ArgumentString = arg; }
    public OperationTestAttribute(Type arg) { ArgumentType = arg; }
}