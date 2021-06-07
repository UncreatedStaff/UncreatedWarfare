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

namespace Uncreated.Warfare
{
    public static class Data
    {
        public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string StatsDirectory = Environment.GetEnvironmentVariable("APPDATA") + @"\Uncreated\Players\";
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
        public static readonly string KitsStorage = DataDirectory + @"Kits\";
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
        public static readonly string ElseWhereSQLPath = @"C:\SteamCMD\unturned\Servers\UncreatedRewrite\sql.json";
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
        public static Dictionary<ECall, string> NodeCalls;
        public static Dictionary<ulong, string> DefaultPlayerNames;
        public static Dictionary<ulong, FPlayerName> OriginalNames = new Dictionary<ulong, FPlayerName>();
        public static Dictionary<ulong, string> Languages;
        public static Dictionary<string, LanguageAliasSet> LanguageAliases;
        public static Dictionary<ulong, PlaytimeComponent> PlaytimeComponents = new Dictionary<ulong, PlaytimeComponent>();
        public static List<BarricadeOwnerDataComponent> OwnerComponents = new List<BarricadeOwnerDataComponent>();
        public static List<UncreatedPlayer> Online = new List<UncreatedPlayer>();
        public static KitManager KitManager;
        public static VehicleSpawner VehicleSpawnSaver;
        public static VehicleBay VehicleBay;
        public static FlagManager FlagManager;
        public static TeamManager TeamManager;
        public static FOBManager FOBManager;
        public static BuildManager BuildManager;
        public static ReviveManager ReviveManager;
        public static LogoutSaver LogoutSaver;
        public static WebInterface WebInterface;
        public static RequestSigns RequestSignManager;
        public static StructureSaver StructureManager;
        public static Whitelister Whitelister;
        internal static Thread ListenerThread;
        internal static AsyncListenServer ListenServer;
        internal static AsyncDatabase DatabaseManager;
        public static WarStatsTracker GameStats;
        internal static ClientStaticMethod<byte, byte, ushort, ushort, string> SendUpdateSign { get; private set; }
        internal static ClientStaticMethod SendMultipleBarricades { get; private set; }
        internal static MethodInfo AppendConsoleMethod;
        internal static ConsoleInputOutputBase defaultIOHandler;
        public static void LoadVariables()
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
            F.Log("Validating directories...", ConsoleColor.Magenta);
            F.CheckDir(StatsDirectory, out _, true);
            F.CheckDir(DataDirectory, out _, true);
            F.CheckDir(LangStorage, out _, true);
            F.CheckDir(KitsStorage, out _, true);
            F.CheckDir(FOBStorage, out _, true);
            F.CheckDir(TeamStorage, out _, true);
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
            NodeCalls = JSONMethods.LoadCalls();
            Languages = JSONMethods.LoadLanguagePreferences();
            LanguageAliases = JSONMethods.LoadLangAliases();

            // Managers
            F.Log("Instantiating Framework...", ConsoleColor.Magenta);
            DatabaseManager = new AsyncDatabase();
            DatabaseManager.OpenAsync(AsyncDatabaseCallbacks.OpenedOnLoad);
            WebInterface = new WebInterface();
            LogoutSaver = new LogoutSaver();
            Whitelister = new Whitelister();
            CommandWindow.shouldLogDeaths = false;


            FlagManager = new FlagManager();
            FlagManager.OnReady += UCWarfare.I.OnFlagManagerReady;
            if (UCWarfare.Config.PlayerStatsSettings.EnableListenServer)
            {
                ListenerThread = new Thread(StartListening);
                ListenerThread.Name = "UCWarfareListenServer";
                ListenerThread.IsBackground = true;
                ListenerThread.Start();
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
        }
        private static void StartListening()
        {
            ListenServer = new AsyncListenServer();
            ListenServer.OnMessageReceived += UCWarfare.I.ReceivedResponeFromListenServer;
            ListenServer.Start();
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
    }
}