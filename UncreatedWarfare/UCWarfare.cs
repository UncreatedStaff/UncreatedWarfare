﻿using Newtonsoft.Json;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Uncreated.Networking;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
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
        public Queue<System.Action> RunOnMainThread = new Queue<System.Action>();
        protected override void Load()
        {
            Instance = this;
            Data.Logs = Data.ReadRocketLog();
            if (Config.UsePatchForPlayerCap)
                Provider.maxPlayers = 24;
            Data.LoadColoredConsole();
            F.Log("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.", ConsoleColor.Green);

            /* PLAYER COUNT VERIFICATION */
            if (!Config.UsePatchForPlayerCap && Provider.clients.Count >= 24)
            {
                Provider.maxPlayers = Config.MaxPlayerCount;
                F.Log("Set max player count to " + Provider.maxPlayers.ToString(), ConsoleColor.Magenta);
            }

            /* PATCHES */
            F.Log("Patching methods...", ConsoleColor.Magenta);
            try
            {
                Patches.DoPatching();
            }
            catch (Exception ex)
            {
                F.LogError("Patching Error, perhaps Nelson changed something:");
                F.LogError(ex);
            }


            /* DEBUG MYSQL LOADING */
            if (LoadMySQLDataFromElsewhere)
            {
                if (!File.Exists(Data.ElseWhereSQLPath))
                {
                    TextWriter w = File.CreateText(Data.ElseWhereSQLPath);
                    JsonTextWriter wr = new JsonTextWriter(w);
                    JsonSerializer s = new JsonSerializer { Formatting = Formatting.Indented };
                    s.Serialize(wr, Config.SQL);
                    wr.Close();
                    w.Close();
                    w.Dispose();
                    _sqlElsewhere = Config.SQL;
                }
                else
                {
                    string json = File.ReadAllText(Data.ElseWhereSQLPath);
                    _sqlElsewhere = JsonConvert.DeserializeObject<MySqlData>(json);
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
            Provider.configData.Normal.Players.Lose_Items_PvP = 0;
            Provider.configData.Normal.Players.Lose_Items_PvE = 0;
            Provider.configData.Normal.Players.Lose_Clothes_PvP = false;
            Provider.configData.Normal.Players.Lose_Clothes_PvE = false;
            Provider.configData.Normal.Barricades.Decay_Time = 0;
            Provider.configData.Normal.Structures.Decay_Time = 0;

            base.Load();
            UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        }
        private void OnLevelLoaded(int level)
        {
            F.CheckDir(Data.FlagStorage, out _, true);
            F.CheckDir(Data.StructureStorage, out _, true);
            F.CheckDir(Data.VehicleStorage, out _, true);
            if (Config.Modules.VehicleSpawning)
            {
            }
            Announcer = gameObject.AddComponent<UCAnnouncer>();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            Data.ExtraZones = JSONMethods.LoadExtraZones();
            F.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
            Data.Gamemode.OnLevelLoaded();
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
            ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
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
            ItemManager.onServerSpawningItemDrop -= EventFunctions.OnDropItemFinal;
            PlayerVoice.onRelayVoice -= EventFunctions.OnRelayVoice;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
        }
        private void OnPluginsLoaded()
        {
            F.Log("Subscribing to events...", ConsoleColor.Magenta);
            SubscribeToEvents();
        }
        internal void UpdateLangs(SteamPlayer player)
        {
            foreach (BarricadeRegion region in BarricadeManager.regions)
            {
                List<BarricadeDrop> signs = new List<BarricadeDrop>();
                foreach (BarricadeDrop drop in region.drops)
                {
                    if (drop.interactable is InteractableSign sign)
                    {
                        if (sign.text.StartsWith("sign_"))
                        {
                            F.InvokeSignUpdateFor(player, sign, false);
                        }
                    }
                }
            }
            if (Data.Gamemode is TeamCTF ctf)
            {
                CTFUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, player.GetTeam(), ctf.Rotation,
                    ctf.Config.FlagUICount, ctf.Config.AttackIcon, ctf.Config.DefendIcon);
                ulong team = player.GetTeam();
                UCPlayer ucplayer = UCPlayer.FromSteamPlayer(player);
                if (ucplayer.Squad == null)
                    SquadManager.UpdateSquadList(ucplayer);
                else
                {
                    SquadManager.UpdateUISquad(ucplayer.Squad);
                    SquadManager.UpdateUIMemberCount(team);
                    if (RallyManager.HasRally(ucplayer.Squad, out RallyPoint p))
                        p.ShowUIForPlayer(ucplayer);
                }
                XP.XPManager.UpdateUI(player.player, XP.XPManager.GetXP(player.player, false), out _);
                Officers.OfficerManager.UpdateUI(player.player, Officers.OfficerManager.GetOfficerPoints(player.player, false), out _);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour")]
        private void Update()
        {
            while (RunOnMainThread.Count > 0)
            {
                try
                {
                    RunOnMainThread.Dequeue().Invoke();
                }
                catch (Exception ex)
                {
                    F.LogError("Error running on main thread:");
                    F.LogError(ex);
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
            F.Log("Unloading " + Name, ConsoleColor.Magenta);
            if (Announcer != null)
                Destroy(Announcer);
            //if (Queue != null)
            //Destroy(Queue);
            Data.Gamemode?.Dispose();
            Data.DatabaseManager?.Dispose();
            F.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            F.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            Data.NetClient.Dispose();
            Logging.OnLog -= F.Log;
            Logging.OnLogWarning -= F.LogWarning;
            Logging.OnLogError -= F.LogError;
            Logging.OnLogException -= F.LogError;
            try
            {
                Patches.Unpatch();
            }
            catch (Exception ex)
            {
                F.LogError("Unpatching Error, perhaps Nelson changed something:");
                F.LogError(ex);
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
