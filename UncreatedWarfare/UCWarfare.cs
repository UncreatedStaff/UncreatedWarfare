#define USE_DEBUGGER

using SDG.Framework.Modules;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Uncreated.Networking;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public partial class UCWarfare : MonoBehaviour, IUncreatedSingleton
{
    public static readonly TimeSpan RestartTime = new TimeSpan(1, 00, 0); // 9:00 PM EST
    public static readonly Version Version      = new Version(2, 6, 0, 2);
    private readonly SystemConfig _config       = new SystemConfig();
#if DEBUG
    private readonly TestConfig _testConfig     = new TestConfig();
#endif
    public static UCWarfare I;
    internal static UCWarfareNexus Nexus;
    public Coroutine? StatsRoutine;
    public UCAnnouncer Announcer;
    internal DebugComponent Debugger;
    public event EventHandler UCWarfareLoaded;
    public event EventHandler UCWarfareUnloading;
    public bool CoroutineTiming = false;
    private bool InitialLoadEventSubscription;
    private DateTime NextRestartTime;
    public static int Season => Version.Major;
    bool IUncreatedSingleton.IsLoaded => IsLoaded;
    public static bool IsLoaded => I is not null;
    public static SystemConfigData Config => I is null ? throw new SingletonUnloadedException(typeof(UCWarfare)) : I._config.Data;
    public static bool CanUseNetCall => IsLoaded && Config.TCPSettings.EnableTCPServer && Data.NetClient is not null && Data.NetClient.IsActive;
    private void Awake()
    {
        if (I != null) throw new SingletonLoadException(ESingletonLoadType.LOAD, this, new Exception("Uncreated Warfare is already loaded."));
        I = this;
    }
    private void Start() => EarlyLoad();
    private void EarlyLoad()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Started loading - Uncreated Warfare version " + Version.ToString() + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
              "Please stop using this plugin now.", ConsoleColor.Green);

        L.Log("Registering Commands: ", ConsoleColor.Magenta);
        CommandHandler.LoadCommands();

        DateTime loadTime = DateTime.Now;
        if (loadTime.TimeOfDay > RestartTime - TimeSpan.FromHours(2)) // dont restart if the restart would be in less than 2 hours
            NextRestartTime = loadTime.Date + RestartTime + TimeSpan.FromDays(1);
        else
            NextRestartTime = loadTime.Date + RestartTime;
        L.Log("Restart scheduled at " + NextRestartTime.ToString("g"), ConsoleColor.Magenta);
        float seconds = (float)(NextRestartTime - DateTime.Now).TotalSeconds;

        StartCoroutine(RestartIn(seconds));

        new PermissionSaver();

        TeamManager.SetupConfig();

        Data.LanguageAliases = JSONMethods.LoadLangAliases();

        Translation.ReadTranslations();

        /* PATCHES */
        L.Log("Patching methods...", ConsoleColor.Magenta);
        try
        {
            Patches.DoPatching();
        }
        catch (Exception ex)
        {
            L.LogError("Patching Error, perhaps Nelson changed something:");
            L.LogError(ex);
        }

        UCInventoryManager.OnLoad();

        gameObject.AddComponent<ActionLogger>();
        Debugger = gameObject.AddComponent<DebugComponent>();
        Data.Singletons = gameObject.AddComponent<SingletonManager>();


        Quests.DailyQuests.EarlyLoad();

        ActionLogger.Add(EActionLogType.SERVER_STARTUP, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Data.Locale)}");
    }

    public void Load()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EventDispatcher.SubscribeToAll();

        OffenseManager.Init();

        try
        {
            /* DATA CONSTRUCTION */
            Data.LoadVariables();
        }
        catch (Exception ex)
        {
            L.LogError("Startup error");
            L.LogError(ex);
            throw new SingletonLoadException(ESingletonLoadType.LOAD, this, ex);
        }

        /* START STATS COROUTINE */
        StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

        L.Log("Subscribing to events...", ConsoleColor.Magenta);
        SubscribeToEvents();

        OnLevelLoaded(Level.BUILD_INDEX_GAME);
        InitialLoadEventSubscription = true;

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
    private void OnLevelLoaded(int level)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.Paths.FlagStorage, out _, true);
        F.CheckDir(Data.Paths.StructureStorage, out _, true);
        F.CheckDir(Data.Paths.VehicleStorage, out _, true);
        ZonePlayerComponent.UIInit();
        Zone.OnLevelLoaded();

        Data.ZoneProvider.Reload();
        Data.ZoneProvider.Save();

        Announcer = Data.Singletons.LoadSingleton<UCAnnouncer>();
        Data.ExtraPoints = JSONMethods.LoadExtraPoints();
        //L.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
        if (Data.Gamemode is not null) Data.Gamemode.OnLevelReady();

        if (Config.Debug && File.Exists(@"C:\orb.wav"))
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\orb.wav");
            player.Load();
            player.Play();
        }

        Debugger.Reset();
    }
    private void SubscribeToEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Subscribe();
        StatsManager.LoadEvents();

        EventDispatcher.OnPlayerJoined += EventFunctions.OnPostPlayerConnected;
        EventDispatcher.OnPlayerLeaving += EventFunctions.OnPlayerDisconnected;
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
        EventDispatcher.OnEnterVehicle += EventFunctions.OnEnterVehicle;
        EventDispatcher.OnVehicleSwapSeat += EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.OnExitVehicle += EventFunctions.OnPlayerLeavesVehicle;
        EventDispatcher.OnLandmineExploding += EventFunctions.OnLandmineExploding;
        VehicleManager.onDamageVehicleRequested += EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed += EventFunctions.OnConsume;
        EventDispatcher.OnBarricadeDestroyed += EventFunctions.OnBarricadeDestroyed;
        Patches.StructureDestroyedHandler += EventFunctions.OnStructureDestroyed;
        PlayerInput.onPluginKeyTick += EventFunctions.OnPluginKeyPressed;
        PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice2;
    }
    private void UnsubscribeFromEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Unsubscribe();
        EventDispatcher.UnsubscribeFromAll();

        ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
        EventDispatcher.OnPlayerJoined -= EventFunctions.OnPostPlayerConnected;
        EventDispatcher.OnPlayerLeaving -= EventFunctions.OnPlayerDisconnected;
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
        EventDispatcher.OnLandmineExploding -= EventFunctions.OnLandmineExploding;
        EventDispatcher.OnEnterVehicle -= EventFunctions.OnEnterVehicle;
        EventDispatcher.OnVehicleSwapSeat -= EventFunctions.OnVehicleSwapSeat;
        EventDispatcher.OnExitVehicle -= EventFunctions.OnPlayerLeavesVehicle;
        VehicleManager.onDamageVehicleRequested -= EventFunctions.OnPreVehicleDamage;
        ItemManager.onServerSpawningItemDrop -= EventFunctions.OnDropItemFinal;
        UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
        UseableConsumeable.onConsumePerformed -= EventFunctions.OnConsume;
        EventDispatcher.OnBarricadeDestroyed -= EventFunctions.OnBarricadeDestroyed;
        Patches.StructureDestroyedHandler -= EventFunctions.OnStructureDestroyed;
        PlayerInput.onPluginKeyTick -= EventFunctions.OnPluginKeyPressed;
        PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice2;
        StatsManager.UnloadEvents();
        if (!InitialLoadEventSubscription)
        {
            Level.onLevelLoaded -= OnLevelLoaded;
        }
    }
    internal static Queue<MainThreadTask.MainThreadResult> ThreadActionRequests = new Queue<MainThreadTask.MainThreadResult>();
    public static MainThreadTask ToUpdate() => new MainThreadTask();
    public static bool IsMainThread => Thread.CurrentThread.IsGameThread();
    public static void RunOnMainThread(System.Action action)
    {
        MainThreadTask.MainThreadResult res = new MainThreadTask.MainThreadResult(new MainThreadTask());
        res.OnCompleted(action);
    }
    internal void UpdateLangs(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EventDispatcher.InvokeUIRefreshRequest(player);
        UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(player);
        foreach (BarricadeRegion region in BarricadeManager.regions)
        {
            List<BarricadeDrop> signs = new List<BarricadeDrop>();
            foreach (BarricadeDrop drop in region.drops)
            {
                if (drop.interactable is InteractableSign sign)
                {
                    bool found = false;
                    if (VehicleSpawner.Loaded && VehicleSpawner.TryGetSpawnFromSign(sign, out Vehicles.VehicleSpawn spawn))
                    {
                        spawn.UpdateSign(player);
                        found = true;
                    }
                    if (!found && sign.text.StartsWith("sign_"))
                    {
                        F.InvokeSignUpdateFor(player, sign, false);
                    }
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
        else if (Data.Is(out Insurgency ins))
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
    }
    private void Update()
    {
        while (ThreadActionRequests.Count > 0)
        {
            MainThreadTask.MainThreadResult? res = null;
            try
            {
                res = ThreadActionRequests.Dequeue();
                res.continuation();
            }
            catch (Exception ex)
            {
                L.LogError("ERROR DEQUEING AND RUNNING MAIN THREAD OPERATION");
                L.LogError(ex);
            }
            finally
            {
                res?.Complete();
            }
        }
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            PlayerManager.OnlinePlayers[i].Update();
    }
    /// <exception cref="SingletonUnloadedException"/>
    internal static void ForceUnload()
    {
        Nexus.UnloadNow();
        throw new SingletonUnloadedException(typeof(UCWarfare));
    }
    public void Unload()
    {
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
                Data.Singletons.UnloadSingleton(ref Data.DeathTracker, false);
                Data.Singletons.UnloadSingleton(ref Data.Gamemode);
                if (Announcer != null)
                    Data.Singletons.UnloadSingleton(ref Announcer);
            }

            if (Maps.MapScheduler.Instance != null)
            {
                Destroy(Maps.MapScheduler.Instance);
                Maps.MapScheduler.Instance = null!;
            }

            if (Debugger != null)
                Destroy(Debugger);
            OffenseManager.Deinit();
            //if (Queue != null)
            //Destroy(Queue);
            try
            {
                Data.DatabaseManager?.Dispose();
            }
            finally
            {
                Data.DatabaseManager = null!;
            }
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            try
            {
                Data.NetClient?.Dispose();
            }
            finally
            {
                Data.NetClient = null!;
            }
            Logging.OnLogInfo -= L.NetLogInfo;
            Logging.OnLogWarning -= L.NetLogWarning;
            Logging.OnLogError -= L.NetLogError;
            Logging.OnLogException -= L.NetLogException;
            try
            {
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
        if (Data.Singletons is not null)
            Data.Singletons.UnloadAll();
        L.Log("Warfare unload complete", ConsoleColor.Blue);
#if DEBUG
        profiler.Dispose();
        F.SaveProfilingData();
#endif
        Thread.Sleep(1000);
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
        if (Data.ColorsHex == null) return "ffffff";
        if (Data.ColorsHex.TryGetValue(key, out string color)) return color;
        else if (Data.ColorsHex.TryGetValue("default", out color)) return color;
        else return "ffffff";
    }

    public static void ShutdownIn(string reason, ulong instigator, int seconds)
    {
        I.StartCoroutine(I.ShutdownIn2(reason, instigator, seconds));
    }
    public static void ShutdownNow(string reason, ulong instigator)
    {
        for (int i = 0; i < Provider.clients.Count; ++i)
            Provider.kick(Provider.clients[i].playerID.steamID, "Intentional Shutdown: " + reason);

        if (VehicleBay.Loaded && VehicleSpawner.Loaded)
            VehicleBay.AbandonAllVehicles();

        if (CanUseNetCall)
        {
            ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
            I.StartCoroutine(I.ShutdownIn(reason, 4));
        }
        else
        {
            Nexus.Unload();
            Provider.shutdown(2, reason);
        }
    }
    private IEnumerator<WaitForSeconds> ShutdownIn(string reason, float seconds)
    {
        yield return new WaitForSeconds(seconds / 2);
        Nexus.Unload();
        Provider.shutdown(Mathf.RoundToInt(seconds / 2), reason);
    }
    private IEnumerator<WaitForSeconds> ShutdownIn2(string reason, ulong instigator, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ShutdownCommand.NetCalls.SendShuttingDownInstant.NetInvoke(instigator, reason);
        yield return new WaitForSeconds(1f);
        Nexus.Unload();
        Provider.shutdown(2, reason);
    }
}

public class UCWarfareNexus : IModuleNexus
{
    public bool Loaded { get; private set; } = false;
    void IModuleNexus.initialize()
    {
        Data.LoadColoredConsole();
        Level.onPostLevelLoaded += OnLevelLoaded;
        UCWarfare.Nexus = this;
        GameObject go = new GameObject("UCWarfare " + UCWarfare.Version.ToString());
        go.AddComponent<Maps.MapScheduler>();
        UnityEngine.Object.DontDestroyOnLoad(go);
        UCWarfare warfare = go.AddComponent<UCWarfare>();
    }
    private void Load()
    {
        try
        {
            UCWarfare.I.Load();
            Loaded = true;
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            Loaded = false;
            Type t = ex.GetType();
            ShutdownCommand.ShutdownIn(10, "Uncreated Warfare failed to load: " + t.Name);
            if (typeof(SingletonLoadException).IsAssignableFrom(t))
                throw;
            else
                throw new SingletonLoadException(ESingletonLoadType.LOAD, UCWarfare.I, ex);
        }
    }

    private void OnLevelLoaded(int level)
    {
        if (level == Level.BUILD_INDEX_GAME)
        {
            Load();
        }
    }

    public void UnloadNow()
    {
        Unload();
        ShutdownCommand.ShutdownIn(10, "Uncreated Warfare unloading.");
    }
    public void Unload()
    {
        try
        {
            UCWarfare.I.Unload();
            UnityEngine.Object.Destroy(UCWarfare.I.gameObject);
            UCWarfare.I = null!;
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            if (typeof(SingletonLoadException).IsAssignableFrom(ex.GetType()))
                throw;
            else
                throw new SingletonLoadException(ESingletonLoadType.UNLOAD, UCWarfare.I, ex);
        }
    }
    void IModuleNexus.shutdown()
    {
        Level.onLevelLoaded -= OnLevelLoaded;
        if (!UCWarfare.IsLoaded) return;
        Unload();
    }
}