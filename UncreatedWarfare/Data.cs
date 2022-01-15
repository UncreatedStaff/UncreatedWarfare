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
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Data
    {
        public const int MAX_LOGS = 1000;
        public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public const string DATA_DIRECTORY = @"Plugins\UncreatedWarfare\";
        public static readonly string StatsDirectory = System.Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Players\";
        public static readonly string MatchDirectory = System.Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Matches\";
        private static readonly string _flagStorage = DATA_DIRECTORY + @"Maps\{0}\Flags\";
        private static string _flagStorageTemp;
        public static string FlagStorage
        {
            get
            {
                if (Provider.map == default) return DATA_DIRECTORY + @"Maps\Unloaded\Flags\";
                if (_flagStorageTemp == default)
                    _flagStorageTemp = string.Format(_flagStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _flagStorageTemp;
            }
        }
        private static readonly string _structuresStorage = DATA_DIRECTORY + @"Maps\{0}\Structures\";
        private static string _structStorageTemp = null;
        public static string StructureStorage
        {
            get
            {
                if (Provider.map == default) return DATA_DIRECTORY + @"Maps\Unloaded\Structures\";
                if (_structStorageTemp == default)
                    _structStorageTemp = string.Format(_structuresStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _structStorageTemp;
            }
        }
        public static readonly string TeamStorage = DATA_DIRECTORY + @"Teams\";
        public static readonly string TicketStorage = DATA_DIRECTORY + @"Tickets\";
        public static readonly string PointsStorage = DATA_DIRECTORY + @"Points\";
        public static readonly string OfficerStorage = DATA_DIRECTORY + @"Officers\";
        public static readonly string CooldownStorage = DATA_DIRECTORY + @"Cooldowns\";
        public static readonly string SquadStorage = DATA_DIRECTORY + @"Squads\";
        public static readonly string KitsStorage = DATA_DIRECTORY + @"Kits\";
        public static readonly string SQLStorage = DATA_DIRECTORY + @"SQL\";
        private static readonly string _vehicleStorage = DATA_DIRECTORY + @"Maps\{0}\Vehicles\";
        private static string _vehicleStorageTemp;
        public static string VehicleStorage
        {
            get
            {
                if (Provider.map == default) return DATA_DIRECTORY + @"Maps\Unloaded\Vehicles\";
                if (_vehicleStorageTemp == default)
                    _vehicleStorageTemp = string.Format(_vehicleStorage, Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _vehicleStorageTemp;
            }
        }
        public static readonly string FOBStorage = DATA_DIRECTORY + @"FOBs\";
        public static readonly string LangStorage = DATA_DIRECTORY + @"Lang\";
        public static readonly string ElseWhereSQLPath = @"C:\sql.json";
        public static readonly CultureInfo Locale = new CultureInfo("en-US");
        public static Dictionary<string, Color> Colors;
        public static Dictionary<string, string> ColorsHex;
        public static Dictionary<string, Dictionary<string, TranslationData>> Localization;
        public static Dictionary<string, Dictionary<string, string>> DeathLocalization;
        public static Dictionary<string, Dictionary<ELimb, string>> LimbLocalization;
        public static Dictionary<int, Zone> ExtraZones;
        public static Dictionary<string, Vector3> ExtraPoints;
        public static Dictionary<ulong, string> DefaultPlayerNames;
        public static Dictionary<ulong, FPlayerName> OriginalNames = new Dictionary<ulong, FPlayerName>();
        public static Dictionary<ulong, string> Languages;
        public static Dictionary<string, LanguageAliasSet> LanguageAliases;
        public static Dictionary<ulong, PlaytimeComponent> PlaytimeComponents = new Dictionary<ulong, PlaytimeComponent>();
        internal static WarfareSQL DatabaseManager;
        public static Gamemode Gamemode;
        public static List<Log> Logs;
        public static bool TrackStats = true;
        public static bool Is<T>(out T gamemode) where T : Gamemodes.Interfaces.IGamemode
        {
            if (Gamemode is T gm)
            {
                gamemode = gm;
                return true;
            }
            gamemode = default;
            return false;
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
        public static Reporter Reporter;
        internal static Client NetClient;
        internal static ClientStaticMethod<byte, byte, uint> SendTakeItem;
        public static void LoadColoredConsole()
        {
            try
            {
                FieldInfo defaultIoHandlerFieldInfo = typeof(CommandWindow).GetField("defaultIOHandler", BindingFlags.Instance | BindingFlags.NonPublic);
                defaultIOHandler = (ConsoleInputOutputBase)defaultIoHandlerFieldInfo.GetValue(Dedicator.commandWindow);
                AppendConsoleMethod = defaultIOHandler.GetType().GetMethod("outputToConsole", BindingFlags.NonPublic | BindingFlags.Instance);
                L.Log("Gathered IO Methods for Colored Console Messages", ConsoleColor.Magenta);
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
                L.Log("Attempting a connection to a TCP server.", ConsoleColor.Magenta);
                NetClient = new Client(UCWarfare.Config.PlayerStatsSettings.TCPServerIP, UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
                NetClient.AssertConnected();
                NetClient.connection.OnReceived += ClientReceived;
                NetClient.connection.OnAutoSent += ClientSent;
                Invocations.Shared.PlayerList.NetInvoke(PlayerManager.GetPlayerList());
            }
        }
        public static void LoadRandomGamemode()
        {

        }

        public static void LoadVariables()
        {
            /* INITIALIZE UNCREATED NETWORKING */
            Logging.OnLog += L.Log;
            Logging.OnLogWarning += L.LogWarning;
            Logging.OnLogError += L.LogError;
            Logging.OnLogException += L.LogError;
            NetFactory.RegisterNetMethods(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

            /* CREATE DIRECTORIES */
            L.Log("Validating directories...", ConsoleColor.Magenta);
            F.CheckDir(StatsDirectory, out _, true);
            F.CheckDir(DATA_DIRECTORY, out _, true);
            F.CheckDir(LangStorage, out _, true);
            F.CheckDir(KitsStorage, out _, true);
            F.CheckDir(FOBStorage, out _, true);
            F.CheckDir(TeamStorage, out _, true);
            F.CheckDir(OfficerStorage, out _, true);

            /* LOAD LOCALIZATION ASSETS */
            L.Log("Loading JSON Data...", ConsoleColor.Magenta);
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
            L.Log("Instantiating Framework...", ConsoleColor.Magenta);
            DatabaseManager = new WarfareSQL(UCWarfare.I.SQL);
            DatabaseManager.Open();
            Points.Initialize();
            CommandWindow.shouldLogDeaths = false;
            Gamemode.ReadGamemodes();

            Type nextMode = Gamemode.GetNextGamemode();
            if (nextMode == null) nextMode = typeof(TeamCTF);
            Gamemode = UCWarfare.I.gameObject.AddComponent(nextMode) as Gamemode;
            if (Gamemode != null)
            {
                Gamemode.Init();
                for (int i = 0; i < Provider.clients.Count; i++)
                    Gamemode.OnPlayerJoined(UCPlayer.FromSteamPlayer(Provider.clients[i]), true);
                L.Log("Loaded " + Gamemode.DisplayName, ConsoleColor.Cyan);
                L.Log("Initialized gamemode.", ConsoleColor.Magenta);
            } else
            {
                L.LogError("Failed to Initialize Gamemode");
            }
            ReloadTCP();

            Reporter = UCWarfare.I.gameObject.AddComponent<Reporter>();

            /* REFLECT PRIVATE VARIABLES */
            L.Log("Getting client calls...", ConsoleColor.Magenta);
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
                L.LogError("Couldn't get SendUpdateSign from BarricadeManager:");
                L.LogError(ex);
                L.LogError("The sign translation system will likely not work!");
            }
            try
            {
                sendRegionInfo = typeof(BarricadeManager).GetField("SendMultipleBarricades", BindingFlags.NonPublic | BindingFlags.Static);
                SendMultipleBarricades = (ClientStaticMethod)sendRegionInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                L.LogError("Couldn't get SendMultipleBarricades from BarricadeManager:");
                L.LogError(ex);
                L.LogError("The sign translation system will likely not work!");
            }
            try
            {
                sendChatInfo = typeof(ChatManager).GetField("SendChatEntry", BindingFlags.NonPublic | BindingFlags.Static);
                SendChatIndividual = (ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>)sendChatInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get SendChatEntry from ChatManager, the chat message will default to the vanilla implementation. (" + ex.Message + ").");
            }
            try
            {
                clearAllUiInfo = typeof(EffectManager).GetField("SendEffectClearAll", BindingFlags.NonPublic | BindingFlags.Static);
                SendEffectClearAll = (ClientStaticMethod)clearAllUiInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get SendEffectClearAll from EffectManager, failed to get send effect clear all. (" + ex.Message + ").");
            }
            try
            {
                sendTakeItemInfo = typeof(ItemManager).GetField("SendTakeItem", BindingFlags.NonPublic | BindingFlags.Static);
                SendTakeItem = (ClientStaticMethod<byte, byte, uint>)sendTakeItemInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get SendTakeItem from ItemManager, ammo item clearing won't work. (" + ex.Message + ").");
            }
            try
            {
                ReplicateStance = typeof(PlayerStance).GetMethod("replicateStance", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get replicateState from PlayerStance, players will spawn while prone. (" + ex.Message + ").");
            }
            try
            {
                ItemManagerInstanceCount = typeof(ItemManager).GetField("instanceCount", BindingFlags.Static | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get instanceCount from ItemManager, ammo item clearing won't work. (" + ex.Message + ").");
            }
            try
            {
                PrivateStance = typeof(PlayerStance).GetField("_stance", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            catch (Exception ex)
            {
                L.LogWarning("Couldn't get state from PlayerStance, players will spawn while prone. (" + ex.Message + ").");
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
            L.LogDebug("Received from TCP server on " + connection.Identity + ": " + string.Join(",", bytes));
        }
        private static void ClientSent(byte[] bytes, IConnection connection, ref bool Allow)
        {
            if (UCWarfare.Config.Debug)
            {
                try
                {
                    ushort id = BitConverter.ToUInt16(bytes, 0);
                    if (id != Invocations.Shared.SendLogMessage.ID)
                        L.Log("Sent over TCP server on " + connection.Identity + ": " + bytes.Length, ConsoleColor.DarkGray);
                } 
                catch { }
            }
        }
        private static void DuplicateKeyError(Exception ex)
        {
            string[] stuff = ex.Message.Split(':');
            string badKey = "unknown";
            if (stuff.Length >= 2) badKey = stuff[1].Trim();
            L.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
            L.LogError(ex);
            if (ex.InnerException != default)
                L.LogError(ex.InnerException);
            Level.onLevelLoaded += (int level) =>
            {
                L.LogError("!!UNCREATED WARFARE DID NOT LOAD!!!");
                L.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
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
                    new Permission("uc.rally"),
                    new Permission("uc.teams")
                };
        }
    }
}