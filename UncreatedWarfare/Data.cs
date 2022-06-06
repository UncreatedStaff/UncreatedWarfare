using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
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
    public static readonly char[] BAD_FILE_NAME_CHARACTERS = new char[] { '>', ':', '"', '/', '\\', '|', '?', '*' };
    public static readonly string DATA_DIRECTORY = System.Environment.CurrentDirectory + @"\Plugins\UncreatedWarfare\";
    private static readonly string _flagStorage = DATA_DIRECTORY + @"Maps\{0}\Flags\";
    private static string? _flagStorageTemp;
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
    private static string? _structStorageTemp = null;
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
    private static string? _vehicleStorageTemp;
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
    public static readonly string LOG_DIRECTORY = System.Environment.CurrentDirectory + @"\Logs\ActionLogs\";
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
    internal delegate void OutputToConsole(string value, ConsoleColor color);
    internal static OutputToConsole? OutputToConsoleMethod;
    internal static SingletonManager Singletons;
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
                }
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError("Couldn't get defaultIOHandler from CommandWindow:");
            CommandWindow.LogError(ex);
        }
    }
    public static void ReloadTCP()
    {
        if (UCWarfare.Config.PlayerStatsSettings.EnableTCPServer)
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
            NetClient = new HomebaseClient(UCWarfare.Config.PlayerStatsSettings.TCPServerIP, UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
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
        UCWarfare.I.gameObject.AddComponent<ActionLog>();
        Singletons = UCWarfare.I.gameObject.AddComponent<SingletonManager>();
        Singletons.OnSingletonLoaded    += OnSingletonLoaded;
        Singletons.OnSingletonUnloaded  += OnSingletonUnloaded;
        Singletons.OnSingletonReloaded  += OnSingletonReloaded;


        ActionLog.Add(EActionLogType.SERVER_STARTUP, $"Name: {Provider.serverName}, Map: {Provider.map}, Max players: {Provider.maxPlayers.ToString(Locale)}");

        /* INITIALIZE UNCREATED NETWORKING */
        Logging.OnLogInfo += L.NetLogInfo;
        Logging.OnLogWarning += L.NetLogWarning;
        Logging.OnLogError += L.NetLogError;
        Logging.OnLogException += L.NetLogException;
        NetFactory.Reflect(Assembly.GetExecutingAssembly(), ENetCall.FROM_SERVER);

        /* CREATE DIRECTORIES */
        L.Log("Validating directories...", ConsoleColor.Magenta);
        F.CheckDir(DATA_DIRECTORY, out _, true);
        F.CheckDir(LangStorage, out _, true);
        F.CheckDir(KitsStorage, out _, true);
        F.CheckDir(PointsStorage, out _, true);
        F.CheckDir(FOBStorage, out _, true);
        F.CheckDir(TeamStorage, out _, true);
        F.CheckDir(OfficerStorage, out _, true);

        ZoneProvider = new JsonZoneProvider(new FileInfo(FlagStorage + "zones.json"));

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
        DatabaseManager = new WarfareSQL(UCWarfare.I.SQL);
        DatabaseManager.Open();
        Points.Initialize();
        CommandWindow.shouldLogDeaths = false;
        Gamemode.ReadGamemodes();

        if (!Gamemode.TryLoadGamemode(Gamemode.GetNextGamemode() ?? typeof(TeamCTF))) throw new SingletonLoadException(ESingletonLoadType.LOAD, null, new Exception("Failed to load gamemode"));
        ReloadTCP();

        if (UCWarfare.Config.EnableReporter)
        {
            Reporter = UCWarfare.I.gameObject.AddComponent<Reporter>();
        }


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
                if (id != L.NetCalls.SendLogMessage.ID)
                {
                    L.Log("Sent over TCP server on " + connection.Identity + ": " + message.Length, ConsoleColor.DarkGray);
                }
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
            ShutdownOverrideCommand.NetCalls.SendShuttingDownAfter.NetInvoke(Gamemode.shutdownPlayer, Gamemode.shutdownMessage);
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
                new Permission("uc.mute"),
                new Permission("uc.unmute"),
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
                new Permission("uc.mute"),
                new Permission("uc.unmute"),
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
                new Permission("uc.teams"),
                new Permission("uc.unstuck"),
                new Permission("uc.confirm"),
                new Permission("uc.buy"),
                new Permission("uc.report"),
                new Permission("uc.structure"),
                new Permission("uc.structure.examine")
            };
    }
}