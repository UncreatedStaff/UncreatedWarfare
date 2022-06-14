#define USE_DEBUGGER

using Rocket.Core;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public delegate void VoidDelegate();
public partial class UCWarfare : RocketPlugin<Config>
{
    public static readonly TimeSpan RestartTime = new TimeSpan(21, 00, 0); // 9:00 PM
    public static readonly Version Version      = new Version(2, 5, 1, 2);
    public static UCWarfare Instance;
    public Coroutine? StatsRoutine;
    public UCAnnouncer Announcer;
    //public NetworkingQueue Queue;
    private MySqlData _sqlElsewhere;
    public bool LoadMySQLDataFromElsewhere = false; // for having sql password defaults without having them in our source code.
    public event EventHandler UCWarfareLoaded;
    public event EventHandler UCWarfareUnloading;
    public bool CoroutineTiming = false;
    private bool InitialLoadEventSubscription;
    private DateTime NextRestartTime;
    public static bool IsLoaded => Instance is not null;
    public static UCWarfare I { get => Instance; }
    public static Config Config { get => Instance?.Configuration?.Instance!; }
    public static bool CanUseNetCall => IsLoaded && Instance.Configuration is not null && Instance.Configuration.Instance is not null && Config.PlayerStatsSettings.EnableTCPServer && Data.NetClient is not null && Data.NetClient.IsActive;
    public static int Season => Version.Major;
    public MySqlData SQL => LoadMySQLDataFromElsewhere && _sqlElsewhere is not null ? _sqlElsewhere : Configuration.Instance.SQL;
    protected override void Load()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Instance = this;
        L.Logs = L.ReadRocketLog();
        Data.LoadColoredConsole();
        L.Log("Started loading " + Name + " - " + Version.ToString() + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
            "Please stop using this plugin now.", ConsoleColor.Green);

        DateTime loadTime = DateTime.Now;
        if (loadTime.TimeOfDay > RestartTime - TimeSpan.FromHours(2)) // dont restart if the restart would be in less than 2 hours
            NextRestartTime = loadTime.Date + RestartTime + TimeSpan.FromDays(1);
        else
            NextRestartTime = loadTime.Date + RestartTime;
        L.Log("Restart scheduled at " + NextRestartTime.ToString("g"), ConsoleColor.Magenta);
        float seconds = (float)(NextRestartTime - DateTime.Now).TotalSeconds;

        StartCoroutine(RestartIn(seconds));

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

        EventDispatcher.SubscribeToAll();

        OffenseManager.Init();

        /* DEBUG MYSQL LOADING */
        if (LoadMySQLDataFromElsewhere)
        {
            if (!File.Exists(Data.ElseWhereSQLPath))
            {
                TextWriter w = File.CreateText(Data.ElseWhereSQLPath);
                w.Write(JsonSerializer.Serialize(Config.SQL, JsonEx.serializerSettings));
                w.Close();
                w.Dispose();
                _sqlElsewhere = Config.SQL;
            }
            else
            {
                string json = File.ReadAllText(Data.ElseWhereSQLPath);
                _sqlElsewhere = JsonSerializer.Deserialize<MySqlData>(json, JsonEx.serializerSettings)!;
            }
        }

        try
        {
            /* DATA CONSTRUCTION */
            Data.LoadVariables();
        }
        catch (Exception ex)
        {
            L.LogError("Startup error");
            L.LogError(ex);
            UnloadPlugin(Rocket.API.PluginState.Failure);
            Provider.shutdown(2);
            return;
        }

        /* START STATS COROUTINE */
        StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

        /* LEVEL SUBSCRIPTIONS */
        if (Level.isLoaded)
        {
            //StartCheckingPlayers(Data.CancelFlags.Token).ConfigureAwait(false); // starts the function without awaiting
            SubscribeToEvents();
            OnLevelLoaded(2);
            InitialLoadEventSubscription = true;
        }
        else
        {
            InitialLoadEventSubscription = false;
            Level.onLevelLoaded += OnLevelLoaded;
            R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
        }

        /* BASIC CONFIGS */
        Provider.modeConfigData.Players.Lose_Items_PvP = 0;
        Provider.modeConfigData.Players.Lose_Items_PvE = 0;
        Provider.modeConfigData.Players.Lose_Clothes_PvP = false;
        Provider.modeConfigData.Players.Lose_Clothes_PvE = false;
        Provider.modeConfigData.Barricades.Decay_Time = 0;
        Provider.modeConfigData.Structures.Decay_Time = 0;

        base.Load();
        UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
    }
    private IEnumerator<WaitForSeconds> RestartIn(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Commands.ShutdownOverrideCommand.ShutdownAfterGameDaily();
    }
    private void OnLevelLoaded(int level)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.FlagStorage, out _, true);
        F.CheckDir(Data.StructureStorage, out _, true);
        F.CheckDir(Data.VehicleStorage, out _, true);
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
        Commands.LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
        Commands.ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
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
        R.Commands.OnExecuteCommand += EventFunctions.OnCommandExecuted;
    }
    private void UnsubscribeFromEvents()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.Gamemode?.Unsubscribe();
        EventDispatcher.UnsubscribeFromAll();
        Commands.ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
        EventDispatcher.OnPlayerJoined -= EventFunctions.OnPostPlayerConnected;
        EventDispatcher.OnPlayerLeaving -= EventFunctions.OnPlayerDisconnected;
        Provider.onCheckValidWithExplanation -= EventFunctions.OnPrePlayerConnect;
        Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
        Commands.LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
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
        if (R.Commands is not null)
            R.Commands.OnExecuteCommand -= EventFunctions.OnCommandExecuted;
        if (!InitialLoadEventSubscription)
        {
            Level.onLevelLoaded -= OnLevelLoaded;
            R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
        }

    }
    internal static Queue<MainThreadTask.MainThreadResult> ThreadActionRequests = new Queue<MainThreadTask.MainThreadResult>();
    public static MainThreadTask ToUpdate() => new MainThreadTask();
    public static PoolTask ToPool() => new PoolTask();
    public static bool IsMainThread => Thread.CurrentThread.IsGameThread();
    public static void RunOnMainThread(System.Action action)
    {
        MainThreadTask.MainThreadResult res = new MainThreadTask.MainThreadResult(new MainThreadTask());
        res.OnCompleted(action);
    }
    private void OnPluginsLoaded()
    {
        L.Log("Subscribing to events...", ConsoleColor.Magenta);
        SubscribeToEvents();
    }
    internal void UpdateLangs(SteamPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        if (TicketManager.Loaded)
            TicketManager.UpdateUI(ucplayer);
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
    internal static void ForceUnload()
    {
        I.UnloadPlugin();
    }
    protected override void Unload()
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

            L.Log("Unloading " + Name, ConsoleColor.Magenta);

            Data.Singletons.UnloadSingleton(ref Data.DeathTracker, false);
            Data.Singletons.UnloadSingleton(ref Data.Gamemode);
            OffenseManager.Deinit();
            if (Announcer != null)
                Data.Singletons.UnloadSingleton(ref Announcer);
            //if (Queue != null)
            //Destroy(Queue);
            Data.DatabaseManager?.Dispose();
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            Data.NetClient?.Dispose();
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
        Data.Singletons.UnloadAll();
        L.Log("Warfare unload complete", ConsoleColor.Blue);
        Thread.Sleep(1000);
#if DEBUG
        profiler.Dispose();
        F.SaveProfilingData();
#endif
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
}
