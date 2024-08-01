#define USE_DEBUGGER
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.EntityFrameworkCore;
using SDG.Framework.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Players.Management.Legacy;
#if NETSTANDARD || NETFRAMEWORK
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking.Purchasing;
#endif

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public class UCWarfare : MonoBehaviour
{
    public static readonly TimeSpan RestartTime = new TimeSpan(6, 00, 0); // 2:00 AM EST
    public static readonly Version Version = new Version(3, 2, 6, 0);
    private readonly SystemConfig _config = UCWarfareNexus.Active ? new SystemConfig() : null!;
    public static UCWarfare I;
    internal static UCWarfareNexus Nexus;
    public UCAnnouncer Announcer;
    internal DebugComponent Debugger;
    public event EventHandler? UCWarfareLoaded;
    public event EventHandler? UCWarfareUnloading;
    internal Projectiles.ProjectileSolver Solver;
    public bool CoroutineTiming = false;
    private DateTime _nextRestartTime;
    internal volatile bool ProcessTasks = true;
    private Task? _earlyLoadTask;
    private readonly CancellationTokenSource _unloadCancellationTokenSource = UCWarfareNexus.Active ? new CancellationTokenSource() : null!;
    public readonly SemaphoreSlim PlayerJoinLock = UCWarfareNexus.Active ? new SemaphoreSlim(0, 1) : null!;
    public float LastUpdateDetected = -1f;
    public bool FullyLoaded { get; private set; }
    public static CancellationToken UnloadCancel => IsLoaded ? I._unloadCancellationTokenSource.Token : CancellationToken.None;
    public static int Season => Version.Major;
    public static bool IsLoaded => I is not null;
    public static SystemConfigData Config => I is null ? throw new SingletonUnloadedException(typeof(UCWarfare)) : I._config.Data;
    public static bool CanUseNetCall => false;
    [UsedImplicitly]
    private void Awake()
    {
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
        finally
        {
            _earlyLoadTask = null;
        }
    });
     
    private async Task EarlyLoad(CancellationToken token)
    {
        L.Log("Started loading - Uncreated Warfare version " + Version + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
              "Please stop using this plugin now.", ConsoleColor.Green);

        L.IsBufferingLogs = true;

        // adds the plugin to the server lobby screen and sets the plugin framework type to 'Unknown'.
        IPluginAdvertising pluginAdvService = PluginAdvertising.Get();

        pluginAdvService.AddPlugin("Uncreated Warfare");

        pluginAdvService
            .GetType()
            .GetProperty("PluginFrameworkTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
           ?.GetSetMethod(true)
           ?.Invoke(pluginAdvService, [ "uw" ]);

        Accessor.Logger = new L.UCLoggerFactory().CreateReflectionToolsLogger(disposeFactoryOnDispose: true);
#if DEBUG
        Accessor.LogDebugMessages = true;
#endif
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        Logging.ExecuteOnMainThread = RunOnMainThread;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), NetCallOrigin.ServerOnly);

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

        Data.RegisterInitialConfig();

        new PermissionSaver();
        await Data.LoadSQL(token).ConfigureAwait(false);

        L.Log("Migrating database changes...", ConsoleColor.Magenta);

        await using (IDbContext dbContext = new WarfareDbContext())
        {
            try
            {
                await dbContext.Database.MigrateAsync(token);
                L.Log(" + Done", ConsoleColor.Gray);
            }
            catch (Exception ex)
            {
                L.LogError(" + Failed to migrate databse.");
                L.LogError(ex);
                Provider.shutdown(10);
                return;
            }
        }

        Data.LanguageDataStore = new WarfareMySqlLanguageDataStore();
        await Data.ReloadLanguageDataStore(false, token).ConfigureAwait(false);
        await ItemIconProvider.DownloadConfig(token).ConfigureAwait(false);
        await TeamManager.ReloadFactions(token).ConfigureAwait(false);
        L.Log("Loading Moderation Data...", ConsoleColor.Magenta);
        Data.ModerationSql = new WarfareDatabaseInterface();
        await Data.ModerationSql.VerifyTables(token).ConfigureAwait(false);
        Data.ModerationSql.OnModerationEntryUpdated += OffenseManager.OnModerationEntryUpdated;
        Data.ModerationSql.OnNewModerationEntryAdded += OffenseManager.OnNewModerationEntryAdded;

#if NETSTANDARD || NETFRAMEWORK
        Data.WarfareStripeService = new WarfareStripeService();
        Data.PurchasingDataStore = new WarfarePurchaseRecordsInterface(); //await PurchaseRecordsInterface.Create<WarfarePurchaseRecordsInterface>(false, token).ConfigureAwait(false);
#endif


        await ToUpdate(token);
        _ = TeamManager.Team1Faction;
        _ = TeamManager.Team2Faction;
        _ = TeamManager.AdminFaction;

        /* LOAD LOCALIZATION ASSETS */
        L.Log("Loading Localization and Color Data...", ConsoleColor.Magenta);
        Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
        Deaths.DeathMessageResolver.Reload();
        Localization.ReadEnumTranslations(Data.TranslatableEnumTypes);
        await Translation.ReadTranslations(token).ConfigureAwait(false);
        await ToUpdate(token);
        L.Log($" Done, {Localization.TotalDefaultTranslations} total translations.", ConsoleColor.Magenta);

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
        
        gameObject.AddComponent<ActionLog>();
        Debugger = gameObject.AddComponent<DebugComponent>();
        Data.Singletons = gameObject.AddComponent<SingletonManager>();

        Data.HomebaseLifetime = new ClientRpcConnectionLifetime();
        Data.RpcSerializer = new DefaultSerializer();
        Data.RpcRouter = new DependencyInjectionRpcRouter(Data.Singletons, Data.RpcSerializer, Data.HomebaseLifetime);

        ProxyGenerator.Instance.SetLogger(L.Logger);
        ((IRefSafeLoggable)Data.HomebaseLifetime).SetLogger(L.Logger);
        ((IRefSafeLoggable)Data.RpcRouter       ).SetLogger(L.Logger);

        await HomebaseConnector.ConnectAsync(token);
        await ToUpdate(token);

        if (!Config.DisableDailyQuests)
            Quests.DailyQuests.EarlyLoad();

        ToastManager.Init();

        ActionLog.Add(ActionLogType.ServerStartup, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Data.AdminLocale)}");
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
        _ = RunTask(async token =>
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
        Data.Gamemode?.Subscribe();
        Data.LanguageDataStore?.Subscribe();
        GameUpdateMonitor.OnGameUpdateDetected += EventFunctions.OnGameUpdateDetected;
        EventDispatcher.PlayerJoined += EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving += EventFunctions.OnPlayerDisconnected;
        EventDispatcher.PlayerPendingAsync += EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        UCPlayerLocale.OnLocaleUpdated += EventFunctions.OnLocaleUpdated;
        ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
        BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
        StructureManager.onDeployStructureRequested += EventFunctions.OnStructureTryPlaced;
        ItemManager.onTakeItemRequested += EventFunctions.OnPickedUpItemRequested;
        PlayerEquipment.OnPunch_Global += EventFunctions.OnPunch;
        UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
        UseableGun.onChangeBarrelRequested += EventFunctions.ChangeBarrelRequested;
        UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning += EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned += EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned += EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
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
        EventDispatcher.PlayerAided += EventFunctions.OnPlayerAided;
        EventDispatcher.PlayerDied += EventFunctions.OnPlayerDied;
        PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice2;
    }
    private void UnsubscribeFromEvents()
    {
        Data.Gamemode?.Unsubscribe();
        EventDispatcher.UnsubscribeFromAll();
        Data.LanguageDataStore?.Unsubscribe();
        GameUpdateMonitor.OnGameUpdateDetected -= EventFunctions.OnGameUpdateDetected;
        ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
        EventDispatcher.PlayerJoined -= EventFunctions.OnPostPlayerConnected;
        EventDispatcher.PlayerLeaving -= EventFunctions.OnPlayerDisconnected;
        EventDispatcher.PlayerPendingAsync -= EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        UCPlayerLocale.OnLocaleUpdated -= EventFunctions.OnLocaleUpdated;
        BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
        StructureManager.onDeployStructureRequested -= EventFunctions.OnStructureTryPlaced;
        ItemManager.onTakeItemRequested -= EventFunctions.OnPickedUpItemRequested;
        PlayerEquipment.OnPunch_Global -= EventFunctions.OnPunch;
        UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
        UseableGun.onChangeBarrelRequested -= EventFunctions.ChangeBarrelRequested;
        UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
        PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
        Provider.onLoginSpawning -= EventFunctions.OnCalculateSpawnDuringJoin;
        BarricadeManager.onBarricadeSpawned -= EventFunctions.OnBarricadePlaced;
        StructureManager.onStructureSpawned -= EventFunctions.OnStructurePlaced;
        Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
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
        EventDispatcher.PlayerAided -= EventFunctions.OnPlayerAided;
        PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice2;
    }
    internal void UpdateLangs(UCPlayer player, bool uiOnly)
    {
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

    public static bool IsMainThread => Thread.CurrentThread == ThreadUtil.gameThread;

    [UsedImplicitly]
    private void Update()
    {
        if (LastUpdateDetected > 0 && (Provider.clients.Count == 0 || (Time.realtimeSinceStartup - LastUpdateDetected) > 3600))
            StartCoroutine(ShutdownIn("Unturned Update", 0f));
        
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].Update();
    }

    /// <exception cref="SingletonUnloadedException"/>
    internal static void ForceUnload()
    {
        Nexus.UnloadNow();
        throw new SingletonUnloadedException(typeof(UCWarfare));
    }
    
    public async Task UnloadAsync(CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
        FullyLoaded = false;
        try
        {
            ProcessTasks = false;
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            L.Log("Unloading Uncreated Warfare", ConsoleColor.Magenta);

            ServerHeartbeatTimer.Beat();

            if (Data.Singletons is not null)
            {
                await Data.Singletons.UnloadSingletonAsync(Data.DeathTracker, false, token: token);
                Data.DeathTracker = null!;

                await ToUpdate(token);

                if (Announcer != null)
                {
                    await Data.Singletons.UnloadSingletonAsync(Announcer, token: token);
                    Announcer = null!;
                }
                await ToUpdate(token);

                // save pending damage records
                if (PlayerManager.OnlinePlayers.Any(player => player.DamageRecords.Count > 0))
                {
                    try
                    {
                        await using IStatsDbContext dbContext = new WarfareDbContext();
                        
                        dbContext.DamageRecords.AddRange(PlayerManager.OnlinePlayers.SelectMany(player => player.DamageRecords));
                        PlayerManager.OnlinePlayers.ForEach(player => player.DamageRecords.Clear());

                        await dbContext.SaveChangesAsync(token);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error saving damage records.");
                        L.LogError(ex);
                    }
                }

                await ToUpdate(token);

                if (Data.Gamemode != null)
                {
                    Data.Gamemode.IsPendingCancel = true;
                    await Data.Singletons.UnloadSingletonAsync(Data.Gamemode, token: token);
                    Data.Gamemode = null!;
                }
            }

            await LetTasksUnload(token).ConfigureAwait(false);
            await ToUpdate(token);

            if (Data.ModerationSql != null)
            {
                Data.ModerationSql.OnModerationEntryUpdated -= OffenseManager.OnModerationEntryUpdated;
                Data.ModerationSql.OnNewModerationEntryAdded -= OffenseManager.OnNewModerationEntryAdded;
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
                using CancellationTokenSource src = new CancellationTokenSource(5000);
                CancellationToken token2 = src.Token;
                using CombinedTokenSources tokens = token2.CombineTokensIfNeeded(src.Token);
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
                using CancellationTokenSource src = new CancellationTokenSource(5000);
                CancellationToken token2 = src.Token;
                using CombinedTokenSources tokens = token2.CombineTokensIfNeeded(src.Token);
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

            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;

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
        }
        catch (Exception ex)
        {
            L.LogError("Error unloading: ");
            L.LogError(ex);
        }

        if (Data.Singletons != null)
        {
            await Data.Singletons.UnloadAllAsync(token);
            await ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }

        Data.NilSteamPlayer = null!;
        L.Log("Warfare unload complete", ConsoleColor.Blue);
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
        ModuleHook.PreVanillaAssemblyResolvePostRedirects += ResolveAssemblyCompiler;
        ModuleHook.PostVanillaAssemblyResolve += ErrorAssemblyNotResolved;
        try
        {
            Init2();
        }
        catch (Exception ex)
        {
            CommandWindow.LogError(ex);
        }
    }

    private void Init2()
    {
        CommandWindow.Log("Initializing UCWarfareNexus...");
        Active = true;
        try
        {
            L.Init();
        }
        catch (Exception ex)
        {
            CommandWindow.LogError(ex);
            return;
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
    private static Assembly? ErrorAssemblyNotResolved(object sender, ResolveEventArgs args)
    {
        // this can be raised when looking for other language translations for an assembly
        if (!args.Name.Contains(".resources, ", StringComparison.Ordinal))
        {
            CommandWindow.LogError($"Unknown assembly: {args.Name}.");
        }
        else
        {
            CommandWindow.Log($"Unknown resx assembly: {args.Name}.");
        }
        return null;
    }
    private static Assembly? ResolveAssemblyCompiler(object sender, ResolveEventArgs args)
    {
        const string runtime = "System.Runtime.CompilerServices.Unsafe";

        if (args.Name.StartsWith(runtime, StringComparison.Ordinal))
        {
            CommandWindow.LogWarning($"Redirected {args.Name} -> {runtime}.dll");
            return typeof(Unsafe).Assembly;
        }

        return null;
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
        ModuleHook.PreVanillaAssemblyResolvePostRedirects -= ResolveAssemblyCompiler;
        Level.onPostLevelLoaded -= OnLevelLoaded;
        if (!UCWarfare.IsLoaded) return;
        Unload(false).Wait(10000);
    }
}