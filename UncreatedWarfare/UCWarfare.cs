using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare
{
    public delegate void VoidDelegate();
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
        public Coroutine StatsRoutine;
        public UCAnnouncer Announcer;
        //public NetworkingQueue Queue;
        public static UCWarfare I { get => Instance; }
        public static Config Config { get => Instance.Configuration.Instance; }
        private MySqlData _sqlElsewhere;
        public MySqlData SQL
        {
            get
            {
                return LoadMySQLDataFromElsewhere && (!_sqlElsewhere.Equals(default(MySqlData))) ? _sqlElsewhere : Configuration.Instance.SQL;
            }
        }
        public bool LoadMySQLDataFromElsewhere = false; // for having sql password defaults without having them in our source code.
        public event EventHandler UCWarfareLoaded;
        public event EventHandler UCWarfareUnloading;
        public bool CoroutineTiming = false;
        private bool InitialLoadEventSubscription;
        protected override void Load()
        {
            Instance = this;
            Data.Logs = Data.ReadRocketLog();
            if (Config.UsePatchForPlayerCap)
                Provider.maxPlayers = 24;
            Data.LoadColoredConsole();
            L.Log("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.", ConsoleColor.Green);

            /* PLAYER COUNT VERIFICATION */
            if (!Config.UsePatchForPlayerCap && Provider.clients.Count >= 24)
            {
                Provider.maxPlayers = Config.MaxPlayerCount;
                L.Log("Set max player count to " + Provider.maxPlayers.ToString(), ConsoleColor.Magenta);
            }

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
                    _sqlElsewhere = JsonSerializer.Deserialize<MySqlData>(json, JsonEx.serializerSettings);
                }
            }

            /* DATA CONSTRUCTION */
            Data.LoadVariables();

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
        private void OnLevelLoaded(int level)
        {
            F.CheckDir(Data.FlagStorage, out _, true);
            F.CheckDir(Data.StructureStorage, out _, true);
            F.CheckDir(Data.VehicleStorage, out _, true);
            // remove once effectmanager supports GUIDs
            SquadManager.TempCacheEffectIDs();
            CTFUI.TempCacheEffectIDs();
            LeaderboardEx.TempCacheEffectIDs();
            FOBManager.TempCacheEffectIDs();
            Announcer = gameObject.AddComponent<UCAnnouncer>();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            Data.ExtraZones = JSONMethods.LoadExtraZones();
            L.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);

            // remove once effectmanager supports GUIDs
            SquadManager.TempCacheEffectIDs();
            CTFUI.TempCacheEffectIDs();
            LeaderboardEx.TempCacheEffectIDs();
            FOBManager.TempCacheEffectIDs();
            FOBManager.OnLevelLoaded();

            Data.Gamemode.OnLevelLoaded();

            if (File.Exists(@"C:\orb.wav"))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\orb.wav");
                player.Load();
                player.Play();
            }

        }
        private void SubscribeToEvents()
        {
            Data.Gamemode.Subscribe();
            U.Events.OnPlayerConnected += EventFunctions.OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected += EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
            Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
            Commands.LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
            Commands.ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
            BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned += EventFunctions.ThrowableSpawned;
            PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
            BarricadeManager.onBarricadeSpawned += EventFunctions.OnBarricadePlaced;
            Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
            Patches.OnPlayerGesture_Global += EventFunctions.OnPlayerGestureRequested;
            Patches.OnPlayerMarker_Global += EventFunctions.OnPlayerMarkedPosOnMap;
            DamageTool.damagePlayerRequested += EventFunctions.OnPlayerDamageRequested;
            EventFunctions.OnGroupChanged += EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested += EventFunctions.BarricadeMovedInWorkzone;
            BarricadeManager.onDamageBarricadeRequested += EventFunctions.OnBarricadeDamaged;
            StructureManager.onTransformRequested += EventFunctions.StructureMovedInWorkzone;
            StructureManager.onDamageStructureRequested += EventFunctions.OnStructureDamaged;
            BarricadeManager.onOpenStorageRequested += EventFunctions.OnEnterStorage;
            VehicleManager.onExitVehicleRequested += EventFunctions.OnPlayerLeavesVehicle;
            VehicleManager.onDamageVehicleRequested += EventFunctions.OnPreVehicleDamage;
            ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
            UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
            Patches.BarricadeDestroyedHandler += EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler += EventFunctions.OnStructureDestroyed;
            PlayerInput.onPluginKeyTick += EventFunctions.OnPluginKeyPressed;
            PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice;
        }
        private void UnsubscribeFromEvents()
        {
            Data.Gamemode.Unsubscribe();
            Commands.ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
            U.Events.OnPlayerConnected -= EventFunctions.OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected -= EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation -= EventFunctions.OnPrePlayerConnect;
            Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
            Commands.LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
            BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned -= EventFunctions.ThrowableSpawned;
            PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
            BarricadeManager.onBarricadeSpawned -= EventFunctions.OnBarricadePlaced;
            Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global -= EventFunctions.OnTryStoreItem;
            Patches.OnPlayerGesture_Global -= EventFunctions.OnPlayerGestureRequested;
            Patches.OnPlayerMarker_Global -= EventFunctions.OnPlayerMarkedPosOnMap;
            DamageTool.damagePlayerRequested -= EventFunctions.OnPlayerDamageRequested;
            EventFunctions.OnGroupChanged -= EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested -= EventFunctions.BarricadeMovedInWorkzone;
            BarricadeManager.onDamageBarricadeRequested -= EventFunctions.OnBarricadeDamaged;
            StructureManager.onTransformRequested -= EventFunctions.StructureMovedInWorkzone;
            BarricadeManager.onOpenStorageRequested -= EventFunctions.OnEnterStorage;
            StructureManager.onDamageStructureRequested -= EventFunctions.OnStructureDamaged;
            VehicleManager.onExitVehicleRequested -= EventFunctions.OnPlayerLeavesVehicle;
            VehicleManager.onDamageVehicleRequested -= EventFunctions.OnPreVehicleDamage;
            ItemManager.onServerSpawningItemDrop -= EventFunctions.OnDropItemFinal;
            UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
            Patches.BarricadeDestroyedHandler -= EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler -= EventFunctions.OnStructureDestroyed;
            PlayerInput.onPluginKeyTick -= EventFunctions.OnPluginKeyPressed;
            PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
        }
        internal static Queue<MainThreadTask.MainThreadResult> ThreadActionRequests = new Queue<MainThreadTask.MainThreadResult>();
        public static MainThreadTask ToUpdate() => new MainThreadTask();
        public static PoolTask ToPool() => new PoolTask();
        public static bool IsMainThread => System.Threading.Thread.CurrentThread.IsGameThread();
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
            UCPlayer ucplayer = UCPlayer.FromSteamPlayer(player);
            foreach (BarricadeRegion region in BarricadeManager.regions)
            {
                List<BarricadeDrop> signs = new List<BarricadeDrop>();
                foreach (BarricadeDrop drop in region.drops)
                {
                    if (drop.interactable is InteractableSign sign)
                    {
                        bool found = false;
                        for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                        {
                            Vehicles.VehicleSpawn spawn = VehicleSpawner.ActiveObjects[i];
                            if (spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable == sign)
                            {
                                spawn.UpdateSign(player);
                                found = true;
                                break;
                            }
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
            if (Data.Gamemode.ShowOFPUI)
                Points.UpdateTWUI(ucplayer);
        }
        private void Update()
        {
            while (ThreadActionRequests.Count > 0)
            {
                MainThreadTask.MainThreadResult res = null;
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
        }
        protected override void Unload()
        {
            if (StatsRoutine != null)
            {
                StopCoroutine(StatsRoutine);
                StatsRoutine = null;
            }
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);
            L.Log("Unloading " + Name, ConsoleColor.Magenta);
            if (Announcer != null)
                Destroy(Announcer);
            //if (Queue != null)
            //Destroy(Queue);
            Data.Gamemode?.Dispose();
            Data.DatabaseManager?.Dispose();
            L.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            L.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            Data.NetClient.Dispose();
            Logging.OnLog -= L.Log;
            Logging.OnLogWarning -= L.LogWarning;
            Logging.OnLogError -= L.LogError;
            Logging.OnLogException -= L.LogError;
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
            NetFactory.ClearRegistry();
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
}
