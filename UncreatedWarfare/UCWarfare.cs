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
using Newtonsoft.Json;
using Steamworks;

namespace UncreatedWarfare
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
                if (LoadMySQLDataFromElsewhere && (!_sqlElsewhere.Equals(null))) return _sqlElsewhere;
                else return Configuration.Instance.SQL;
            }
        }
        public const bool LoadMySQLDataFromElsewhere = true;
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
        public static readonly string ElseWhereSQLPath = @"C:\SteamCMD\unturned\Servers\UncreatedRewrite\sql.json";
        public Dictionary<string, Color> Colors;
        public Dictionary<string, string> ColorsHex;
        public Dictionary<string, Dictionary<string, string>> Localization;
        public Dictionary<EXPGainType, int> XPData;
        public Dictionary<ECreditsGainType, int> CreditsData;
        public Dictionary<int, Zone> ExtraZones;
        public Dictionary<string, Vector3> ExtraPoints;
        public Dictionary<string, MySqlTableLang> TableData;
        public Dictionary<ECall, string> NodeCalls;
        public Dictionary<ulong, string> DefaultPlayerNames;
        public Dictionary<ulong, FPlayerName> OriginalNames;
        public Dictionary<ulong, string> Languages;
        public Dictionary<string, LanguageAliasSet> LanguageAliases;
        private bool InitialLoadEventSubscription;
        internal Thread ListenerThread;
        internal AsyncListenServer ListenServer;
        internal AsyncDatabase DatabaseManager;
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
            //Patches.InternalPatches.DoPatching();

            if(LoadMySQLDataFromElsewhere)
            {
                if (!File.Exists(ElseWhereSQLPath))
                {
                    TextWriter w = File.CreateText(ElseWhereSQLPath);
                    JsonTextWriter wr = new JsonTextWriter(w);
                    JsonSerializer s = new JsonSerializer();
                    s.Formatting = Formatting.Indented;
                    s.Serialize(wr, Config.SQL);
                    wr.Close();
                    w.Close();
                    w.Dispose();
                    _sqlElsewhere = Config.SQL;
                } else
                {
                    string json = File.ReadAllText(ElseWhereSQLPath);
                    _sqlElsewhere = JsonConvert.DeserializeObject<MySqlData>(json);
                }
            }
            CommandWindow.Log("Validating directories...");
            CheckDir(DataDirectory);
            CheckDir(FlagStorage);
            CheckDir(LangStorage);
            CheckDir(KitsStorage);
            CheckDir(VehicleStorage);
            CheckDir(FOBStorage);
            CheckDir(TeamStorage);

            void DuplicateKeyError(Exception ex)
            {
                string[] stuff = ex.Message.Split(':');
                string badKey = "unknown";
                if (stuff.Length >= 2) badKey = stuff[1].Trim();
                CommandWindow.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
                Level.onLevelLoaded += (int level) =>
                {
                    CommandWindow.LogError("!!UNCREATED WARFARE DID NOT LOAD!!!");
                    CommandWindow.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
                };
                UnloadPlugin();
            }
            CommandWindow.Log("Loading JSON Data...");
            try
            {
                JSONMethods.CreateDefaultTranslations();
            } catch (TypeInitializationException ex)
            {
                DuplicateKeyError(ex);
                return;
            } catch (ArgumentException ex)
            {
                DuplicateKeyError(ex);
                return;
            }

            OriginalNames = new Dictionary<ulong, FPlayerName>();
            Colors = JSONMethods.LoadColors(out ColorsHex);
            XPData = JSONMethods.LoadXP();
            CreditsData = JSONMethods.LoadCredits();
            Localization = JSONMethods.LoadTranslations();
            ExtraZones = JSONMethods.LoadExtraZones();
            ExtraPoints = JSONMethods.LoadExtraPoints();
            TableData = JSONMethods.LoadTables();
            NodeCalls = JSONMethods.LoadCalls();
            Languages = JSONMethods.LoadLanguagePreferences();
            LanguageAliases = JSONMethods.LoadLangAliases();

            // Managers
            CommandWindow.Log("Instantiating Framework...");
            DatabaseManager = new AsyncDatabase();
            DatabaseManager.OpenAsync(AsyncDatabaseCallbacks.OpenedOnLoad);
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
                if(Configuration.Instance.SendAssetsOnStartup)
                    WebInterface.SendAssetUpdate();
                Log("Subscribing to events...");
                InitialLoadEventSubscription = true;
                U.Events.OnPlayerConnected += OnPostPlayerConnected;
                U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
                Provider.onCheckValidWithExplanation += OnPrePlayerConnect;
            } else
            {
                InitialLoadEventSubscription = false;
                Level.onLevelLoaded += OnLevelLoaded;
                R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            }
            base.Load();
            UCWarfareLoaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnPrePlayerConnect(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == callback.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            if (OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
        }

        private void OnLevelLoaded(int level)
        {
            CommandWindow.Log("Sending assets...");
            if (Configuration.Instance.SendAssetsOnStartup)
                WebInterface.SendAssetUpdate();
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
            U.Events.OnPlayerConnected += OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
        }
        private void OnPostPlayerConnected(UnturnedPlayer player)
        {
            F.Broadcast("player_connected", Colors["join_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["join_message_name"]);
            WebInterface?.SendPlayerJoinedAsync(player.Player.channel.owner);
            FPlayerName names;
            if (OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                names = OriginalNames[player.Player.channel.owner.playerID.steamID.m_SteamID];
            else names = new FPlayerName(player);
            DatabaseManager?.UpdateUsernameAsync(player.Player.channel.owner.playerID.steamID.m_SteamID, names);
        }
        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            F.Broadcast("player_disconnected", Colors["leave_message_background"], player.Player.channel.owner.playerID.playerName, ColorsHex["leave_message_name"]);
            WebInterface?.SendPlayerLeftAsync(player.Player.channel.owner);
        }

        protected override void Unload()
        {
            UCWarfareUnloading?.Invoke(this, EventArgs.Empty);

            IAsyncResult CloseSQLAsyncResult = DatabaseManager.CloseAsync(AsyncDatabaseCallbacks.ClosedOnUnload);
            WebInterface?.Dispose();
            FlagManager?.Dispose();
            DatabaseManager?.Dispose();
            CommandWindow.LogWarning("Unloading " + Name);
            CommandWindow.Log("Stopping Coroutines...");
            StopAllCoroutines();
            CommandWindow.Log("Unsubscribing from events...");
            U.Events.OnPlayerConnected -= OnPostPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            if(ListenServer != null) ListenServer.ListenerResultHeard -= ReceivedResponeFromListenServer;
            if (!InitialLoadEventSubscription)
            {
                Level.onLevelLoaded -= OnLevelLoaded;
                R.Plugins.OnPluginsLoaded -= OnPluginsLoaded;
            }
            try
            {
                CloseSQLAsyncResult.AsyncWaitHandle.WaitOne();
            }
            catch (ObjectDisposedException) { }
        }
    }
}
