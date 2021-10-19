using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Data
    {
        public const int MAX_LOGS = 1000;
        public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public static readonly Dictionary<string, Type> GAME_MODES = new Dictionary<string, Type>
        {
            { "TeamCTF", typeof(TeamCTF) }
        };
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string StatsDirectory = System.Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Players\";
        public static readonly string MatchDirectory = System.Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Matches\";
        private static readonly string _flagStorage = DataDirectory + @"Maps\{0}\Flags\";
        private static string _flagStorageTemp;
        public static string FlagStorage
        {
            get
            {
                if (Provider.map == default) return DataDirectory + @"Maps\Unloaded\Flags\";
                if (_flagStorageTemp == default)
                    _flagStorageTemp = string.Format(_flagStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _flagStorageTemp;
            }
        }
        private static readonly string _structuresStorage = DataDirectory + @"Maps\{0}\Structures\";
        private static string _structStorageTemp = null;
        public static string StructureStorage
        {
            get
            {
                if (Provider.map == default) return DataDirectory + @"Maps\Unloaded\Structures\";
                if (_structStorageTemp == default)
                    _structStorageTemp = string.Format(_structuresStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _structStorageTemp;
            }
        }
        public static readonly string TeamStorage = DataDirectory + @"Teams\";
        public static readonly string TicketStorage = DataDirectory + @"Tickets\";
        public static readonly string XPStorage = DataDirectory + @"XP\";
        public static readonly string OfficerStorage = DataDirectory + @"Officers\";
        public static readonly string CooldownStorage = DataDirectory + @"Cooldowns\";
        public static readonly string SquadStorage = DataDirectory + @"Squads\";
        public static readonly string KitsStorage = DataDirectory + @"Kits\";
        public static readonly string SQLStorage = DataDirectory + @"SQL\";
        private static readonly string _vehicleStorage = DataDirectory + @"Maps\{0}\Vehicles\";
        private static string _vehicleStorageTemp;
        public static string VehicleStorage
        {
            get
            {
                if (Provider.map == default) return DataDirectory + @"Maps\Unloaded\Vehicles\";
                if (_vehicleStorageTemp == default)
                    _vehicleStorageTemp = string.Format(_vehicleStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _vehicleStorageTemp;
            }
        }
        public static readonly string FOBStorage = DataDirectory + @"FOBs\";
        public static readonly string LangStorage = DataDirectory + @"Lang\";
        public static readonly string ElseWhereSQLPath = @"C:\sql.json";
        public static readonly CultureInfo Locale = new CultureInfo("en-US");
        public static Dictionary<string, Color> Colors;
        public static Dictionary<string, string> ColorsHex;
        public static Dictionary<string, Dictionary<string, TranslationData>> Localization;
        public static Dictionary<string, Dictionary<string, string>> DeathLocalization;
        public static Dictionary<string, Dictionary<ELimb, string>> LimbLocalization;
        public static Dictionary<int, Zone> ExtraZones;
        public static Dictionary<string, Vector3> ExtraPoints;
        public static Dictionary<string, MySqlTableLang> TableData;
        public static Dictionary<ulong, string> DefaultPlayerNames;
        public static Dictionary<ulong, FPlayerName> OriginalNames = new Dictionary<ulong, FPlayerName>();
        public static Dictionary<ulong, string> Languages;
        public static Dictionary<string, LanguageAliasSet> LanguageAliases;
        public static Dictionary<ulong, PlaytimeComponent> PlaytimeComponents = new Dictionary<ulong, PlaytimeComponent>();
        public static JoinManager JoinManager;
        public static KitManager KitManager;
        public static VehicleSpawner VehicleSpawner;
        public static VehicleBay VehicleBay;
        public static VehicleSigns VehicleSigns;
        public static FOBManager FOBManager;
        public static BuildManager BuildManager;
        public static TeamManager TeamManager;
        public static ReviveManager ReviveManager;
        public static TicketManager TicketManager;
        public static XPManager XPManager;
        public static OfficerManager OfficerManager;
        public static PlayerManager LogoutSaver;
        public static RequestSigns RequestSignManager;
        public static StructureSaver StructureManager;
        public static Whitelister Whitelister;
        public static SquadManager SquadManager;
        public static CooldownManager Cooldowns;
        internal static WarfareSQL DatabaseManager;
        public static Gamemode Gamemode;
        public static List<Log> Logs;
        public static TeamCTF CtfGamemode
        {
            get
            {
                if (Gamemode is TeamCTF ctf) return ctf;
                else return null;
            }
        }
        public static Gamemodes.Flags.FlagGamemode FlagGamemode
        {
            get
            {
                if (Gamemode is Gamemodes.Flags.FlagGamemode fg) return fg;
                else return null;
            }
        }
        internal static ClientInstanceMethod<string> SendChangeText { get; private set; }
        internal static ClientStaticMethod SendMultipleBarricades { get; private set; }
        internal static ClientStaticMethod SendEffectClearAll { get; private set; }
        internal static ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string> SendChatIndividual { get; private set; }
        internal static MethodInfo AppendConsoleMethod;
        internal static MethodInfo ReplicateStance;
        internal static FieldInfo PrivateStance;
        internal static FieldInfo ItemManagerInstanceCount;
        internal static ConsoleInputOutputBase defaultIOHandler;
        internal static Client NetClient;
        internal static ClientStaticMethod<byte, byte, uint> SendTakeItem;
        public static void LoadColoredConsole()
        {
            //CommandWindow.Log("Loading Colored Console Method");
            try
            {
                FieldInfo defaultIoHandlerFieldInfo = typeof(CommandWindow).GetField("defaultIOHandler", BindingFlags.Instance | BindingFlags.NonPublic);
                defaultIOHandler = (ConsoleInputOutputBase)defaultIoHandlerFieldInfo.GetValue(Dedicator.commandWindow);
                AppendConsoleMethod = defaultIOHandler.GetType().GetMethod("outputToConsole", BindingFlags.NonPublic | BindingFlags.Instance);
                F.Log("Gathered IO Methods for Colored Console Messages", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("Couldn't get defaultIOHandler from CommandWindow:");
                CommandWindow.LogError(ex);
                CommandWindow.LogError("The colored console will likely work in boring colors!");
            }
        }
        public static void ReloadTCP()
        {
            if (UCWarfare.Config.PlayerStatsSettings.EnableTCPServer)
            {
                if (NetClient != null)
                {
                    NetClient.connection.Close();
                    NetClient.Dispose();
                }
                F.Log("Attempting a connection to a TCP server.", ConsoleColor.Magenta);
                NetClient = new Client(UCWarfare.Config.PlayerStatsSettings.TCPServerIP, UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
                NetClient.AssertConnected();
                NetClient.connection.OnReceived += ClientReceived;
                NetClient.connection.OnAutoSent += ClientSent;
                Invocations.Shared.PlayerList.NetInvoke(PlayerManager.GetPlayerList());
            }
        }


        public static void LoadVariables()
        {
            /* INITIALIZE UNCREATED NETWORKING */
            Logging.OnLog += F.Log;
            Logging.OnLogWarning += F.LogWarning;
            Logging.OnLogError += F.LogError;
            Logging.OnLogException += F.LogError;
            NetFactory.RegisterNetMethods(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

            /* CREATE DIRECTORIES */
            F.Log("Validating directories...", ConsoleColor.Magenta);
            F.CheckDir(StatsDirectory, out _, true);
            F.CheckDir(DataDirectory, out _, true);
            F.CheckDir(LangStorage, out _, true);
            F.CheckDir(KitsStorage, out _, true);
            F.CheckDir(FOBStorage, out _, true);
            F.CheckDir(TeamStorage, out _, true);
            F.CheckDir(OfficerStorage, out _, true);

            /* LOAD LOCALIZATION ASSETS */
            F.Log("Loading JSON Data...", ConsoleColor.Magenta);
            try
            {
                JSONMethods.CreateDefaultTranslations();
            }
            catch (TypeInitializationException ex)
            {
                DuplicateKeyError(ex);
                return;
            }
            catch (ArgumentException ex)
            {
                DuplicateKeyError(ex);
                return;
            }

            Colors = JSONMethods.LoadColors(out ColorsHex);
            Localization = JSONMethods.LoadTranslations(out DeathLocalization, out LimbLocalization);
            Languages = JSONMethods.LoadLanguagePreferences();
            LanguageAliases = JSONMethods.LoadLangAliases();

            /* CONSTRUCT FRAMEWORK */
            F.Log("Instantiating Framework...", ConsoleColor.Magenta);
            DatabaseManager = new WarfareSQL(UCWarfare.I.SQL);
            DatabaseManager.Open();
            LogoutSaver = new PlayerManager();
            Whitelister = new Whitelister();
            SquadManager = new SquadManager();
            CommandWindow.shouldLogDeaths = false;
            TicketManager = new TicketManager();
            XPManager = new XPManager();
            OfficerManager = new OfficerManager();
            Cooldowns = new CooldownManager();
            JoinManager = UCWarfare.I.gameObject.AddComponent<JoinManager>();

            F.Log("Searching for gamemode: " + UCWarfare.Config.ActiveGamemode, ConsoleColor.Magenta);
            Gamemode = Gamemode.FindGamemode(UCWarfare.Config.ActiveGamemode, GAME_MODES);
            if (Gamemode == null)
            {
                F.LogError("Unable to find gamemode by the name " + UCWarfare.Config.ActiveGamemode + ", defaulting to " + nameof(TeamCTF));
                Gamemode = UCWarfare.I.gameObject.AddComponent<TeamCTF>();
            }
            Gamemode.Init();
            F.Log("Initialized gamemode.", ConsoleColor.Magenta);
            ReloadTCP();
            if (UCWarfare.Config.Modules.Kits)
            {
                KitManager = new KitManager();
            }
            if (UCWarfare.Config.Modules.FOBs)
            {
                FOBManager = new FOBManager();
                BuildManager = new BuildManager();
            }
            if (UCWarfare.Config.Modules.Revives)
            {
                ReviveManager = new ReviveManager();
            }

            /* REFLECT PRIVATE VARIABLES */
            F.Log("Getting client calls...", ConsoleColor.Magenta);
            FieldInfo updateSignInfo;
            FieldInfo sendRegionInfo;
            FieldInfo sendChatInfo;
            FieldInfo clearAllUiInfo;
            FieldInfo sendTakeItemInfo;
            try
            {
                updateSignInfo = typeof(InteractableSign).GetField("SendChangeText", BindingFlags.NonPublic | BindingFlags.Static);
                SendChangeText = (ClientInstanceMethod<string>)updateSignInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogError("Couldn't get SendUpdateSign from BarricadeManager:");
                F.LogError(ex);
                F.LogError("The sign translation system will likely not work!");
            }
            try
            {
                sendRegionInfo = typeof(BarricadeManager).GetField("SendMultipleBarricades", BindingFlags.NonPublic | BindingFlags.Static);
                SendMultipleBarricades = (ClientStaticMethod)sendRegionInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogError("Couldn't get SendMultipleBarricades from BarricadeManager:");
                F.LogError(ex);
                F.LogError("The sign translation system will likely not work!");
            }
            try
            {
                sendChatInfo = typeof(ChatManager).GetField("SendChatEntry", BindingFlags.NonPublic | BindingFlags.Static);
                SendChatIndividual = (ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>)sendChatInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get SendChatEntry from ChatManager, the chat message will default to the vanilla implementation. (" + ex.Message + ").");
            }
            try
            {
                clearAllUiInfo = typeof(EffectManager).GetField("SendEffectClearAll", BindingFlags.NonPublic | BindingFlags.Static);
                SendEffectClearAll = (ClientStaticMethod)clearAllUiInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get SendEffectClearAll from EffectManager, failed to get send effect clear all. (" + ex.Message + ").");
            }
            try
            {
                sendTakeItemInfo = typeof(ItemManager).GetField("SendTakeItem", BindingFlags.NonPublic | BindingFlags.Static);
                SendTakeItem = (ClientStaticMethod<byte, byte, uint>)sendTakeItemInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get SendTakeItem from ItemManager, ammo item clearing won't work. (" + ex.Message + ").");
            }
            try
            {
                ReplicateStance = typeof(PlayerStance).GetMethod("replicateStance", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get replicateState from PlayerStance, players will spawn while prone. (" + ex.Message + ").");
            }
            try
            {
                ItemManagerInstanceCount = typeof(ItemManager).GetField("instanceCount", BindingFlags.Static | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get instanceCount from ItemManager, ammo item clearing won't work. (" + ex.Message + ").");
            }
            try
            {
                PrivateStance = typeof(PlayerStance).GetField("_stance", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                F.LogWarning("Couldn't get state from PlayerStance, players will spawn while prone. (" + ex.Message + ").");
            }

            /* SET UP ROCKET GROUPS */
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup) == default)
                _ = R.Permissions.AddGroup(AdminOnDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup) == default)
                _ = R.Permissions.AddGroup(AdminOffDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup) == default)
                _ = R.Permissions.AddGroup(InternOnDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup) == default)
                _ = R.Permissions.AddGroup(InternOffDutyGroup);
            RocketPermissionsGroup defgroup = R.Permissions.GetGroup("default");
            if (defgroup == default)
                _ = R.Permissions.AddGroup(new RocketPermissionsGroup("default", "Guest", string.Empty, new List<string>(), DefaultPerms, priority: 1));
            else defgroup.Permissions = DefaultPerms;
            _ = R.Permissions.SaveGroup(defgroup);

            /* REGISTER STATS MANAGER */
            StatsManager.LoadTeams();
            StatsManager.LoadWeapons();
            StatsManager.LoadKits();
            StatsManager.LoadVehicles();
            for (int i = 0; i < Provider.clients.Count; i++)
                StatsManager.RegisterPlayer(Provider.clients[i].playerID.steamID.m_SteamID);
        }
        public static List<Log> ReadRocketLog()
        {
            List<Log> logs = new List<Log>();
            string path = Path.Combine(Rocket.Core.Environment.LogsDirectory, Rocket.Core.Environment.LogFile);
            if (!File.Exists(path))
                return logs;
            using (FileStream str = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] bytes = new byte[str.Length];
                str.Read(bytes, 0, bytes.Length);
                string file = Encoding.UTF8.GetString(bytes);
                string[] lines = file.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (logs.Count >= MAX_LOGS)
                    {
                        logs.RemoveRange(MAX_LOGS - 1, logs.Count - MAX_LOGS - 1);
                    }
                    logs.Insert(0, new Log(lines[i]));
                }
            }
            return logs;
        }
        public static void AddLog(Log log)
        {
            if (Logs.Count > MAX_LOGS)
            {
                Logs.RemoveRange(MAX_LOGS - 1, Logs.Count - MAX_LOGS + 1);
            }
            else if (Logs.Count == MAX_LOGS) Logs.RemoveAt(MAX_LOGS - 1);
            Logs.Insert(0, log);
        }
        private static void ClientReceived(byte[] bytes, IConnection connection)
        {
            if (UCWarfare.Config.Debug)
                F.Log("Received from TCP server on " + connection.Identity + ": " + string.Join(",", bytes), ConsoleColor.DarkGray);
        }
        private static void ClientSent(byte[] bytes, IConnection connection, ref bool Allow)
        {
            if (UCWarfare.Config.Debug)
                F.Log("Sent over TCP server on " + connection.Identity + ": " + bytes.Length, ConsoleColor.DarkGray);
        }
        private static void DuplicateKeyError(Exception ex)
        {
            string[] stuff = ex.Message.Split(':');
            string badKey = "unknown";
            if (stuff.Length >= 2) badKey = stuff[1].Trim();
            F.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
            F.LogError(ex);
            if (ex.InnerException != default)
                F.LogError(ex.InnerException);
            Level.onLevelLoaded += (int level) =>
            {
                F.LogError("!!UNCREATED WARFARE DID NOT LOAD!!!");
                F.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
            };
            UCWarfare.I.UnloadPlugin();
        }
        private static RocketPermissionsGroup AdminOnDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup,
                "Admin", "default", new List<string>(), AdminPerms, "00ffff", 100);
        }
        private static RocketPermissionsGroup AdminOffDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup,
                "Admin Off-Duty", "default", new List<string>(), new List<Permission> { new Permission("uc.duty") }, priority: 100);
        }

        private static RocketPermissionsGroup InternOnDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup,
                "Intern", "default", new List<string>(), TrialAdminPerms, "66ffff", 50);
        }
        private static RocketPermissionsGroup InternOffDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup,
                "Intern Off-Duty", "default", new List<string>(), new List<Permission> { new Permission("uc.duty") }, priority: 50);
        }

        private static List<Permission> AdminPerms
        {
            get =>
                new List<Permission>()
                {
                    new Permission("uc.duty"),
                    new Permission("uc.reload"),
                    new Permission("uc.test"),
                    new Permission("uc.ban"),
                    new Permission("uc.clear"),
                    new Permission("uc.group"),
                    new Permission("uc.group.current"),
                    new Permission("uc.group.create"),
                    new Permission("uc.group.join"),
                    new Permission("uc.join"),
                    new Permission("uc.kick"),
                    new Permission("uc.lang"),
                    new Permission("uc.rally"),
                    new Permission("uc.reload"),
                    new Permission("uc.reload.all"),
                    new Permission("uc.reload.translations"),
                    new Permission("uc.reload.flags"),
                    new Permission("uc.request"),
                    new Permission("uc.request.save"),
                    new Permission("uc.request.remove"),
                    new Permission("uc.structure"),
                    new Permission("uc.structure.save"),
                    new Permission("uc.structure.remove"),
                    new Permission("uc.structure.pop"),
                    new Permission("uc.structure.examine"),
                    new Permission("uc.unban"),
                    new Permission("uc.warn"),
                    new Permission("uc.whitelist"),
                    new Permission("uc.build"),
                    new Permission("uc.kit"),
                    new Permission("uc.ammo"),
                    new Permission("uc.squad"),
                    new Permission("uc.vehiclebay")
                };
        }
        private static List<Permission> TrialAdminPerms
        {
            get =>
                new List<Permission>()
                {
                    new Permission("uc.duty"),
                    new Permission("uc.reload"),
                    new Permission("uc.test"),
                    new Permission("uc.ban"),
                    new Permission("uc.clear"),
                    new Permission("uc.join"),
                    new Permission("uc.kick"),
                    new Permission("uc.lang"),
                    new Permission("uc.rally"),
                    new Permission("uc.request"),
                    new Permission("uc.structure"),
                    new Permission("uc.structure.pop"),
                    new Permission("uc.structure.examine"),
                    new Permission("uc.unban"),
                    new Permission("uc.warn"),
                    new Permission("uc.build"),
                    new Permission("uc.ammo"),
                    new Permission("uc.squad"),
                };
        }
        private static List<Permission> DefaultPerms
        {
            get =>
                new List<Permission>()
                {
                    new Permission("uc.request"),
                    new Permission("uc.join"),
                    new Permission("uc.range"),
                    new Permission("uc.repair"),
                    new Permission("uc.lang"),
                    new Permission("uc.discord"),
                    new Permission("uc.ammo"),
                    new Permission("uc.build"),
                    new Permission("uc.deploy"),
                    new Permission("uc.kits"),
                    new Permission("uc.squad"),
                    new Permission("uc.rally"),
                    new Permission("uc.group"),
                    new Permission("uc.group.current"),
                    new Permission("uc.rally")
                };
        }
    }
}