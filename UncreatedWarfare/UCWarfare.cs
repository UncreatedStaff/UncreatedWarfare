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

namespace Uncreated.Warfare
{
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
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
        public const bool LoadMySQLDataFromElsewhere = true;
        public event EventHandler UCWarfareLoaded;
        public event EventHandler UCWarfareUnloading;
        public bool CoroutineTiming = false;
        private bool InitialLoadEventSubscription;
        protected override void Load()
        {
            ThreadTool.SetGameThread();
            Instance = this;
            Data.LoadColoredConsole();
            F.Log("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.", ConsoleColor.Green);

            F.Log("Patching methods...", ConsoleColor.Magenta);
            Patches.InternalPatches.DoPatching();
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
            Data.LoadVariables().GetAwaiter().GetResult();
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
        private async void OnLevelLoaded(int level)
        {
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            F.CheckDir(Data.FlagStorage, out _, true);
            F.CheckDir(Data.StructureStorage, out _, true);
            F.CheckDir(Data.VehicleStorage, out _, true);
            if (Config.Modules.VehicleSpawning)
            {
                Data.VehicleSpawner = new VehicleSpawner();
                Data.VehicleBay = new VehicleBay();
                Data.VehicleSigns = new VehicleSigns();
            }
            Data.RequestSignManager = new RequestSigns();
            Data.StructureManager = new StructureSaver();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            Data.ExtraZones = JSONMethods.LoadExtraZones();
            Data.TeamManager = new TeamManager();
            F.Log("Wiping barricades then replacing important ones...", ConsoleColor.Magenta);
            await ReplaceBarricadesAndStructures();
            Data.VehicleSpawner.OnLevelLoaded();
            FOBManager.LoadFobs();
            RepairManager.LoadRepairStations();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            await Data.Gamemode.OnLevelLoaded();
            await rtn;
            if (Provider.clients.Count > 0)
            {
                List<Players.FPlayerName> playersOnline = new List<Players.FPlayerName>();
                Provider.clients.ForEach(x => playersOnline.Add(F.GetPlayerOriginalNames(x)));
                await Networking.Client.SendPlayerList(playersOnline);
            }
        }
        public static async Task ReplaceBarricadesAndStructures()
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    for (ushort i = 0; i < BarricadeManager.regions[x, y].barricades.Count; i++)
                    {
                        uint instid = BarricadeManager.regions[x, y].barricades[i].instanceID;
                        if (!StructureSaver.StructureExists(instid, EStructType.BARRICADE, out _) && !RequestSigns.SignExists(instid, out _))
                        {
                            if (BarricadeManager.regions[x, y].drops[i].model.transform.TryGetComponent(out InteractableStorage storage))
                                storage.despawnWhenDestroyed = true;
                            BarricadeManager.destroyBarricade(BarricadeManager.regions[x, y], x, y, ushort.MaxValue, i);
                        }
                    }
                    for (ushort i = 0; i < StructureManager.regions[x, y].structures.Count; i++)
                    {
                        uint instid = StructureManager.regions[x, y].structures[i].instanceID;
                        if (!StructureSaver.StructureExists(instid, EStructType.STRUCTURE, out _) && !RequestSigns.SignExists(instid, out _))
                            StructureManager.destroyStructure(StructureManager.regions[x, y], x, y, i, Vector3.zero);
                    }
                }
            }
            for (ushort vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                for (ushort i = 0; i < BarricadeManager.vehicleRegions[vr].barricades.Count; i++)
                {
                    uint instid = BarricadeManager.vehicleRegions[vr].barricades[i].instanceID;
                    if (!StructureSaver.StructureExists(instid, EStructType.BARRICADE, out _) && !RequestSigns.SignExists(instid, out _))
                    {
                        if (BarricadeManager.vehicleRegions[vr].drops[i].model.transform.TryGetComponent(out InteractableStorage storage))
                            storage.despawnWhenDestroyed = true;
                        BarricadeManager.destroyBarricade(BarricadeManager.vehicleRegions[vr], 0, 0, vr, i);
                    }
                }
            }
            await RequestSigns.DropAllSigns();
            await StructureSaver.DropAllStructures();
        }
        private void SubscribeToEvents()
        {
            U.Events.OnPlayerConnected += EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected += EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
            if(Networking.TCPClient.I != null) Networking.TCPClient.I.OnReceivedData += Networking.Client.ProcessResponse;
            Commands.LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
            Commands.ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
            BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned += EventFunctions.ThrowableSpawned;
            Patches.InternalPatches.OnLandmineExplode += EventFunctions.OnLandmineExploded;
            PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
            Patches.BarricadeSpawnedHandler += EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler += EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler += EventFunctions.OnStructureDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
            EventFunctions.OnGroupChanged += EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested += EventFunctions.BarricadeMovedInWorkzone;
            StructureManager.onTransformRequested += EventFunctions.StructureMovedInWorkzone;
            VehicleManager.onExitVehicleRequested += EventFunctions.OnPlayerLeavesVehicle;
        }
        private void UnsubscribeFromEvents()
        {
            Commands.ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
            U.Events.OnPlayerConnected -= EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected -= EventFunctions.OnPlayerDisconnected;
            if (Networking.TCPClient.I != null) Networking.TCPClient.I.OnReceivedData -= Networking.Client.ProcessResponse;
            Commands.LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
            BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
            Patches.InternalPatches.OnLandmineExplode -= EventFunctions.OnLandmineExploded;
            PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
            Patches.BarricadeSpawnedHandler -= EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler -= EventFunctions.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler -= EventFunctions.OnStructureDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
            Patches.OnPlayerTriedStoreItem_Global += EventFunctions.OnTryStoreItem;
            EventFunctions.OnGroupChanged -= EventFunctions.GroupChangedAction;
            BarricadeManager.onTransformRequested -= EventFunctions.BarricadeMovedInWorkzone;
            StructureManager.onTransformRequested -= EventFunctions.StructureMovedInWorkzone;
            VehicleManager.onExitVehicleRequested -= EventFunctions.OnPlayerLeavesVehicle;
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
        internal async Task UpdateLangs(SteamPlayer player)
        {
            foreach (BarricadeRegion region in BarricadeManager.regions)
            {
                List<BarricadeDrop> signs = new List<BarricadeDrop>();
                foreach (BarricadeDrop drop in region.drops)
                {
                    if (drop.model.TryGetComponent(out InteractableSign sign))
                    {
                        if (sign.text.StartsWith("sign_"))
                        {
                            if (BarricadeManager.tryGetInfo(drop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion _))
                                await F.InvokeSignUpdateFor(player, x, y, plant, index, region, false); 
                        }
                    }
                }
            }
            
        }
        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);
            F.Log("Unloading " + Name, ConsoleColor.Magenta);
            Data.CancelFlags.Cancel();
            Data.CancelTcp.Cancel();
            Data.Gamemode?.Dispose();
            Data.DatabaseManager?.Dispose();
            Data.ReviveManager?.Dispose();
            Data.Whitelister?.Dispose();
            Data.VehicleSpawner?.Dispose();
            F.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            F.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            Networking.TCPClient.I?.Dispose();
        }
        public static Color GetColor(string key)
        {
            if (Data.Colors == null) return Color.white;
            if (Data.Colors.ContainsKey(key)) return Data.Colors[key];
            else if (Data.Colors.ContainsKey("default")) return Data.Colors["default"];
            else return Color.white;
        }
        public static string GetColorHex(string key)
        {
            if (Data.ColorsHex == null) return "ffffff";
            if (Data.ColorsHex.ContainsKey(key)) return Data.ColorsHex[key];
            else if (Data.ColorsHex.ContainsKey("default")) return Data.ColorsHex["default"];
            else return "ffffff";
        }
    }
}
