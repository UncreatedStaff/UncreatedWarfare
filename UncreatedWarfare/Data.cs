using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Stats;
using SDG.Unturned;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Kits;
using System.Threading;
using System.Reflection;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Squads;
using Rocket.Core;
using Rocket.API.Serialisation;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.XP;
using System.Globalization;

namespace Uncreated.Warfare
{
    public static class Data
    {
        public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string StatsDirectory = System.Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Players\";
        private static readonly string _flagStorage = DataDirectory + @"Maps\{0}\Flags\";
        private static string _flagStorageTemp;
        public static string FlagStorage {
            get
            {
                if(Level.level == default) return DataDirectory + @"Maps\Unloaded\Flags\";
                if (_flagStorageTemp == default)
                    _flagStorageTemp = string.Format(_flagStorage, Level.level.name.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _flagStorageTemp;
            } 
        }
        private static readonly string _structuresStorage = DataDirectory + @"Maps\{0}\Structures\";
        private static string _structStorageTemp = null;
        public static string StructureStorage
        {
            get
            {
                if (Level.level == default) return DataDirectory + @"Maps\Unloaded\Structures\";
                if (_structStorageTemp == default)
                    _structStorageTemp = string.Format(_structuresStorage, Level.level.name.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
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
                if (Level.level == default) return DataDirectory + @"Maps\Unloaded\Vehicles\";
                if (_vehicleStorageTemp == default)
                    _vehicleStorageTemp = string.Format(_vehicleStorage, Level.level.name.RemoveMany(false, BAD_FILE_NAME_CHARACTERS));
                return _vehicleStorageTemp;
            }
        }
        public static readonly string FOBStorage = DataDirectory + @"FOBs\";
        public static readonly string LangStorage = DataDirectory + @"Lang\";
        public static readonly string ElseWhereSQLPath = @"C:\sql.json";
        public static readonly CultureInfo Locale = new CultureInfo("en-US");
        public static Dictionary<string, Color> Colors;
        public static Dictionary<string, string> ColorsHex;
        public static Dictionary<string, Dictionary<string, string>> Localization;
        public static Dictionary<string, Dictionary<string, string>> DeathLocalization;
        public static Dictionary<string, Dictionary<ELimb, string>> LimbLocalization;
        public static Dictionary<EXPGainType, int> XPData;
        public static Dictionary<ECreditsGainType, int> CreditsData;
        public static Dictionary<int, Zone> ExtraZones;
        public static Dictionary<string, Vector3> ExtraPoints;
        public static Dictionary<string, MySqlTableLang> TableData;
        public static Dictionary<ulong, string> DefaultPlayerNames;
        public static Dictionary<ulong, FPlayerName> OriginalNames = new Dictionary<ulong, FPlayerName>();
        public static Dictionary<ulong, string> Languages;
        public static Dictionary<string, LanguageAliasSet> LanguageAliases;
        public static Dictionary<ulong, PlaytimeComponent> PlaytimeComponents = new Dictionary<ulong, PlaytimeComponent>();
        public static List<BarricadeOwnerDataComponent> OwnerComponents = new List<BarricadeOwnerDataComponent>();
        public static KitManager KitManager;
        public static VehicleSpawner VehicleSpawnSaver;
        public static VehicleBay VehicleBay; 
        public static FlagManager FlagManager;
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
        public static SquadManager squadManager;
        internal static WarfareSqlTest DatabaseManager;
        public static WarStatsTracker GameStats;
        internal static ClientStaticMethod<byte, byte, ushort, ushort, string> SendUpdateSign { get; private set; }
        internal static ClientStaticMethod SendMultipleBarricades { get; private set; }
        internal static MethodInfo AppendConsoleMethod;
        internal static ConsoleInputOutputBase defaultIOHandler;
        public static void LoadColoredConsole()
        {
            CommandWindow.Log("Loading Colored Console Method");
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
        public static async Task LoadVariables()
        {
            F.Log("Validating directories...", ConsoleColor.Magenta);
            F.CheckDir(StatsDirectory, out _, true);
            F.CheckDir(DataDirectory, out _, true);
            F.CheckDir(LangStorage, out _, true);
            F.CheckDir(KitsStorage, out _, true);
            F.CheckDir(FOBStorage, out _, true);
            F.CheckDir(TeamStorage, out _, true);
            F.CheckDir(OfficerStorage, out _, true);
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

            Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            XPData = JSONMethods.LoadXP();
            CreditsData = JSONMethods.LoadCredits();
            Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
            TableData = JSONMethods.LoadTables();
            Languages = JSONMethods.LoadLanguagePreferences();
            LanguageAliases = JSONMethods.LoadLangAliases();

            // Managers
            F.Log("Instantiating Framework...", ConsoleColor.Magenta);
            DatabaseManager = new WarfareSqlTest(UCWarfare.I.SQL);
            await DatabaseManager.OpenAsync();
            LogoutSaver = new PlayerManager();
            Whitelister = new Whitelister();
            squadManager = new SquadManager();
            CommandWindow.shouldLogDeaths = false;

            FlagManager = new FlagManager();
            FlagManager.OnReady += UCWarfare.I.OnFlagManagerReady;
            TicketManager = new TicketManager();
            XPManager = new XPManager();
            OfficerManager = new OfficerManager();
            if (UCWarfare.Config.PlayerStatsSettings.EnableTCPServer)
            {
                F.Log("Attempting a connection to a TCP server.", ConsoleColor.Magenta);
                Networking.TCPClient.I = new Networking.TCPClient(UCWarfare.Config.PlayerStatsSettings.TCPServerIP,
                    UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
            }
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
            F.Log("Getting client calls...", ConsoleColor.Magenta);
            Type barricadeManager = typeof(BarricadeManager);
            FieldInfo updateSignInfo;
            FieldInfo sendRegionInfo;
            try
            {
                updateSignInfo = barricadeManager.GetField("SendUpdateSign", BindingFlags.NonPublic | BindingFlags.Static);
                SendUpdateSign = (ClientStaticMethod<byte, byte, ushort, ushort, string>)updateSignInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogError("Couldn't get SendUpdateSign from BarricadeManager:");
                F.LogError(ex);
                F.LogError("The sign translation system will likely not work!");
            }
            try
            {
                sendRegionInfo = barricadeManager.GetField("SendMultipleBarricades", BindingFlags.NonPublic | BindingFlags.Static);
                SendMultipleBarricades = (ClientStaticMethod)sendRegionInfo.GetValue(null);
            }
            catch (Exception ex)
            {
                F.LogError("Couldn't get SendMultipleBarricades from BarricadeManager:");
                F.LogError(ex);
                F.LogError("The sign translation system will likely not work!");
            }

            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup) == default)
                R.Permissions.AddGroup(AdminOnDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup) == default)
                R.Permissions.AddGroup(AdminOffDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup) == default)
                R.Permissions.AddGroup(InternOnDutyGroup);
            if (R.Permissions.GetGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup) == default)
                R.Permissions.AddGroup(InternOffDutyGroup);
        }
        private static void DuplicateKeyError(Exception ex)
        {
            string[] stuff = ex.Message.Split(':');
            string badKey = "unknown";
            if (stuff.Length >= 2) badKey = stuff[1].Trim();
            F.LogError("\"" + badKey + "\" has a duplicate key in default translations, unable to load them. Unloading...");
            F.LogError(ex);
            if(ex.InnerException != default)
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
                "Admin", "default", new List<string>(), AdminPerms, "00ffff", 100)
                { Prefix = "[Admin] " };
        }
        private static RocketPermissionsGroup AdminOffDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup,
                "Admin Off-Duty", "default", new List<string>(), new List<Permission> { new Permission("uc.duty") }, priority: 100)
                { Prefix = "[Admin] " };
        }
        
        private static RocketPermissionsGroup InternOnDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup,
                "Intern", "default", new List<string>(), TrialAdminPerms, "66ffff", 50)
                { Prefix = "[Intern] " };
        }
        private static RocketPermissionsGroup InternOffDutyGroup
        {
            get =>
                new RocketPermissionsGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup,
                "Intern Off-Duty", "default", new List<string>(), new List<Permission> { new Permission("uc.duty") }, priority: 50)
                { Prefix = "[Intern] " };
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
    }
}