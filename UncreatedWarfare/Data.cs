
//#define SHOW_BYTES

using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SDG.NetTransport;
using Uncreated.Framework;
using Uncreated.Homebase.Unturned.Warfare;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using UnityEngine;
using UnityEngine.Assertions;

namespace Uncreated.Warfare;

public static class Data
{
    public static class Paths
    {
        public static readonly char[] BadFileNameCharacters = { '>', ':', '"', '/', '\\', '|', '?', '*' };
        public static readonly string BaseDirectory = Path.Combine(Environment.CurrentDirectory, "Uncreated", "Warfare") + Path.DirectorySeparatorChar;
        private static string? _mapCache;
        public static string MapStorage
        {
            get
            {
                if (_mapCache is null)
                {
                    if (Provider.map == default) throw new Exception("Map not yet set.");
                    _mapCache = Path.Combine(BaseDirectory, "Maps", Provider.map.RemoveMany(false, BadFileNameCharacters)) + Path.DirectorySeparatorChar;
                }
                return _mapCache;
            }
        }
        private static string? _flagCache;
        private static string? _structureCache;
        private static string? _vehicleCache;
        public static readonly string FactionsStorage = Path.Combine(BaseDirectory, "Factions") + Path.DirectorySeparatorChar;
        public static readonly string TicketStorage = Path.Combine(BaseDirectory, "Tickets") + Path.DirectorySeparatorChar;
        public static readonly string PointsStorage = Path.Combine(BaseDirectory, "Points") + Path.DirectorySeparatorChar;
        public static readonly string OfficerStorage = Path.Combine(BaseDirectory, "Officers") + Path.DirectorySeparatorChar;
        public static readonly string CooldownStorage = Path.Combine(BaseDirectory, "Cooldowns") + Path.DirectorySeparatorChar;
        public static readonly string SquadStorage = Path.Combine(BaseDirectory, "Squads") + Path.DirectorySeparatorChar;
        public static readonly string KitsStorage = Path.Combine(BaseDirectory, "Kits") + Path.DirectorySeparatorChar;
        public static readonly string SQLStorage = Path.Combine(BaseDirectory, "SQL") + Path.DirectorySeparatorChar;
        public static readonly string FOBStorage = Path.Combine(BaseDirectory, "FOBs") + Path.DirectorySeparatorChar;
        public static readonly string LangStorage = Path.Combine(BaseDirectory, "Lang") + Path.DirectorySeparatorChar;
        public static readonly string Logs = Path.Combine(BaseDirectory, "Logs") + Path.DirectorySeparatorChar;
        public static readonly string Sync = Path.Combine(BaseDirectory, "Sync") + Path.DirectorySeparatorChar;
        public static readonly string ActionLog = Path.Combine(Logs, "ActionLog") + Path.DirectorySeparatorChar;
        public static readonly string PendingOffenses = Path.Combine(BaseDirectory, "Offenses") + Path.DirectorySeparatorChar;
        public static readonly string TraitDataStorage = Path.Combine(BaseDirectory, "traits.json");
        public static readonly string ConfigSync = Path.Combine(Sync, "config.json");
        public static readonly string KitSync = Path.Combine(Sync, "kits.json");
        public static readonly string PlayersSync = Path.Combine(Sync, "players.json");
        public static readonly string CurrentLog = Path.Combine(Logs, "current.txt");
        public static string FlagStorage => _flagCache ??= Path.Combine(MapStorage, "Flags") + Path.DirectorySeparatorChar;
        public static string StructureStorage => _structureCache ??= Path.Combine(MapStorage, "Structures") + Path.DirectorySeparatorChar;
        public static string VehicleStorage => _vehicleCache ??= Path.Combine(MapStorage, "Vehicles") + Path.DirectorySeparatorChar;
        public static void OnMapChanged()
        {
            _mapCache = null;
            _flagCache = null;
            _structureCache = null;
            _vehicleCache = null;
        }
    }
    public static class Keys
    {
        public const PlayerKey GiveUp = PlayerKey.PluginKey3;
        public const PlayerKey SelfRevive = PlayerKey.PluginKey2;
        public const PlayerKey SpawnCountermeasures = PlayerKey.PluginKey3;
        public const PlayerKey ActionMenu = PlayerKey.PluginKey2;
        public const PlayerKey DropSupplyOverride = PlayerKey.PluginKey3;
    }

    public const string SuppressCategory = "Microsoft.Performance";
    public const string SuppressID = "IDE0051";
    public static readonly Regex ChatFilter = new Regex(@"(?:[nV\|\\\/][il][gqb](?!h)\W{0,1}[gqb]{0,1}\W{0,1}[gqb]{0,1}\W{0,1}[ae]{0,1}\W{0,1}[r]{0,1}(?:ia){0,1})|(?:f\W{0,1}a\W{0,1}g{1,2}\W{0,1}o{0,1}\W{0,1}t{0,1})");
    [Obsolete("Choose between LocalLocale and AdminLocale")]
    public static CultureInfo Locale = LanguageAliasSet.ENGLISH_C;
    public static CultureInfo LocalLocale = LanguageAliasSet.ENGLISH_C; // todo set from config
    public static readonly CultureInfo AdminLocale = LanguageAliasSet.ENGLISH_C;
    public static Dictionary<string, Color> Colors;
    public static Dictionary<string, string> ColorsHex;
    public static Dictionary<string, Vector3> ExtraPoints;
    public static Dictionary<ulong, string> DefaultPlayerNames;
    public static Dictionary<ulong, string> Languages;
    public static Dictionary<ulong, PlayerNames> OriginalPlayerNames = new Dictionary<ulong, PlayerNames>(Provider.maxPlayers);
    public static List<LanguageAliasSet> LanguageAliases;
    public static Dictionary<ulong, UCPlayerData> PlaytimeComponents = new Dictionary<ulong, UCPlayerData>();
    internal static WarfareSQL DatabaseManager;
    internal static WarfareSQL? RemoteSQL;
    public static Gamemode Gamemode;
    public static bool TrackStats = true;
    public static bool UseFastKits;
    public static bool UseElectricalGrid;
    internal static MethodInfo ReplicateStance;
    public static Reporter? Reporter;
    public static DeathTracker DeathTracker;
    internal static ClientStaticMethod<byte, byte, uint, bool> SendDestroyItem;
    internal static ClientInstanceMethod<byte[]>? SendUpdateBarricadeState;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearShirt;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearPants;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearHat;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearBackpack;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearVest;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearMask;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearGlasses;
    internal static ClientInstanceMethod<string> SendChangeText;
    internal static ClientStaticMethod SendMultipleBarricades;
    internal static ClientStaticMethod SendEffectClearAll;
    internal static ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string> SendChatIndividual;
    internal static ClientInstanceMethod? SendInventory;
    internal static SingletonManager Singletons;
    internal static InstanceSetter<PlayerStance, EPlayerStance> SetPrivateStance;
    internal static InstanceSetter<PlayerInventory, bool> SetOwnerHasInventory;
    internal static InstanceSetter<InteractableStorage, Items> SetStorageInventory;
    internal static InstanceGetter<PlayerInventory, bool> GetOwnerHasInventory;
    internal static InstanceGetter<Items, bool[,]> GetItemsSlots;
    internal static StaticGetter<uint> GetItemManagerInstanceCount;
    internal static Action<Vector3, Vector3, string, Transform?, IEnumerable<ITransportConnection>>? ServerSpawnLegacyImpact;
    [OperationTest(DisplayName = "Fast Kits Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestFastKits()
    {
        Assert.IsTrue(UseFastKits);
    }
    [OperationTest(DisplayName = "ServerSpawnLegacyImpact Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestServerSpawnLegacyImpact()
    {
        Assert.IsNotNull(ServerSpawnLegacyImpact);
    }
    [OperationTest(DisplayName = "RPC Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestRPCs()
    {
        Assert.IsNotNull(SendUpdateBarricadeState);
        Assert.IsNotNull(SendDestroyItem);
        Assert.IsNotNull(SendChangeText);
        Assert.IsNotNull(SendMultipleBarricades);
        Assert.IsNotNull(SendEffectClearAll);
        Assert.IsNotNull(SendChatIndividual);
    }
    [OperationTest(DisplayName = "Generated Getters and Setters Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestGettersAndSetters()
    {
        Assert.IsNotNull(SetPrivateStance);
        Assert.IsNotNull(SetOwnerHasInventory);
        Assert.IsNotNull(GetItemManagerInstanceCount);
        Assert.IsNotNull(ReplicateStance);
    }
    public static bool IsInitialSyncRegistering { get; private set; } = true;
    public static WarfareSQL AdminSql => RemoteSQL ?? DatabaseManager;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is<TGamemode>(out TGamemode gamemode) where TGamemode : class, IGamemode => (gamemode = (Gamemode as TGamemode)!) is not null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is<TGamemode>() where TGamemode : class, IGamemode => Gamemode is TGamemode;
    
    internal static async Task LoadSQL(CancellationToken token)
    {
        DatabaseManager = new WarfareSQL(UCWarfare.Config.SQL);
        bool status = await DatabaseManager.OpenAsync(token);
        L.Log("Local MySql database status: " + status + ".", ConsoleColor.Magenta);
        if (UCWarfare.Config.RemoteSQL != null)
        {
            RemoteSQL = new WarfareSQL(UCWarfare.Config.RemoteSQL);
            status = await RemoteSQL.OpenAsync(token);
            L.Log("Remote MySql database status: " + status + ".", ConsoleColor.Magenta);
        }
        else
            L.Log("Using local as remote MySql database.", ConsoleColor.Magenta);
    }
    internal static async Task LoadVariables(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singletons.OnSingletonLoaded += OnSingletonLoaded;
        Singletons.OnSingletonUnloaded += OnSingletonUnloaded;
        Singletons.OnSingletonReloaded += OnSingletonReloaded;


        /* CREATE DIRECTORIES */
        L.Log("Validating directories...", ConsoleColor.Magenta);
        F.CheckDir(Paths.BaseDirectory, out _, true);
        F.CheckDir(Paths.MapStorage, out _, true);
        F.CheckDir(Paths.LangStorage, out _, true);
        F.CheckDir(Paths.KitsStorage, out _, true);
        F.CheckDir(Paths.PointsStorage, out _, true);
        F.CheckDir(Paths.FOBStorage, out _, true);
        F.CheckDir(Paths.OfficerStorage, out _, true);

        ZoneList l = await Singletons.LoadSingletonAsync<ZoneList>(token: token);
        L.Log("Read " + l.Items.Count + " zones.", ConsoleColor.Magenta);

        /* CONSTRUCT FRAMEWORK */
        L.Log("Instantiating Framework...", ConsoleColor.Magenta);
#if DEBUG
        //L.Log("Connection string: " + UCWarfare.Config.SQL.GetConnectionString(), ConsoleColor.DarkGray);
#endif

        await UCWarfare.ToUpdate(token);
        Points.Initialize();
        Gamemode.ReadGamemodes();
        
        if (UCWarfare.Config.EnableReporter)
            Reporter = UCWarfare.I.gameObject.AddComponent<Reporter>();


        DeathTracker = await Singletons.LoadSingletonAsync<DeathTracker>(false, token: token);
        await UCWarfare.ToUpdate(token);

        /* REFLECT PRIVATE VARIABLES */
        L.Log("Getting RPCs...", ConsoleColor.Magenta);
        IDisposable indent = L.IndentLog(1);
        SendChangeText           = Util.GetRPC<ClientInstanceMethod<string>, InteractableSign>("SendChangeText", true)!;
        SendMultipleBarricades   = Util.GetRPC<ClientStaticMethod, BarricadeManager>("SendMultipleBarricades", true)!;
        SendChatIndividual       = Util.GetRPC<ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>, ChatManager>("SendChatEntry", true)!;
        SendEffectClearAll       = Util.GetRPC<ClientStaticMethod, EffectManager>("SendEffectClearAll", true)!;
        SendDestroyItem          = Util.GetRPC<ClientStaticMethod<byte, byte, uint, bool>, ItemManager>("SendDestroyItem", true)!;
        SendUpdateBarricadeState = Util.GetRPC<ClientInstanceMethod<byte[]>, BarricadeDrop>("SendUpdateState");
        SendInventory            = Util.GetRPC<ClientInstanceMethod, PlayerInventory>("SendInventory");
        SendWearShirt            = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearShirt");
        SendWearPants            = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearPants");
        SendWearHat              = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearHat");
        SendWearBackpack         = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearBackpack");
        SendWearVest             = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearVest");
        SendWearMask             = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearMask");
        SendWearGlasses          = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearGlasses");
        UseFastKits = true;
        if (SendWearShirt is null || SendWearPants is null || SendWearHat is null || SendWearBackpack is null || SendWearVest is null || SendWearMask is null || SendWearGlasses is null || SendInventory is null)
        {
            L.LogWarning("Unable to gather all the RPCs needed for Fast Kits, kits will not work as quick.");
            UseFastKits = false;
        }
        GetItemManagerInstanceCount = Util.GenerateStaticGetter<ItemManager, uint>("instanceCount", BindingFlags.NonPublic);
        SetPrivateStance = Util.GenerateInstanceSetter<PlayerStance, EPlayerStance>("_stance", BindingFlags.NonPublic);
        SetStorageInventory = Util.GenerateInstanceSetter<InteractableStorage, Items>("_items", BindingFlags.NonPublic);
        try
        {
            SetOwnerHasInventory = Util.GenerateInstanceSetter<PlayerInventory, bool>("ownerHasInventory", BindingFlags.NonPublic);
            GetOwnerHasInventory = Util.GenerateInstanceGetter<PlayerInventory, bool>("ownerHasInventory", BindingFlags.NonPublic);
            GetItemsSlots = Util.GenerateInstanceGetter<Items, bool[,]>("slots", BindingFlags.NonPublic);
        }
        catch (Exception ex)
        {
            UseFastKits = false;
            L.LogWarning("Couldn't generate a setter method for ownerHasInventory, kits will not work as quick.");
            L.LogError(ex);
        }
        try
        {
            ReplicateStance = typeof(PlayerStance).GetMethod("replicateStance", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }
        catch (Exception ex)
        {
            L.LogWarning("Couldn't get replicateState from PlayerStance, players will spawn while prone. (" + ex.Message + ").");
        }
        try
        {
            ServerSpawnLegacyImpact = 
                (Action<Vector3, Vector3, string, Transform?, IEnumerable<ITransportConnection>>?)typeof(DamageTool)
                    .GetMethod("ServerSpawnLegacyImpact", BindingFlags.Static | BindingFlags.NonPublic)?
                    .CreateDelegate(typeof(Action<Vector3, Vector3, string, Transform?, IEnumerable<ITransportConnection>>));
        }
        catch (Exception ex)
        {
            L.LogWarning("Couldn't get ServerSpawnLegacyImpact from DamageTool, explosives will not play the flesh sound. (" + ex.Message + ").");
        }
        indent.Dispose();

        /* REGISTER STATS MANAGER */
        StatsManager.LoadTeams();
        StatsManager.LoadWeapons();
        StatsManager.LoadKits();
        StatsManager.LoadVehicles();
        for (int i = 0; i < Provider.clients.Count; i++)
            StatsManager.RegisterPlayer(Provider.clients[i].playerID.steamID.m_SteamID);

        L.Log("Loading first gamemode...", ConsoleColor.Magenta);
        if (!await Gamemode.TryLoadGamemode(Gamemode.GetNextGamemode() ?? typeof(TeamCTF), token))
            throw new SingletonLoadException(SingletonLoadType.Load, null, new Exception("Failed to load gamemode"));
    }
    internal static void RegisterInitialSyncs()
    {
        Gamemode.ConfigObj = new GamemodeConfig();
        Gamemode.WinToastUI = new Gamemodes.UI.WinToastUI();
        IsInitialSyncRegistering = false;
        if (UCWarfare.Config.EnableSync)
            ConfigSync.OnInitialSyncRegisteringComplete();
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
    internal static void OnClientReceivedMessage(IConnection connection, in MessageOverhead overhead, byte[] message)
    {
        if (UCWarfare.Config.Debug)
        {
            L.Log("Received from TCP server: " + overhead + "."
#if SHOW_BYTES
                + "\n" + Logging.GetBytesHex(message)
#endif
                , ConsoleColor.DarkGray);
        }
    }
    internal static void OnClientSentMessage(IConnection connection, in MessageOverhead overhead, byte[] message)
    {
        if (UCWarfare.Config.Debug)
        {
            L.Log("Sent over TCP server    : " + overhead + "."
#if SHOW_BYTES
                  + "\n" + Logging.GetBytesHex(message)
#endif
                , ConsoleColor.DarkGray);
        }
    }
    internal static void OnClientDisconnected(IConnection connection)
    {
        L.Log("Disconnected from HomeBase.", ConsoleColor.DarkYellow);
    }
    internal static void OnClientConnected(IConnection connection)
    {
        L.Log("Established a verified connection to HomeBase.", ConsoleColor.DarkYellow);
        PlayerManager.NetCalls.SendPlayerList.NetInvoke(PlayerManager.GetPlayerList());
        if (ActionLog.Instance != null)
            ActionLog.Instance.OnConnected();
        if (!UCWarfare.Config.DisableDailyQuests)
            Quests.DailyQuests.OnConnectedToServer();
        if (Gamemode != null && Gamemode.ShouldShutdownAfterGame)
            ShutdownCommand.NetCalls.SendShuttingDownAfter.NetInvoke(Gamemode.ShutdownPlayer, Gamemode.ShutdownMessage);
        UCWarfare.RunTask(OffenseManager.OnConnected, ctx: "Offense syncing (may take a while if it's been a long time since the bot was connected).");
        ConfigSync.OnConnected(connection);
    }
    public class NetCalls
    {
        public static readonly NetCallRaw<WarfareServerInfo> SendServerInfo = new NetCallRaw<WarfareServerInfo>(1008, WarfareServerInfo.Read, WarfareServerInfo.Write);
    }
}