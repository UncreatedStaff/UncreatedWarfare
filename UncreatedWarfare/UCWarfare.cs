using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rocket.Core.Logging.Logger;
using System.IO;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UncreatedWarfare.Kits;
using UncreatedWarfare.Vehicles;
using UncreatedWarfare.FOBs;
using UnityEngine;
using Rocket.Core;
using Rocket.Unturned;
using UncreatedWarfare.Stats;
using UncreatedWarfare.Revives;
using System.Threading;
using Rocket.Unturned.Player;

namespace UncreatedWarfare
{
    public partial class UCWarfare : RocketPlugin<Config>
    {
        public static UCWarfare Instance;
        public static UCWarfare I { get => Instance; }
        public static Config Config { get => Instance.Configuration.Instance; }
        public event EventHandler UCWarfareLoaded;
        public event EventHandler UCWarfareUnloading;
        public KitManager KitManager;
        public VehicleBay VehicleBay;
        public FlagManager FlagManager;
        public TeamManager TeamManager;
        public FOBManager FOBManager;
        public BuildManager BuildManager;
        public ReviveManager reviveManager;
        public WebInterface WebInterface;
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string FlagStorage = DataDirectory + @"Flags\Presets\";
        public static readonly string TeamStorage = DataDirectory + @"Teams\";
        public static readonly string KitsStorage = DataDirectory + @"Kits\";
        public static readonly string VehicleStorage = DataDirectory + @"Vehicles\";
        public static readonly string FOBStorage = DataDirectory + @"FOBs\";
        public static readonly string LangStorage = DataDirectory + @"Lang\";
        public Dictionary<string, Color> Colors;
        public Dictionary<string, string> ColorsHex;
        public Dictionary<string, string> Localization;
        public Dictionary<EXPGainType, int> XPData;
        public Dictionary<ECreditsGainType, int> CreditsData;
        public Dictionary<int, Zone> ExtraZones;
        public Dictionary<string, Vector3> ExtraPoints;
        public Dictionary<string, MySqlTableLang> TableData;
        public Dictionary<ECall, string> NodeCalls;
        public DatabaseManager DB { get; private set; }
        private bool InitialLoadEventSubscription;
        internal Thread ListenerThread;
        internal AsyncListenServer ListenServer;
        private void CheckDir(string path)
        {
            if (!System.IO.Directory.Exists(path))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    CommandWindow.Log("Created directory: \"" + path + "\".");
                }
                catch (Exception ex)
                {
                    CommandWindow.LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                    UnloadPlugin();
                }
            }
        }
        protected override void Load()
        {
            Coroutines = new List<IEnumerator<WaitForSeconds>> { CheckPlayers() };
            CommandWindow.LogWarning("Started loading " + Name + " - By BlazingFlame and 420DankMeister. If this is not running on an official Uncreated Server than it has been obtained illigimately. " +
                "Please stop using this plugin now.");
            Instance = this;

            CommandWindow.Log("Patching methods...");
            Patches.InternalPatches.DoPatching();

            CommandWindow.Log("Validating directories...");
            CheckDir(DataDirectory);
            CheckDir(FlagStorage);
            CheckDir(LangStorage);
            CheckDir(KitsStorage);
            CheckDir(VehicleStorage);
            CheckDir(TeamStorage);
            CheckDir(FOBStorage);

            CommandWindow.Log("Loading JSON Data...");
            Colors = JSONMethods.LoadColors(out ColorsHex);
            XPData = JSONMethods.LoadXP();
            CreditsData = JSONMethods.LoadCredits();
            Localization = JSONMethods.LoadTranslations();
            ExtraZones = JSONMethods.LoadExtraZones();
            ExtraPoints = JSONMethods.LoadExtraPoints();
            TableData = JSONMethods.LoadTables();
            NodeCalls = JSONMethods.LoadCalls();

            // Managers
            CommandWindow.Log("Instantiating Framework...");
            DB = new DatabaseManager();
            WebInterface = new WebInterface();

            ListenerThread = new Thread(StartListening);

            TeamManager = new TeamManager();

            if (Config.Modules.Flags)
            {
                FlagManager = new FlagManager(Config.FlagSettings.CurrentGamePreset);
            }
            if (Config.Modules.Kits)
            {
                KitManager = new KitManager();
            }
            if (Config.Modules.VehicleSpawning)
            {
                VehicleBay = new VehicleBay();
            }
            if (Config.Modules.FOBs)
            {
                FOBManager = new FOBManager();
                BuildManager = new BuildManager();
            }
            if (Config.Modules.Revives)
            {
                reviveManager = new ReviveManager();
            }

            CommandWindow.Log("Starting Coroutines...");
            if (Level.isLoaded)
            {
                StartAllCoroutines();
                CommandWindow.Log("Sending assets...");
                WebInterface.SendAssetUpdate();
                Log("Subscribing to events...");
                InitialLoadEventSubscription = true;
                U.Events.OnPlayerConnected += OnPlayerConnected;
                U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
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
            CommandWindow.Log("Sending assets...");
            //WebInterface.SendAssetUpdate();
        }

        private void StartListening()
        {
            ListenServer = new AsyncListenServer();
            ListenServer.ListenerResultHeard += ReceivedResponeFromListenServer;
            ListenServer.StartListening();
        }

        private void OnPluginsLoaded()
        {
            StartAllCoroutines();
            Log("Subscribing to events...");
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
        }
        private void OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            F.Broadcast("player_connected", Colors["join_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["join_message_name"]);
            WebInterface?.SendPlayerJoinedAsync(player.Player.channel.owner);
        }
        private void OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            F.Broadcast("player_disconnected", Colors["leave_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["leave_message_name"]);
            WebInterface?.SendPlayerLeftAsync(player.Player.channel.owner);
        }

        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            WebInterface?.Dispose();
            FlagManager?.Dispose();
            CommandWindow.LogWarning("Unloading " + Name);
            CommandWindow.Log("Stopping Coroutines...");
            StopAllCoroutines();
            CommandWindow.Log("Unsubscribing from events...");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            if(ListenServer != null) ListenServer.ListenerResultHeard -= ReceivedResponeFromListenServer;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
            //DB.Close();
        }   

    }
}
