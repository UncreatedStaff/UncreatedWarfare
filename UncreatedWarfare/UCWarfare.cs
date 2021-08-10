using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Rocket.Core;
using Rocket.Unturned;
using Uncreated.Warfare.Stats;
using Newtonsoft.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;
using Steamworks;
using Rocket.Core.Steam;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using System.Linq;

namespace Uncreated.Warfare
{
    public delegate void VoidDelegate();
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
        public Coroutine StatsRoutine;
        public Components.UCAnnouncer Announcer;
        public static UCWarfare I { get => Instance; }
        public static Config Config { get => Instance.Configuration.Instance; }
        private MySqlData _sqlElsewhere;
        public MySqlData SQL { 
            get
            {
                if (LoadMySQLDataFromElsewhere && (!_sqlElsewhere.Equals(default))) return _sqlElsewhere;
                else return Configuration.Instance.SQL;
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
            Data.LoadColoredConsole();
            F.Log("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.", ConsoleColor.Green);

            //F.SetPrivatePlayerCount(Config.MaxPlayerCount);
            if (Provider.clients.Count >= 24)
            {
                Provider.maxPlayers = Config.MaxPlayerCount;
            }
            F.Log("Set max player count to " + Provider.maxPlayers.ToString(), ConsoleColor.Magenta);

            F.Log("Patching methods...", ConsoleColor.Magenta);
            try
            {
                Patches.InternalPatches.DoPatching();
            }
            catch (Exception ex)
            {
                F.LogError("Patching Error, perhaps Nelson changed something:");
                F.LogError(ex);
            }

            StatsRoutine = StartCoroutine(StatsCoroutine.StatsRoutine());

            if(LoadMySQLDataFromElsewhere)
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
                } else
                {
                    string json = File.ReadAllText(Data.ElseWhereSQLPath);
                    _sqlElsewhere = JsonConvert.DeserializeObject<MySqlData>(json);
                }
            }
            Data.LoadVariables();
            if (Level.isLoaded)
            {
                //StartCheckingPlayers(Data.CancelFlags.Token).ConfigureAwait(false); // starts the function without awaiting
                SubscribeToEvents();
                OnLevelLoaded(2);
                InitialLoadEventSubscription = true;
            } else
            {
                InitialLoadEventSubscription = false;
                Level.onLevelLoaded += OnLevelLoaded;
                R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            }

            Provider.configData.Normal.Players.Lose_Items_PvP = 0;
            Provider.configData.Normal.Players.Lose_Items_PvE = 0;
            Provider.configData.Normal.Players.Lose_Clothes_PvP = false;
            Provider.configData.Normal.Players.Lose_Clothes_PvE = false;

            base.Load();
            UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        }
        private void OnLevelLoaded(int level)
        {
            F.CheckDir(Data.FlagStorage, out _, true);
            F.CheckDir(Data.StructureStorage, out _, true);
            F.CheckDir(Data.VehicleStorage, out _, true);
            Data.StructureManager = new StructureSaver();
            if (Config.Modules.VehicleSpawning)
            {
                Data.VehicleBay = new VehicleBay();
                Data.VehicleSpawner = new VehicleSpawner();
                Data.VehicleSigns = new VehicleSigns();
            }
            Announcer = gameObject.AddComponent<Components.UCAnnouncer>();
            Data.RequestSignManager = new RequestSigns();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            Data.ExtraZones = JSONMethods.LoadExtraZones();
            Data.TeamManager = new TeamManager();
            F.Log("Wiping unsaved barricades...", ConsoleColor.Magenta);
            ReplaceBarricadesAndStructures();
            Data.VehicleSpawner.OnLevelLoaded();
            FOBManager.LoadFobs();
            RepairManager.LoadRepairStations();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            Data.Gamemode.OnLevelLoaded();
            if (Provider.clients.Count > 0)
            {
                List<Players.FPlayerName> playersOnline = Provider.clients.Select(x => F.GetPlayerOriginalNames(x)).ToList();
                Networking.Client.SendPlayerList(playersOnline);
            }
        }

        public static void ReplaceBarricadesAndStructures()
        {
            try
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        try
                        {
                            for (int i = BarricadeManager.regions[x, y].drops.Count - 1; i >= 0; i--)
                            {
                                uint instid = BarricadeManager.regions[x, y].drops[i].instanceID;
                                if (!StructureSaver.StructureExists(instid, EStructType.BARRICADE, out _) && !RequestSigns.SignExists(instid, out _))
                                {
                                    if (BarricadeManager.regions[x, y].drops[i].model.transform.TryGetComponent(out InteractableStorage storage))
                                        storage.despawnWhenDestroyed = true;
                                    BarricadeManager.destroyBarricade(BarricadeManager.regions[x, y].drops[i], x, y, ushort.MaxValue);
                                }
                            }
                            for (int i = StructureManager.regions[x, y].drops.Count - 1; i >= 0; i--)
                            {
                                uint instid = StructureManager.regions[x, y].drops[i].instanceID;
                                if (!StructureSaver.StructureExists(instid, EStructType.STRUCTURE, out _) && !RequestSigns.SignExists(instid, out _))
                                    StructureManager.destroyStructure(StructureManager.regions[x, y].drops[i], x, y, Vector3.zero);
                            }
                        }
                        catch (Exception ex)
                        {
                            F.LogError($"Failed to clear barricades/structures of region ({x}, {y}):");
                            F.LogError(ex);
                        }
                    }
                }
                RequestSigns.DropAllSigns();
                StructureSaver.DropAllStructures();
            }
            catch (Exception ex)
            {
                F.LogError($"Failed to clear barricades/structures:");
                F.LogError(ex);
            }
        }
        private void SubscribeToEvents()
        {
            U.Events.OnPlayerConnected += EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected += EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
            Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
            //if (Networking.TCPClient.I != null) Networking.TCPClient.I.OnReceivedData += Networking.Client.ProcessResponse;
            Commands.LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
            Commands.ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
            BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned += EventFunctions.ThrowableSpawned;
            Patches.InternalPatches.OnLandmineExplode += EventFunctions.OnLandmineExploded;
            PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
            BarricadeManager.onBarricadeSpawned += EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler += EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler += EventFunctions.OnStructureDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
            Patches.OnPlayerGesture_Global += EventFunctions.OnPlayerGestureRequested;
            Patches.OnPlayerMarker_Global += EventFunctions.OnPlayerMarkedPosOnMap;
            DamageTool.damagePlayerRequested += EventFunctions.OnPlayerDamageRequested;
            PlayerInput.onPluginKeyTick += EventFunctions.OnPluginKeyPressed;
            EventFunctions.OnGroupChanged += EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested += EventFunctions.BarricadeMovedInWorkzone;
            BarricadeManager.onDamageBarricadeRequested += EventFunctions.OnBarricadeDamaged;
            StructureManager.onTransformRequested += EventFunctions.StructureMovedInWorkzone;
            BarricadeManager.onOpenStorageRequested += EventFunctions.OnEnterStorage;
            VehicleManager.onExitVehicleRequested += EventFunctions.OnPlayerLeavesVehicle;
            ItemManager.onServerSpawningItemDrop += EventFunctions.OnDropItemFinal;
            PlayerVoice.onRelayVoice += EventFunctions.OnRelayVoice;
        }
        private void UnsubscribeFromEvents()
        {
            Commands.ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
            U.Events.OnPlayerConnected -= EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected -= EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation -= EventFunctions.OnPrePlayerConnect;
            Provider.onBattlEyeKick += EventFunctions.OnBattleyeKicked;
            //if (Networking.TCPClient.I != null) Networking.TCPClient.I.OnReceivedData -= Networking.Client.ProcessResponse;
            Commands.LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
            BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned -= EventFunctions.ThrowableSpawned;
            Patches.InternalPatches.OnLandmineExplode -= EventFunctions.OnLandmineExploded;
            PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
            BarricadeManager.onBarricadeSpawned -= EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler -= EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler -= EventFunctions.OnStructureDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global -= EventFunctions.OnTryStoreItem;
            Patches.OnPlayerGesture_Global -= EventFunctions.OnPlayerGestureRequested;
            Patches.OnPlayerMarker_Global -= EventFunctions.OnPlayerMarkedPosOnMap;
            DamageTool.damagePlayerRequested -= EventFunctions.OnPlayerDamageRequested;
            PlayerInput.onPluginKeyTick -= EventFunctions.OnPluginKeyPressed;
            EventFunctions.OnGroupChanged -= EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested -= EventFunctions.BarricadeMovedInWorkzone;
            BarricadeManager.onDamageBarricadeRequested -= EventFunctions.OnBarricadeDamaged;
            StructureManager.onTransformRequested -= EventFunctions.StructureMovedInWorkzone;
            BarricadeManager.onOpenStorageRequested -= EventFunctions.OnEnterStorage;
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
                XP.XPManager.UpdateUI(player.player, XP.XPManager.GetXP(player.player, team, false), out _);
                Officers.OfficerManager.UpdateUI(player.player, Officers.OfficerManager.GetOfficerPoints(player.player, team, false), out _);
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
            Data.CancelFlags.Cancel();
            Data.CancelTcp.Cancel();
            Data.Gamemode?.Dispose();
            Data.DatabaseManager?.Dispose();
            Data.ReviveManager?.Dispose();
            Data.Whitelister?.Dispose();
            Data.SquadManager?.Dispose();
            Data.VehicleSpawner?.Dispose();
            F.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            F.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            //Networking.TCPClient.I?.Dispose();
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
