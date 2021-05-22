using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Components;
using UncreatedWarfare.Stats;
using SDG.Unturned;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UncreatedWarfare.FOBs;
using UncreatedWarfare.Revives;
using UncreatedWarfare.Vehicles;
using UncreatedWarfare.Kits;
using System.Threading;

namespace UncreatedWarfare
{
    public static class Data
    {
        public const string DataDirectory = @"Plugins\UncreatedWarfare\";
        public static readonly string FlagStorage = DataDirectory + @"Flags\Presets\";
        public static readonly string TeamStorage = DataDirectory + @"Teams\";
        public static readonly string KitsStorage = DataDirectory + @"Kits\";
        public static readonly string VehicleStorage = DataDirectory + @"Vehicles\";
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
        public static KitManager KitManager;
        public static VehicleBay VehicleBay;
        public static FlagManager FlagManager;
        public static TeamManager TeamManager;
        public static FOBManager FOBManager;
        public static BuildManager BuildManager;
        public static ReviveManager ReviveManager;
        public static WebInterface WebInterface;
        internal static Thread ListenerThread;
        internal static AsyncListenServer ListenServer;
        internal static AsyncDatabase DatabaseManager;
        public static WarStatsTracker GameStats;
        internal static readonly ClientStaticMethod<byte, byte, ushort, ushort, string> SendUpdateSign =
            ClientStaticMethod<byte, byte, ushort, ushort, string>.Get(
                new ClientStaticMethod<byte, byte, ushort, ushort, string>.ReceiveDelegate(BarricadeManager.ReceiveUpdateSign));
        internal static readonly ClientStaticMethod SendMultipleBarricades =
            ClientStaticMethod.Get(new ClientStaticMethod.ReceiveDelegateWithContext(BarricadeManager.ReceiveMultipleBarricades));
        internal static readonly ClientInstanceMethod SendScreenshotDestination =
            ClientInstanceMethod.Get(typeof(Player), "ReceiveScreenshotDestination");
    }
}