
//#define SHOW_BYTES

using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Uncreated.Framework;
using Uncreated.Homebase.Unturned;
using Uncreated.Homebase.Unturned.Warfare;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.ReportSystem;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Sync;
using Uncreated.Warfare.Teams;
using UnityEngine;
using UnityEngine.Assertions;
#if NETSTANDARD || NETFRAMEWORK
using Uncreated.Warfare.Networking.Purchasing;
#endif

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
        public static readonly string PointsStorage = Path.Combine(BaseDirectory, "Points") + Path.DirectorySeparatorChar;
        public static readonly string CooldownStorage = Path.Combine(BaseDirectory, "Cooldowns") + Path.DirectorySeparatorChar;
        public static readonly string SquadStorage = Path.Combine(BaseDirectory, "Squads") + Path.DirectorySeparatorChar;
        public static readonly string KitsStorage = Path.Combine(BaseDirectory, "Kits") + Path.DirectorySeparatorChar;
        public static readonly string FOBStorage = Path.Combine(BaseDirectory, "FOBs") + Path.DirectorySeparatorChar;
        public static readonly string LangStorage = Path.Combine(BaseDirectory, "Lang") + Path.DirectorySeparatorChar;
        public static readonly string Logs = Path.Combine(BaseDirectory, "Logs") + Path.DirectorySeparatorChar;
        public static readonly string Sync = Path.Combine(BaseDirectory, "Sync") + Path.DirectorySeparatorChar;
        public static readonly string ActionLog = Path.Combine(Logs, "ActionLog") + Path.DirectorySeparatorChar;
        public static readonly string PendingOffenses = Path.Combine(BaseDirectory, "Offenses") + Path.DirectorySeparatorChar;
        public static readonly string TraitDataStorage = Path.Combine(BaseDirectory, "traits.json");
        public static readonly string ConfigSync = Path.Combine(Sync, "config.json");
        public static readonly string KitSync = Path.Combine(Sync, "kits.json");
        public static readonly string CurrentLog = Path.Combine(Logs, "current.txt");
        public static readonly string FunctionLog = Path.Combine(Logs, "funclog.txt");
        public static readonly string Heartbeat = Path.Combine(BaseDirectory, "Stats", "heartbeat.dat");
        public static readonly string HeartbeatBackup = Path.Combine(BaseDirectory, "Stats", "heartbeat_last.dat");
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
        public const PlayerKey DropSupplyOverride = PlayerKey.PluginKey1;
    }

    internal static IUncreatedSingleton[] GamemodeListeners;
    public const string SuppressCategory = "Microsoft.Performance";
    public const string SuppressID = "IDE0051";
    public static readonly Regex ChatFilter = new Regex(@"(?:[nńǹňñṅņṇṋṉn̈ɲƞᵰᶇɳȵɴｎŋǌvṼṽṿʋᶌᶌⱱⱴᴠʌｖ\|\\\/]\W{0,}[il1ÍíìĭîǐïḯĩįīỉȉȋịḭɨᵻᶖiıɪɩｉﬁIĳ\|\!]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ](?!h|(?:an)|(?:[e|a|o]t)|(?:un)|(?:rab)|(?:rain)|(?:low)|(?:ue)|(?:uy))(?!n\shadi)\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[ae]{0,1}\W{0,}[r]{0,}(?:ia){0,})|(?:c\W{0,}h\W{0,}i{1,}\W{0,}n{1,}\W{0,}k{1,})|(?:[fḟƒᵮᶂꜰｆﬀﬃﬄﬁﬂ]\W{0,}[aáàâǎăãảȧạäåḁāąᶏⱥȁấầẫẩậắằẵẳặǻǡǟȃɑᴀɐɒａæᴁᴭᵆǽǣᴂ]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{1,}\W{0,}o{0,}\W{0,}t{0,1}(?!ain))", RegexOptions.IgnoreCase);
    public static readonly Regex NameRichTextReplaceFilter = new Regex("<.*>");
    public static readonly Regex PluginKeyMatch = new Regex(@"\<plugin_\d\/\>", RegexOptions.IgnoreCase);
    public static CultureInfo LocalLocale = Languages.CultureEnglishUS; // todo set from config
    public static readonly CultureInfo AdminLocale = Languages.CultureEnglishUS;
    public static Dictionary<string, Color> Colors;
    public static Dictionary<string, string> ColorsHex;
    public static Dictionary<string, Vector3> ExtraPoints;
    public static Dictionary<ulong, string> DefaultPlayerNames;
    public static Dictionary<ulong, PlayerNames> OriginalPlayerNames;
    public static Dictionary<ulong, UCPlayerData> PlaytimeComponents;
    internal static WarfareSQL DatabaseManager;
    internal static WarfareSQL? RemoteSQL;
    internal static DatabaseInterface ModerationSql;
    internal static WarfareMySqlLanguageDataStore LanguageDataStore;
    internal static PurchaseRecordsInterface<WarfareDbContext> PurchasingDataStore;
    public static Gamemode Gamemode;
    public static bool TrackStats = true;
    public static bool UseFastKits;
    public static bool UseElectricalGrid;
    internal static MethodInfo ReplicateStance;
    public static Reporter? Reporter;
    public static DeathTracker DeathTracker;
    public static Points Points;
    public static SessionManager Sessions;
#if NETSTANDARD || NETFRAMEWORK
    public static WarfareStripeService WarfareStripeService;
#endif
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
    internal static ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string> SendChatIndividual;
    internal static ClientStaticMethod<uint, byte, byte>? SendSwapVehicleSeats;
    internal static ClientStaticMethod<uint, byte, CSteamID>? SendEnterVehicle;
    internal static ClientInstanceMethod? SendInventory;
    // internal static ClientInstanceMethod? SendScreenshotDestination;
    internal static SingletonManager Singletons;
    internal static InstanceSetter<PlayerStance, EPlayerStance>? SetPrivateStance;
    internal static InstanceSetter<InteractableStorage, Items>? SetStorageInventory;
    internal static InstanceSetter<PlayerInventory, bool> SetOwnerHasInventory;
    internal static InstanceGetter<PlayerInventory, bool> GetOwnerHasInventory;
    internal static InstanceGetter<Items, bool[,]> GetItemsSlots;
    internal static InstanceGetter<UseableGun, bool>? GetUseableGunReloading;
    internal static InstanceGetter<PlayerLife, CSteamID>? GetRecentKiller;
    internal static StaticGetter<uint> GetItemManagerInstanceCount;
    internal static Action<Vector3, Vector3, string, Transform?, List<ITransportConnection>>? ServerSpawnLegacyImpact;
    internal static Action<PlayerInventory, SteamPlayer> SendInitialInventoryState;
    internal static Func<PooledTransportConnectionList>? PullFromTransportConnectionListPool;
    internal static Action<InteractablePower>? RefreshIsConnectedToPower;
    internal static SteamPlayer NilSteamPlayer;

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
    [OperationTest(DisplayName = "TransportConnectionListPool.Get Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestPullFromTransportConnectionListPool()
    {
        Assert.IsNotNull(PullFromTransportConnectionListPool);
    }
    [OperationTest(DisplayName = "InteractablePower.RefreshIsConnectedToPower Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestRefreshIsConnectedToPower()
    {
        Assert.IsNotNull(RefreshIsConnectedToPower);
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
    public static ServerConfig GetServerConfig()
    {
        string? ip;
        try
        {
            ip = SteamGameServer.GetPublicIP().ToIPAddress().ToString();
            L.LogDebug("IP: " + ip);
        }
        catch (InvalidOperationException)
        {
            L.LogDebug("IP not available.");
            ip = null;
        }
        
        return new WarfareServerConfig
        {
            Address = ip,
            FactionTeam1 = TeamManager.Team1Faction.PrimaryKey.Key,
            FactionTeam2 = TeamManager.Team2Faction.PrimaryKey.Key,
            Identity = UCWarfare.Config.TCPSettings.TCPServerIdentity,
            MapId = MapScheduler.Current,
            Port = Provider.port,
            MaxPlayers = Provider.maxPlayers,
            Region = UCWarfare.Config.Region,
            RegionId = UCWarfare.Config.RegionKey,
            ServerId = Provider.server.m_SteamID,
            ServerName = Provider.serverName,
            MapName = Level.info?.name ?? Provider.map
        };
    }
    public static void SendUpdateServerConfig()
    {
        L.LogDebug("Updating config to net client: " + (UCWarfare.I.NetClient?.Identity) + ".");
        UCWarfare.I.NetClient?.UpdateConfig();
    }
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

        L.Log("Verifying global tables...", ConsoleColor.Magenta);
        await AdminSql.RefreshInformationSchemaAsync(token).ConfigureAwait(false);
        await AdminSql.VerifyTables(WarfareSQL.WarfareSchemas, token).ConfigureAwait(false);
        L.Log(" -- Done.", ConsoleColor.DarkMagenta);
    }
    internal static async Task LoadVariables(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        OriginalPlayerNames = new Dictionary<ulong, PlayerNames>(Provider.maxPlayers);
        PlaytimeComponents = new Dictionary<ulong, UCPlayerData>(Provider.maxPlayers);

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

        ZoneList l = await Singletons.LoadSingletonAsync<ZoneList>(token: token);
        L.Log("Read " + l.Items.Count + " zones.", ConsoleColor.Magenta);

        /* CONSTRUCT FRAMEWORK */
        L.Log("Instantiating Framework...", ConsoleColor.Magenta);
#if DEBUG
        //L.Log("Connection string: " + UCWarfare.Config.SQL.GetConnectionString(), ConsoleColor.DarkGray);
#endif

        await UCWarfare.ToUpdate(token);
        Gamemode.ReadGamemodes();

        if (UCWarfare.Config.EnableReporter)
            Reporter = UCWarfare.I.gameObject.AddComponent<Reporter>();


        DeathTracker = await Singletons.LoadSingletonAsync<DeathTracker>(true, token: token);
        GamemodeListeners = new IUncreatedSingleton[2];
        GamemodeListeners[0] = Points = await Singletons.LoadSingletonAsync<Points>(true, token: token);
        GamemodeListeners[1] = Sessions = await Singletons.LoadSingletonAsync<SessionManager>(true, token: token);
        await Singletons.LoadSingletonAsync<PlayerList>(true, token: token);
        await UCWarfare.ToUpdate(token);

        /* REFLECT PRIVATE VARIABLES */
        L.Log("Getting RPCs...", ConsoleColor.Magenta);
        IDisposable indent = L.IndentLog(1);
        SendChangeText = Util.GetRPC<ClientInstanceMethod<string>, InteractableSign>("SendChangeText", true)!;
        SendMultipleBarricades = Util.GetRPC<ClientStaticMethod, BarricadeManager>("SendMultipleBarricades", true)!;
        SendChatIndividual = Util.GetRPC<ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>, ChatManager>("SendChatEntry", true)!;
        SendDestroyItem = Util.GetRPC<ClientStaticMethod<byte, byte, uint, bool>, ItemManager>("SendDestroyItem", true)!;
        SendUpdateBarricadeState = Util.GetRPC<ClientInstanceMethod<byte[]>, BarricadeDrop>("SendUpdateState");
        SendInventory = Util.GetRPC<ClientInstanceMethod, PlayerInventory>("SendInventory");
        SendWearShirt = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearShirt");
        SendWearPants = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearPants");
        SendWearHat = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearHat");
        SendWearBackpack = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearBackpack");
        SendWearVest = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearVest");
        SendWearMask = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearMask");
        SendWearGlasses = Util.GetRPC<ClientInstanceMethod<Guid, byte, byte[], bool>, PlayerClothing>("SendWearGlasses");
        SendSwapVehicleSeats = Util.GetRPC<ClientStaticMethod<uint, byte, byte>, VehicleManager>("SendSwapVehicleSeats");
        SendEnterVehicle = Util.GetRPC<ClientStaticMethod<uint, byte, CSteamID>, VehicleManager>("SendEnterVehicle");
        // SendScreenshotDestination = Util.GetRPC<ClientInstanceMethod, Player>("SendScreenshotDestination");
        UseFastKits = true;
        if (SendWearShirt is null || SendWearPants is null || SendWearHat is null || SendWearBackpack is null || SendWearVest is null || SendWearMask is null || SendWearGlasses is null || SendInventory is null)
        {
            L.LogWarning("Unable to gather all the RPCs needed for Fast Kits, kits will not work as quick.");
            UseFastKits = false;
        }
        GetItemManagerInstanceCount = Accessor.GenerateStaticGetter<ItemManager, uint>("instanceCount", throwOnError: true)!;
        SetPrivateStance = Accessor.GenerateInstanceSetter<PlayerStance, EPlayerStance>("_stance");
        SetStorageInventory = Accessor.GenerateInstanceSetter<InteractableStorage, Items>("_items");
        RefreshIsConnectedToPower = (Action<InteractablePower>?)Accessor.GenerateInstanceCaller<InteractablePower>("RefreshIsConnectedToPower");
        GetUseableGunReloading = Accessor.GenerateInstanceGetter<UseableGun, bool>("isReloading");

        SendInitialInventoryState = Accessor.GenerateInstanceCaller<PlayerInventory, Action<PlayerInventory, SteamPlayer>>("SendInitialPlayerState", throwOnError: true)!;
        try
        {
            GetItemsSlots = Accessor.GenerateInstanceGetter<Items, bool[,]>("slots", throwOnError: true)!;
            SetOwnerHasInventory = Accessor.GenerateInstanceSetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: true)!;
            GetOwnerHasInventory = Accessor.GenerateInstanceGetter<PlayerInventory, bool>("ownerHasInventory", throwOnError: true)!;
        }
        catch (Exception ex)
        {
            UseFastKits = false;
            L.LogWarning("Couldn't generate a fast kits accessors, kits will not work as quick.");
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
                (Action<Vector3, Vector3, string, Transform?, List<ITransportConnection>>?)typeof(DamageTool)
                    .GetMethod("ServerSpawnLegacyImpact", BindingFlags.Static | BindingFlags.NonPublic)?
                    .CreateDelegate(typeof(Action<Vector3, Vector3, string, Transform, List<ITransportConnection>>));
        }
        catch (Exception ex)
        {
            L.LogWarning("Couldn't get ServerSpawnLegacyImpact from DamageTool, explosives will not play the flesh sound. (" + ex.Message + ").");
        }

        try
        {
            GetRecentKiller = Accessor.GenerateInstanceGetter<PlayerLife, CSteamID>("recentKiller");
        }
        catch (Exception ex)
        {
            L.LogWarning("Couldn't get PlayerLife.recentKiller from PlayerLife, other reputation sources will be ignored. (" + ex.Message + ").");
        }

        PullFromTransportConnectionListPool = null;
        try
        {
            MethodInfo? method = typeof(Provider).Assembly
                .GetType("SDG.Unturned.TransportConnectionListPool", true, false)?.GetMethod("Get",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                PullFromTransportConnectionListPool = (Func<PooledTransportConnectionList>)method.CreateDelegate(typeof(Func<PooledTransportConnectionList>));
            }
            else
            {
                L.LogWarning("Couldn't find Get in TransportConnectionListPool, list pooling will not be used.");
            }
        }
        catch (Exception ex)
        {
            L.LogWarning("Couldn't get Get from TransportConnectionListPool, list pooling will not be used (" + ex.Message + ").");
        }

        indent.Dispose();

        /* REGISTER STATS MANAGER */
        StatsManager.LoadTeams();
        StatsManager.LoadWeapons();
        StatsManager.LoadKits();
        StatsManager.LoadVehicles();
        for (int i = 0; i < Provider.clients.Count; i++)
            StatsManager.RegisterPlayer(Provider.clients[i].playerID.steamID.m_SteamID);

        await SessionManager.CheckForTerminatedSessions(token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);

        L.Log("Loading first gamemode...", ConsoleColor.Magenta);
        if (!await Gamemode.TryLoadGamemode(Gamemode.GetNextGamemode() ?? typeof(TeamCTF), true, token).ConfigureAwait(false))
            throw new SingletonLoadException(SingletonLoadType.Load, null, new Exception("Failed to load gamemode"));

        SteamPlayerID id = new SteamPlayerID(CSteamID.Nil, 0, "Nil", "Nil", "Nil", CSteamID.Nil);
        GameObject obj = Provider.gameMode.getPlayerGameObject(id);
        obj.SetActive(false);
        NetId nilNetId = new NetId(uint.MaxValue - 100);

        NilSteamPlayer = new SteamPlayer(null, nilNetId, id, obj.transform, false, false, -1, 0, 0, 0,
            Color.white, Color.white, Color.white, false, 0, 0, 0, 0, 0, 0, 0, Array.Empty<int>(), Array.Empty<string>(), Array.Empty<string>(), EPlayerSkillset.NONE,
            Provider.language, CSteamID.Nil, EClientPlatform.Windows);

        for (uint i = nilNetId.id; i < uint.MaxValue; ++i)
        {
            if (!NetIdRegistry.Release(new NetId(i)))
                break;
        }

        UnityEngine.Object.Destroy(obj);
    }
    public static PooledTransportConnectionList GetPooledTransportConnectionList(int capacity = -1)
    {
        PooledTransportConnectionList? rtn = null;
        Exception? ex2 = null;
        if (PullFromTransportConnectionListPool != null)
        {
            try
            {
                rtn = PullFromTransportConnectionListPool();
            }
            catch (Exception ex)
            {
                ex2 = ex;
                L.LogError(ex);
            }
        }
        if (rtn == null)
        {
            if (capacity == -1)
                capacity = Provider.clients.Count;
            try
            {
                rtn = (PooledTransportConnectionList?)Activator.CreateInstance(typeof(PooledTransportConnectionList),
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { capacity }, CultureInfo.InvariantCulture, null);
            }
            catch (Exception ex)
            {
                L.LogError(ex);
                if (ex2 != null)
                    throw new AggregateException("Unable to create pooled transport connection!", ex2, ex);

                throw new Exception("Unable to create pooled transport connection!", ex);
            }

            if (rtn == null)
                throw new Exception("Unable to create pooled transport connection, returned null!");
        }

        return rtn;
    }
    public static PooledTransportConnectionList GetPooledTransportConnectionList(IEnumerable<ITransportConnection> selector, int capacity = -1)
    {
        PooledTransportConnectionList rtn = GetPooledTransportConnectionList(capacity);
        rtn.AddRange(selector);
        return rtn;
    }
    private static readonly char[] TrimChars = [ '.', '?', '\\', '/', '-', '=', '_', ',' ];
    public static string? GetChatFilterViolation(string input)
    {
        Match match = ChatFilter.Match(input);
        if (!match.Success || match.Length <= 0)
            return null;

        string matchValue = match.Value.TrimEnd().TrimEnd(TrimChars);
        int len1 = matchValue.Length;
        matchValue = matchValue.TrimStart().TrimStart(TrimChars);

        int matchIndex = match.Index + (len1 - matchValue.Length);

        static bool IsPunctuation(char c)
        {
            for (int i = 0; i < TrimChars.Length; ++i)
                if (TrimChars[i] == c)
                    return true;

            return false;
        }

        // whole word
        if ((matchIndex == 0 || char.IsWhiteSpace(input[matchIndex - 1]) || char.IsPunctuation(input[matchIndex - 1])) &&
            (matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length]) || IsPunctuation(input[matchIndex + matchValue.Length])))
        {
            // vibe matches the filter
            if (matchValue.Equals("vibe", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
        }
        // .. can i be .. or .. can i go ..
        if (matchIndex - 2 >= 0 && input.Substring(matchIndex - 2, 2) is { } sub &&
            (sub.Equals("ca", StringComparison.InvariantCultureIgnoreCase) || sub.Equals("ma", StringComparison.InvariantCultureIgnoreCase)))
        {
            if ((matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length]) || IsPunctuation(input[matchIndex + matchValue.Length]))
                && matchValue.Equals("n i be", StringComparison.InvariantCultureIgnoreCase))
                return null;

            if ((matchIndex + matchValue.Length < input.Length && input[matchIndex + matchValue.Length].ToString().Equals("o", StringComparison.InvariantCultureIgnoreCase))
                && matchValue.Equals("n i g", StringComparison.InvariantCultureIgnoreCase))
                return null;
        }
        else if (matchIndex - 2 > 0 && input.Substring(matchIndex - 1, 1).Equals("o", StringComparison.InvariantCultureIgnoreCase)
                 && !(matchIndex + matchValue.Length >= input.Length || char.IsWhiteSpace(input[matchIndex + matchValue.Length + 1]) || IsPunctuation(input[matchIndex + matchValue.Length])))
        {
            // .. of a g___
            if (matchValue.Equals("f a g", StringComparison.InvariantCultureIgnoreCase))
                return null;
        }
        // .. an igla ..
        else if (matchValue.Equals("n ig", StringComparison.InvariantCultureIgnoreCase) && matchIndex > 0 &&
                 input[matchIndex - 1].ToString().Equals("a", StringComparison.InvariantCultureIgnoreCase) &&
                 matchIndex < input.Length - 2 && input.Substring(matchIndex + matchValue.Length, 2).Equals("la", StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return matchValue;
    }
    public static async Task ReloadLanguageDataStore(bool init, CancellationToken token = default)
    {
        if (init)
            await LanguageDataStore.Initialize(token).ConfigureAwait(false);
        await LanguageDataStore.ReloadCache(token).ConfigureAwait(false);

        if (LanguageDataStore.GetInfoCached(L.Default) is { } defaultLang)
            FallbackLanguageInfo = defaultLang;
    }
    internal static void RegisterInitialConfig()
    {
        Gamemode.ConfigObj = new GamemodeConfig();
        Gamemode.WinToastUI = new Gamemodes.UI.WinToastUI();
        IsInitialSyncRegistering = false;
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
    
    private static CancellationTokenSource? _netClientSource;
    internal static void OnClientConnected(IConnection connection)
    {
        L.Log("Established a verified connection to HomeBase.", ConsoleColor.DarkYellow);
        CancellationTokenSource src = new CancellationTokenSource();
        CancellationTokenSource? old = Interlocked.Exchange(ref _netClientSource, src);
        old?.Cancel();
        CancellationToken tkn = src.Token;
        tkn.CombineIfNeeded(UCWarfare.UnloadCancel);
        UCWarfare.RunTask(async token =>
        {
            await UCWarfare.ToUpdate(token);
            tkn.ThrowIfCancellationRequested();
            PlayerManager.NetCalls.SendPlayerList.NetInvoke(PlayerManager.GetPlayerList());
            if (!UCWarfare.Config.DisableDailyQuests)
                Quests.DailyQuests.OnConnectedToServer();
            if (Gamemode != null && Gamemode.ShouldShutdownAfterGame)
                ShutdownCommand.NetCalls.SendShuttingDownAfter.NetInvoke(Gamemode.ShutdownPlayer, Gamemode.ShutdownMessage);
            tkn.ThrowIfCancellationRequested();
            IUncreatedSingleton[] singletons = Singletons.GetSingletons();
            for (int i = 0; i < singletons.Length; ++i)
            {
                if (singletons[i] is ITCPConnectedListener l)
                {
                    await l.OnConnected(token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                }
            }
            tkn.ThrowIfCancellationRequested();
            if (ActionLog.Instance != null)
                await ActionLog.Instance.OnConnected(token).ConfigureAwait(false);
        }, tkn, ctx: "Execute on client connected events.", timeout: 120000);
    }
    public static void HideAllUI(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        IUncreatedSingleton[] singletons = Singletons.GetSingletons();
        for (int i = 0; i < singletons.Length; ++i)
        {
            if (singletons[i] is IUIListener ui)
                ui.HideUI(player);
        }
    }
    public static void ShowAllUI(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        IUncreatedSingleton[] singletons = Singletons.GetSingletons();
        for (int i = 0; i < singletons.Length; ++i)
        {
            if (singletons[i] is IUIListener ui)
                ui.ShowUI(player);
        }
    }
    public static void UpdateAllUI(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        IUncreatedSingleton[] singletons = Singletons.GetSingletons();
        for (int i = 0; i < singletons.Length; ++i)
        {
            if (singletons[i] is IUIListener ui)
                ui.UpdateUI(player);
        }
    }
    public static LanguageInfo FallbackLanguageInfo { get; internal set; } = new LanguageInfo
    {
        Key = 0,
        Code = L.Default,
        DisplayName = "English",
        Aliases = new LanguageAlias[]
        {
            new LanguageAlias { Alias = "English" },
            new LanguageAlias { Alias = "American" },
            new LanguageAlias { Alias = "British" },
            new LanguageAlias { Alias = "Inglés" },
            new LanguageAlias { Alias = "Ingles" },
            new LanguageAlias { Alias = "Inglesa" }
        },
        Contributors = new LanguageContributor[]
        {
            new LanguageContributor { Contributor = 76561198267927009 },
            new LanguageContributor { Contributor = 76561198857595123 }
        },
        HasTranslationSupport = true,
        DefaultCultureCode = "en-US",
        RequiresIMGUI = false,
        SupportedCultures = new LanguageCulture[]
        {
           new LanguageCulture { CultureCode = "en-001" },
           new LanguageCulture { CultureCode = "en-029" },
           new LanguageCulture { CultureCode = "en-150" },
           new LanguageCulture { CultureCode = "en-AE" },
           new LanguageCulture { CultureCode = "en-AG" },
           new LanguageCulture { CultureCode = "en-AI" },
           new LanguageCulture { CultureCode = "en-AS" },
           new LanguageCulture { CultureCode = "en-AT" },
           new LanguageCulture { CultureCode = "en-AU" },
           new LanguageCulture { CultureCode = "en-BB" },
           new LanguageCulture { CultureCode = "en-BE" },
           new LanguageCulture { CultureCode = "en-BI" },
           new LanguageCulture { CultureCode = "en-BM" },
           new LanguageCulture { CultureCode = "en-BS" },
           new LanguageCulture { CultureCode = "en-BW" },
           new LanguageCulture { CultureCode = "en-BZ" },
           new LanguageCulture { CultureCode = "en-CA" },
           new LanguageCulture { CultureCode = "en-CC" },
           new LanguageCulture { CultureCode = "en-CH" },
           new LanguageCulture { CultureCode = "en-CK" },
           new LanguageCulture { CultureCode = "en-CM" },
           new LanguageCulture { CultureCode = "en-CX" },
           new LanguageCulture { CultureCode = "en-CY" },
           new LanguageCulture { CultureCode = "en-DE" },
           new LanguageCulture { CultureCode = "en-DK" },
           new LanguageCulture { CultureCode = "en-DM" },
           new LanguageCulture { CultureCode = "en-ER" },
           new LanguageCulture { CultureCode = "en-FI" },
           new LanguageCulture { CultureCode = "en-FJ" },
           new LanguageCulture { CultureCode = "en-FK" },
           new LanguageCulture { CultureCode = "en-FM" },
           new LanguageCulture { CultureCode = "en-GB" },
           new LanguageCulture { CultureCode = "en-GD" },
           new LanguageCulture { CultureCode = "en-GG" },
           new LanguageCulture { CultureCode = "en-GH" },
           new LanguageCulture { CultureCode = "en-GI" },
           new LanguageCulture { CultureCode = "en-GM" },
           new LanguageCulture { CultureCode = "en-GU" },
           new LanguageCulture { CultureCode = "en-GY" },
           new LanguageCulture { CultureCode = "en-HK" },
           new LanguageCulture { CultureCode = "en-ID" },
           new LanguageCulture { CultureCode = "en-IE" },
           new LanguageCulture { CultureCode = "en-IL" },
           new LanguageCulture { CultureCode = "en-IM" },
           new LanguageCulture { CultureCode = "en-IN" },
           new LanguageCulture { CultureCode = "en-IO" },
           new LanguageCulture { CultureCode = "en-JE" },
           new LanguageCulture { CultureCode = "en-JM" },
           new LanguageCulture { CultureCode = "en-KE" },
           new LanguageCulture { CultureCode = "en-KI" },
           new LanguageCulture { CultureCode = "en-KN" },
           new LanguageCulture { CultureCode = "en-KY" },
           new LanguageCulture { CultureCode = "en-LC" },
           new LanguageCulture { CultureCode = "en-LR" },
           new LanguageCulture { CultureCode = "en-LS" },
           new LanguageCulture { CultureCode = "en-MG" },
           new LanguageCulture { CultureCode = "en-MH" },
           new LanguageCulture { CultureCode = "en-MO" },
           new LanguageCulture { CultureCode = "en-MP" },
           new LanguageCulture { CultureCode = "en-MS" },
           new LanguageCulture { CultureCode = "en-MT" },
           new LanguageCulture { CultureCode = "en-MU" },
           new LanguageCulture { CultureCode = "en-MW" },
           new LanguageCulture { CultureCode = "en-MY" },
           new LanguageCulture { CultureCode = "en-NA" },
           new LanguageCulture { CultureCode = "en-NF" },
           new LanguageCulture { CultureCode = "en-NG" },
           new LanguageCulture { CultureCode = "en-NL" },
           new LanguageCulture { CultureCode = "en-NR" },
           new LanguageCulture { CultureCode = "en-NU" },
           new LanguageCulture { CultureCode = "en-NZ" },
           new LanguageCulture { CultureCode = "en-PG" },
           new LanguageCulture { CultureCode = "en-PH" },
           new LanguageCulture { CultureCode = "en-PK" },
           new LanguageCulture { CultureCode = "en-PN" },
           new LanguageCulture { CultureCode = "en-PR" },
           new LanguageCulture { CultureCode = "en-PW" },
           new LanguageCulture { CultureCode = "en-RW" },
           new LanguageCulture { CultureCode = "en-SB" },
           new LanguageCulture { CultureCode = "en-SC" },
           new LanguageCulture { CultureCode = "en-SD" },
           new LanguageCulture { CultureCode = "en-SE" },
           new LanguageCulture { CultureCode = "en-SG" },
           new LanguageCulture { CultureCode = "en-SH" },
           new LanguageCulture { CultureCode = "en-SI" },
           new LanguageCulture { CultureCode = "en-SL" },
           new LanguageCulture { CultureCode = "en-SS" },
           new LanguageCulture { CultureCode = "en-SX" },
           new LanguageCulture { CultureCode = "en-SZ" },
           new LanguageCulture { CultureCode = "en-TC" },
           new LanguageCulture { CultureCode = "en-TK" },
           new LanguageCulture { CultureCode = "en-TO" },
           new LanguageCulture { CultureCode = "en-TT" },
           new LanguageCulture { CultureCode = "en-TV" },
           new LanguageCulture { CultureCode = "en-TZ" },
           new LanguageCulture { CultureCode = "en-UG" },
           new LanguageCulture { CultureCode = "en-UM" },
           new LanguageCulture { CultureCode = "en-US" },
           new LanguageCulture { CultureCode = "en-VC" },
           new LanguageCulture { CultureCode = "en-VG" },
           new LanguageCulture { CultureCode = "en-VI" },
           new LanguageCulture { CultureCode = "en-VU" },
           new LanguageCulture { CultureCode = "en-WS" },
           new LanguageCulture { CultureCode = "en-ZA" },
           new LanguageCulture { CultureCode = "en-ZM" },
           new LanguageCulture { CultureCode = "en-ZW" }
        }
    };
}