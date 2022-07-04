using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uncreated.Homebase.Unturned.Warfare;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Stats;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Data
{
    public static class Paths
    {
        public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public static readonly string BaseDirectory = Path.Combine(Environment.CurrentDirectory, "Uncreated", "Warfare") + Path.DirectorySeparatorChar;
        private static string? mapCache;
        public static string MapStorage
        {
            get
            {
                if (mapCache is null)
                {
                    if (Provider.map == default) throw new Exception("Map not yet set.");
                    mapCache = Path.Combine(BaseDirectory, "Maps", Provider.map.RemoveMany(false, BAD_FILE_NAME_CHARACTERS)) + Path.DirectorySeparatorChar;
                }
                return mapCache;
            }
        }
        private static string? _flagCache;
        private static string? _structureCache;
        private static string? _vehicleCache;
        public static readonly string TicketStorage    = Path.Combine(BaseDirectory, "Tickets")   + Path.DirectorySeparatorChar;
        public static readonly string PointsStorage    = Path.Combine(BaseDirectory, "Points")    + Path.DirectorySeparatorChar;
        public static readonly string OfficerStorage   = Path.Combine(BaseDirectory, "Officers")  + Path.DirectorySeparatorChar;
        public static readonly string CooldownStorage  = Path.Combine(BaseDirectory, "Cooldowns") + Path.DirectorySeparatorChar;
        public static readonly string SquadStorage     = Path.Combine(BaseDirectory, "Squads")    + Path.DirectorySeparatorChar;
        public static readonly string KitsStorage      = Path.Combine(BaseDirectory, "Kits")      + Path.DirectorySeparatorChar;
        public static readonly string SQLStorage       = Path.Combine(BaseDirectory, "SQL")       + Path.DirectorySeparatorChar;
        public static readonly string FOBStorage       = Path.Combine(BaseDirectory, "FOBs")      + Path.DirectorySeparatorChar;
        public static readonly string LangStorage      = Path.Combine(BaseDirectory, "Lang")      + Path.DirectorySeparatorChar;
        public static readonly string Logs             = Path.Combine(BaseDirectory, "Logs")      + Path.DirectorySeparatorChar;
        public static readonly string ActionLog        = Path.Combine(Logs,          "ActionLog") + Path.DirectorySeparatorChar;
        public static readonly string PendingOffenses  = Path.Combine(BaseDirectory, "Offenses")  + Path.DirectorySeparatorChar;

        public static readonly string CurrentLog       = Path.Combine(Logs,          "current.txt");
        public static string FlagStorage        => _flagCache is null       ? (_flagCache       = Path.Combine(MapStorage, "Flags")      + Path.DirectorySeparatorChar) : _flagCache;
        public static string StructureStorage   => _structureCache is null  ? (_structureCache  = Path.Combine(MapStorage, "Structures") + Path.DirectorySeparatorChar) : _structureCache;
        public static string VehicleStorage     => _vehicleCache is null    ? (_vehicleCache    = Path.Combine(MapStorage, "Vehicles")   + Path.DirectorySeparatorChar) : _vehicleCache;
        public static void OnMapChanged()
        {
            mapCache = null;
            _flagCache = null;
            _structureCache = null;
            _vehicleCache = null;
        }
    }
    public static readonly Regex ChatFilter = new Regex(@"(?:[nV\|\\\/][il][gqb](?!h)\W{0,1}[gqb]{0,1}\W{0,1}[gqb]{0,1}\W{0,1}[ae]{0,1}\W{0,1}[r]{0,1}(?:ia){0,1})|(?:f\W{0,1}a\W{0,1}g{1,2}\W{0,1}o{0,1}\W{0,1}t{0,1})");
    public static readonly CultureInfo Locale = new CultureInfo("en-US");
    public static Dictionary<string, Color> Colors;
    public static Dictionary<string, string> ColorsHex;
    public static Dictionary<string, Dictionary<string, TranslationData>> Localization;
    public static Dictionary<string, Vector3> ExtraPoints;
    public static Dictionary<ulong, string> DefaultPlayerNames;
    public static Dictionary<ulong, FPlayerName> OriginalNames = new Dictionary<ulong, FPlayerName>();
    public static Dictionary<ulong, string> Languages;
    public static Dictionary<string, LanguageAliasSet> LanguageAliases;
    public static Dictionary<ulong, UCPlayerData> PlaytimeComponents = new Dictionary<ulong, UCPlayerData>();
    internal static JsonZoneProvider ZoneProvider;
    internal static WarfareSQL DatabaseManager;
    public static Gamemode Gamemode;
    public static bool TrackStats = true;
    public static bool Is<T>(out T gamemode) where T : Gamemodes.Interfaces.IGamemode
    {
        if (Gamemode is T gm)
        {
            gamemode = gm;
            return true;
        }
        gamemode = default!;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is<T>() where T : Gamemodes.Interfaces.IGamemode
    {
        return Gamemode is T;
    }
    internal static ClientInstanceMethod<string> SendChangeText { get; private set; }
    internal static ClientStaticMethod SendMultipleBarricades { get; private set; }
    internal static ClientStaticMethod SendEffectClearAll { get; private set; }
    internal static ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string> SendChatIndividual { get; private set; }
    internal static MethodInfo ReplicateStance;
    internal static FieldInfo PrivateStance;
    internal static FieldInfo ItemManagerInstanceCount;
    internal static ICommandInputOutput? defaultIOHandler;
    public static Reporter Reporter;
    public static DeathTracker DeathTracker;
    internal static HomebaseClient? NetClient;
    internal static ClientStaticMethod<byte, byte, uint> SendTakeItem;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearShirt;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearPants;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearHat;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearBackpack;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearVest;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearMask;
    internal static ClientInstanceMethod<Guid, byte, byte[]> SendWearGlasses;
    public static bool UseFastKits = false;
    internal static ClientInstanceMethod SendInventory;
    internal delegate void OutputToConsole(string value, ConsoleColor color);
    internal static OutputToConsole? OutputToConsoleMethod;
    internal static SingletonManager Singletons;

    internal static InstanceSetter<PlayerInventory, bool> SetOwnerHasInventory;
    internal static InstanceGetter<PlayerInventory, bool> GetOwnerHasInventory;
    internal static InstanceGetter<Items, bool[,]> GetItemsSlots;

    public static void LoadColoredConsole()
    {
        try
        {
            FieldInfo defaultIoHandlerFieldInfo = typeof(CommandWindow).GetField("defaultIOHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            if (defaultIoHandlerFieldInfo != null)
            {
                defaultIOHandler = (ICommandInputOutput)defaultIoHandlerFieldInfo.GetValue(Dedicator.commandWindow);
                MethodInfo appendConsoleMethod = defaultIOHandler.GetType().GetMethod("outputToConsole", BindingFlags.NonPublic | BindingFlags.Instance);
                if (appendConsoleMethod != null)
                {
                    OutputToConsoleMethod = (OutputToConsole)appendConsoleMethod.CreateDelegate(typeof(OutputToConsole), defaultIOHandler);
                    L.Log("Gathered IO Methods for Colored Console Messages", ConsoleColor.Magenta);
                    return;
                }
            }
            OutputToConsoleMethod = null;
        }
        catch (Exception ex)
        {
            CommandWindow.LogError("Couldn't get defaultIOHandler from CommandWindow:");
            CommandWindow.LogError(ex);
            OutputToConsoleMethod = null;
        }
    }
    public static void ReloadTCP()
    {
        if (UCWarfare.Config.TCPSettings.EnableTCPServer)
        {
            if (NetClient is not null)
            {
                try
                {
                    NetClient.Dispose();
                    NetClient.OnClientVerified -= OnClientConnected;
                    NetClient.OnClientDisconnected -= OnClientDisconnected;
                    NetClient.OnSentMessage -= OnClientSentMessage;
                    NetClient.OnReceivedMessage -= OnClientReceivedMessage;
                    GC.Collect();
                }
                catch { }
            }
            L.Log("Attempting a connection to a TCP server.", ConsoleColor.Magenta);
            NetClient = new HomebaseClient(UCWarfare.Config.TCPSettings.TCPServerIP, UCWarfare.Config.TCPSettings.TCPServerPort, UCWarfare.Config.TCPSettings.TCPServerIdentity);
            NetClient.OnClientVerified += OnClientConnected;
            NetClient.OnClientDisconnected += OnClientDisconnected;
            NetClient.OnSentMessage += OnClientSentMessage;
            NetClient.OnReceivedMessage += OnClientReceivedMessage;
        }
    }

    public static void LoadVariables()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singletons.OnSingletonLoaded    += OnSingletonLoaded;
        Singletons.OnSingletonUnloaded  += OnSingletonUnloaded;
        Singletons.OnSingletonReloaded  += OnSingletonReloaded;


        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

        /* CREATE DIRECTORIES */
        L.Log("Validating directories...", ConsoleColor.Magenta);
        F.CheckDir(Paths.BaseDirectory, out _, true);
        F.CheckDir(Paths.MapStorage, out _, true);
        F.CheckDir(Paths.LangStorage, out _, true);
        F.CheckDir(Paths.KitsStorage, out _, true);
        F.CheckDir(Paths.PointsStorage, out _, true);
        F.CheckDir(Paths.FOBStorage, out _, true);
        F.CheckDir(Paths.OfficerStorage, out _, true);

        ZoneProvider = new JsonZoneProvider(new FileInfo(Path.Combine(Paths.FlagStorage, "zones.json")));

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

        Quests.QuestManager.Init();

        Colors = JSONMethods.LoadColors(out ColorsHex);
        Localization = JSONMethods.LoadTranslations();
        Deaths.Localization.Reload();
        Languages = JSONMethods.LoadLanguagePreferences();
        LanguageAliases = JSONMethods.LoadLangAliases();

        Translation.ReadEnumTranslations(TranslatableEnumTypes);

        /* CONSTRUCT FRAMEWORK */
        L.Log("Instantiating Framework...", ConsoleColor.Magenta);
        DatabaseManager = new WarfareSQL(UCWarfare.Config.SQL);
        DatabaseManager.Open();
        Points.Initialize();
        CommandWindow.shouldLogDeaths = false;
        Gamemode.ReadGamemodes();

        if (!Gamemode.TryLoadGamemode(Gamemode.GetNextGamemode() ?? typeof(TeamCTF))) throw new SingletonLoadException(ESingletonLoadType.LOAD, null, new Exception("Failed to load gamemode"));
        ReloadTCP();

        if (UCWarfare.Config.EnableReporter)
            Reporter = UCWarfare.I.gameObject.AddComponent<Reporter>();


        DeathTracker = Singletons.LoadSingleton<DeathTracker>(false);

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

        UseFastKits = false;
        try
        {
            SendInventory = (typeof(PlayerInventory).GetField("SendInventory", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod)!;
        }
        catch (Exception ex)
        {
            UseFastKits |= true;
            L.LogWarning("Couldn't get SendInventory from PlayerInventory, kits will not work as quick. (" + ex.Message + ").");
        }

        try
        {
            SendWearShirt = (typeof(PlayerClothing).GetField("SendWearShirt", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearPants = (typeof(PlayerClothing).GetField("SendWearPants", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearHat = (typeof(PlayerClothing).GetField("SendWearHat", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearBackpack = (typeof(PlayerClothing).GetField("SendWearBackpack", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearVest = (typeof(PlayerClothing).GetField("SendWearVest", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearMask = (typeof(PlayerClothing).GetField("SendWearMask", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
            SendWearGlasses = (typeof(PlayerClothing).GetField("SendWearGlasses", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as ClientInstanceMethod<Guid, byte, byte[]>)!;
        }
        catch (Exception ex)
        {
            UseFastKits |= true;
            L.LogWarning("Couldn't get one of the SendWear______ methods from PlayerInventory, kits will not work as quick. (" + ex.Message + ").");
        }

        try
        {
            SetOwnerHasInventory = F.GenerateInstanceSetter<PlayerInventory, bool>("ownerHasInventory", BindingFlags.NonPublic);
            GetOwnerHasInventory = F.GenerateInstanceGetter<PlayerInventory, bool>("ownerHasInventory", BindingFlags.NonPublic);
            GetItemsSlots = F.GenerateInstanceGetter<Items, bool[,]>("slots", BindingFlags.NonPublic);
        }
        catch (Exception ex)
        {
            UseFastKits |= true;
            L.LogWarning("Couldn't generate a setter method for ownerHasInventory, kits will not work as quick. (" + ex.Message + ").");
            L.LogError(ex);
        }
        UseFastKits = !UseFastKits;

        /* REGISTER STATS MANAGER */
        StatsManager.LoadTeams();
        StatsManager.LoadWeapons();
        StatsManager.LoadKits();
        StatsManager.LoadVehicles();
        for (int i = 0; i < Provider.clients.Count; i++)
            StatsManager.RegisterPlayer(Provider.clients[i].playerID.steamID.m_SteamID);
        //Quests.DailyQuests.OnLoad();
    }

    private static void OnSingletonReloaded(IReloadableSingleton singleton, bool success)
    {
        if (success)
            L.Log("Singleton reloaded || " + singleton.GetType().Name, ConsoleColor.Blue);
        else
            L.LogWarning("Singleton failed to reload | " + singleton.GetType().Name, ConsoleColor.Red);
    }

    private static void OnSingletonUnloaded(IUncreatedSingleton singleton, bool success)
    {
        if (success)
            L.Log("Singleton unloaded \\\\ " + singleton.GetType().Name, ConsoleColor.Blue);
        else
            L.LogWarning("Singleton failed to unload | " + singleton.GetType().Name, ConsoleColor.Red);
    }

    private static void OnSingletonLoaded(IUncreatedSingleton singleton, bool success)
    {
        if (success)
            L.Log("Singleton loaded // " + singleton.GetType().Name, ConsoleColor.Blue);
        else
            L.LogWarning("Singleton failed to load | " + singleton.GetType().Name, ConsoleColor.Red);
    }

    internal static readonly List<KeyValuePair<Type, string?>> TranslatableEnumTypes = new List<KeyValuePair<Type, string?>>()
    {
        new KeyValuePair<Type, string?>(typeof(EDamageOrigin), "Damage Origin"), 
        new KeyValuePair<Type, string?>(typeof(EDeathCause), "Death Cause"),
        new KeyValuePair<Type, string?>(typeof(ELimb), "Limb")
    };
    private static void OnClientReceivedMessage(IConnection connection, byte[] message)
    {
        if (UCWarfare.Config.Debug)
        {
            L.Log("Received from TCP server on " + connection.Identity + ": " + string.Join(",", message), ConsoleColor.DarkGray);
        }
    }
    private static void OnClientSentMessage(IConnection connection, byte[] message)
    {
        if (UCWarfare.Config.Debug)
        {
            try
            {
                ushort id = BitConverter.ToUInt16(message, 0);
                //if (id != L.NetCalls.SendLogMessage.ID)
                //{
                L.Log("Sent over TCP server on " + connection.Identity + ": " + message.Length, ConsoleColor.DarkGray);
                //}
            }
            catch { }
        }
    }
    private static void OnClientDisconnected(IConnection connection)
    {
        L.Log("Disconnected from HomeBase.", ConsoleColor.DarkYellow);
    }
    private static void OnClientConnected(IConnection connection)
    {
        L.Log("Established a verified connection to HomeBase.", ConsoleColor.DarkYellow);
        PlayerManager.NetCalls.SendPlayerList.NetInvoke(PlayerManager.GetPlayerList());
        ActionLog.OnConnected();
        if (Gamemode.shutdownAfterGame)
            ShutdownCommand.NetCalls.SendShuttingDownAfter.NetInvoke(Gamemode.shutdownPlayer, Gamemode.shutdownMessage);
        Task.Run(OffenseManager.OnConnected).ConfigureAwait(false);
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
        throw new SingletonLoadException(ESingletonLoadType.LOAD, UCWarfare.I, ex);
    }
    public class NetCalls
    {
        public static readonly NetCallRaw<WarfareServerInfo> SendServerInfo = new NetCallRaw<WarfareServerInfo>(1008, WarfareServerInfo.Read, WarfareServerInfo.Write);
    }
}