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
            Coroutines = new List<IEnumerator<WaitForSeconds>> { CheckPlayers() };
            Instance = this;

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
            Data.LoadVariables();
            if (Level.isLoaded)
            {
                StartAllCoroutines();
                SubscribeToEvents();
                OnLevelLoaded(2);
                InitialLoadEventSubscription = true;
            } else
            {
                InitialLoadEventSubscription = false;
                Level.onLevelLoaded += OnLevelLoaded;
                R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            }
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
                Data.VehicleSpawnSaver = new VehicleSpawner();
                Data.VehicleBay = new VehicleBay();
            }
            Data.VehicleBay.FirstSpawn();
            Data.RequestSignManager = new RequestSigns();
            Data.StructureManager = new StructureSaver();
            Data.ExtraPoints = JSONMethods.LoadExtraPoints();
            Data.ExtraZones = JSONMethods.LoadExtraZones();
            if (Configuration.Instance.SendAssetsOnStartup)
            {
                F.Log("Sending assets...", ConsoleColor.Magenta);
                Data.WebInterface.SendAssetUpdate();
            }
            Data.TeamManager = new TeamManager();
            F.Log("Wiping barricades then replacing important ones...", ConsoleColor.Magenta);
            ReplaceBarricadesAndStructures();
            Data.FlagManager.Load(); // starts new game
            VehicleBay.StartAllActive();
            Data.GameStats = gameObject.AddComponent<WarStatsTracker>();
        }
        public static void ReplaceBarricadesAndStructures()
        {
            BarricadeManager.askClearAllBarricades();
            StructureManager.askClearAllStructures();
            RequestSigns.DropAllSigns();
            StructureSaver.DropAllStructures();
        }
        internal void OnFlagManagerReady(object sender, EventArgs e)
        {
            Data.FlagManager.StartNextGame();
        }
        private void SubscribeToEvents()
        {
            U.Events.OnPlayerConnected += EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid += EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected += EventFunctions.OnPlayerDisconnected;
            Provider.onCheckValidWithExplanation += EventFunctions.OnPrePlayerConnect;
            Commands.LangCommand.OnPlayerChangedLanguage += EventFunctions.LangCommand_OnPlayerChangedLanguage;
            Commands.ReloadCommand.OnTranslationsReloaded += EventFunctions.ReloadCommand_onTranslationsReloaded;
            BarricadeManager.onDeployBarricadeRequested += EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            UseableGun.onBulletSpawned += EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned += EventFunctions.ProjectileSpawned;
            UseableThrowable.onThrowableSpawned += EventFunctions.ThrowableSpawned;
            Patches.InternalPatches.OnLandmineExplode += EventFunctions.OnLandmineExploded;
            Provider.onLoginSpawning += EventFunctions.OnCalculateSpawnDuringLogin;
            PlayerLife.OnSelectingRespawnPoint += EventFunctions.OnCalculateSpawnDuringRevive;
            Patches.BarricadeSpawnedHandler += EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler += EventFunctions.OnBarricadeDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global += EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global += EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global += EventFunctions.BatteryStolen;
            EventFunctions.OnGroupChanged += EventFunctions.GroupChangedAction;
        }
        private void UnsubscribeFromEvents()
        {
            Commands.ReloadCommand.OnTranslationsReloaded -= EventFunctions.ReloadCommand_onTranslationsReloaded;
            U.Events.OnPlayerConnected -= EventFunctions.OnPostPlayerConnected;
            UseableConsumeable.onPerformedAid -= EventFunctions.OnPostHealedPlayer;
            U.Events.OnPlayerDisconnected -= EventFunctions.OnPlayerDisconnected;
            if (Data.ListenServer != null) Data.ListenServer.OnMessageReceived -= ReceivedResponeFromListenServer;
            Commands.LangCommand.OnPlayerChangedLanguage -= EventFunctions.LangCommand_OnPlayerChangedLanguage;
            BarricadeManager.onDeployBarricadeRequested -= EventFunctions.OnBarricadeTryPlaced;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            UseableGun.onBulletSpawned -= EventFunctions.BulletSpawned;
            UseableGun.onProjectileSpawned -= EventFunctions.ProjectileSpawned;
            Patches.InternalPatches.OnLandmineExplode -= EventFunctions.OnLandmineExploded;
            Provider.onLoginSpawning -= EventFunctions.OnCalculateSpawnDuringLogin;
            PlayerLife.OnSelectingRespawnPoint -= EventFunctions.OnCalculateSpawnDuringRevive;
            Patches.BarricadeSpawnedHandler -= EventFunctions.OnBarricadePlaced;
            Patches.BarricadeDestroyedHandler -= EventFunctions.OnBarricadeDestroyed;
            Patches.OnPlayerTogglesCosmetics_Global -= EventFunctions.StopCosmeticsToggleEvent;
            Patches.OnPlayerSetsCosmetics_Global -= EventFunctions.StopCosmeticsSetStateEvent;
            Patches.OnBatterySteal_Global -= EventFunctions.BatteryStolen;
            EventFunctions.OnGroupChanged -= EventFunctions.GroupChangedAction;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
        }
        private void OnPluginsLoaded()
        {
            StartAllCoroutines();
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
                    if (drop.model.TryGetComponent(out InteractableSign sign))
                    {
                        if (sign.text.StartsWith("sign_"))
                        {
                            if (BarricadeManager.tryGetInfo(drop.model, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion _))
                                F.InvokeSignUpdateFor(player, x, y, plant, index, region, false); 
                        }
                    }
                }
            }
            
        }
        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            IAsyncResult CloseSQLAsyncResult = Data.DatabaseManager.CloseAsync(AsyncDatabaseCallbacks.ClosedOnUnload);
            F.Log("Unloading " + Name, ConsoleColor.Magenta);
            Data.WebInterface?.Dispose();
            Data.FlagManager?.Dispose();
            Data.DatabaseManager?.Dispose();
            Data.ReviveManager?.Dispose();
            Data.FOBManager?.Dispose();
            Data.Whitelister?.Dispose();
            F.Log("Stopping Coroutines...", ConsoleColor.Magenta);
            StopAllCoroutines();
            F.Log("Unsubscribing from events...", ConsoleColor.Magenta);
            UnsubscribeFromEvents();
            CommandWindow.shouldLogDeaths = true;
            try
            {
                if(CloseSQLAsyncResult != default && CloseSQLAsyncResult.AsyncWaitHandle != default)
                    CloseSQLAsyncResult.AsyncWaitHandle.WaitOne();
            }
            catch (ObjectDisposedException) { }
            Data.ListenServer?.Dispose();
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
