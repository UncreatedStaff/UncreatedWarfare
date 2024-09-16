
//#define SHOW_BYTES

using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations.Languages;

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
    public static bool UseFastKits;
    public static bool UseElectricalGrid;
    internal static MethodInfo ReplicateStance;
    public static Points Points;
    public static SessionManager Sessions;
#if NETSTANDARD || NETFRAMEWORK
    public static IStripeService WarfareStripeService;
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

    internal static InstanceSetter<PlayerStance, EPlayerStance>? SetPrivateStance;
    internal static InstanceSetter<InteractableStorage, Items>? SetStorageInventory;
    internal static InstanceSetter<PlayerInventory, bool> SetOwnerHasInventory;
    internal static InstanceGetter<PlayerInventory, bool> GetOwnerHasInventory;
    internal static InstanceGetter<Items, bool[,]> GetItemsSlots;
    internal static InstanceGetter<UseableGun, bool>? GetUseableGunReloading;
    internal static InstanceGetter<PlayerLife, CSteamID>? GetRecentKiller;
    internal static Action<Vector3, Vector3, string, Transform?, List<ITransportConnection>>? ServerSpawnLegacyImpact;
    internal static Action<PlayerInventory, SteamPlayer> SendInitialInventoryState;
    internal static Func<PooledTransportConnectionList>? PullFromTransportConnectionListPool;
    internal static Action<InteractablePower>? RefreshIsConnectedToPower;
    internal static SteamPlayer NilSteamPlayer;

    public static IRpcConnectionLifetime HomebaseLifetime { get; internal set; }
    public static IRpcSerializer RpcSerializer { get; internal set; }
    public static IRpcRouter RpcRouter { get; internal set; }
    public static IModularRpcRemoteConnection RpcConnection { get; internal set; }
    public static bool IsInitialSyncRegistering { get; private set; } = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is<TGamemode>([NotNullWhen(true)] out TGamemode? gamemode)
    {
        gamemode = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is<TGamemode>() => false;
    
    internal static async Task LoadVariables(CancellationToken token)
    {
        OriginalPlayerNames = new Dictionary<ulong, PlayerNames>(Provider.maxPlayers);
        PlaytimeComponents = new Dictionary<ulong, UCPlayerData>(Provider.maxPlayers);

        /* CREATE DIRECTORIES */
        L.Log("Validating directories...", ConsoleColor.Magenta);

        /* CONSTRUCT FRAMEWORK */
        L.Log("Instantiating Framework...", ConsoleColor.Magenta);
#if DEBUG
        //L.Log("Connection string: " + UCWarfare.Config.SQL.GetConnectionString(), ConsoleColor.DarkGray);
#endif


        /* REFLECT PRIVATE VARIABLES */
        L.Log("Getting RPCs...", ConsoleColor.Magenta);
        IDisposable indent = L.IndentLog(1);

        SendChangeText = ReflectionUtility.FindRequiredRpc<InteractableSign, ClientInstanceMethod<string>>("SendChangeText");
        SendMultipleBarricades = ReflectionUtility.FindRequiredRpc<BarricadeManager, ClientStaticMethod>("SendMultipleBarricades");
        SendChatIndividual = ReflectionUtility.FindRequiredRpc<ChatManager, ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>>("SendChatEntry");
        SendDestroyItem = ReflectionUtility.FindRequiredRpc<ItemManager, ClientStaticMethod<byte, byte, uint, bool>>("SendDestroyItem");

        SendUpdateBarricadeState = ReflectionUtility.FindRpc<BarricadeDrop, ClientInstanceMethod<byte[]>>("SendUpdateState");
        SendInventory = ReflectionUtility.FindRpc<PlayerInventory, ClientInstanceMethod>("SendInventory");
        SendWearShirt = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearShirt");
        SendWearPants = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearPants");
        SendWearHat = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearHat");
        SendWearBackpack = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearBackpack");
        SendWearVest = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearVest");
        SendWearMask = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearMask");
        SendWearGlasses = ReflectionUtility.FindRpc<PlayerClothing, ClientInstanceMethod<Guid, byte, byte[], bool>>("SendWearGlasses");
        SendSwapVehicleSeats = ReflectionUtility.FindRpc<VehicleManager, ClientStaticMethod<uint, byte, byte>>("SendSwapVehicleSeats");
        SendEnterVehicle = ReflectionUtility.FindRpc<VehicleManager, ClientStaticMethod<uint, byte, CSteamID>>("SendEnterVehicle");
        // SendScreenshotDestination = ReflectionUtility.FindRpc<ClientInstanceMethod, Player>("SendScreenshotDestination");

        UseFastKits = true;
        if (SendWearShirt is null || SendWearPants is null || SendWearHat is null || SendWearBackpack is null || SendWearVest is null || SendWearMask is null || SendWearGlasses is null || SendInventory is null)
        {
            L.LogWarning("Unable to gather all the RPCs needed for Fast Kits, kits will not work as quick.");
            UseFastKits = false;
        }

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


        L.Log("Loading first gamemode...", ConsoleColor.Magenta);

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

        Object.Destroy(obj);
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

    internal static readonly List<KeyValuePair<Type, string?>> TranslatableEnumTypes = new List<KeyValuePair<Type, string?>>()
    {
        new KeyValuePair<Type, string?>(typeof(EDamageOrigin), "Damage Origin"),
        new KeyValuePair<Type, string?>(typeof(EDeathCause), "Death Cause"),
        new KeyValuePair<Type, string?>(typeof(ELimb), "Limb")
    };

    //internal static void OnClientConnected(IConnection connection)
    //{
    //    L.Log("Established a verified connection to HomeBase.", ConsoleColor.DarkYellow);
    //    CancellationTokenSource src = new CancellationTokenSource();
    //    CancellationTokenSource? old = Interlocked.Exchange(ref _netClientSource, src);
    //    old?.Cancel();
    //    CancellationToken tkn = src.Token;
    //    CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(UCWarfare.UnloadCancel);
    //    tkn.ThrowIfCancellationRequested();
    //    UCWarfare.RunTask(async tokens =>
    //    {
    //        try
    //        {
    //            await UCWarfare.ToUpdate(tokens.Token);
    //            tokens.Token.ThrowIfCancellationRequested();
    //            PlayerManager.NetCalls.SendPlayerList.NetInvoke(PlayerManager.GetPlayerList());
    //            if (!UCWarfare.Config.DisableDailyQuests)
    //                Quests.DailyQuests.OnConnectedToServer();
    //            if (Gamemode != null && Gamemode.ShouldShutdownAfterGame)
    //                ShutdownCommand.NetCalls.SendShuttingDownAfter.NetInvoke(Gamemode.ShutdownPlayer, Gamemode.ShutdownMessage);
    //            tokens.Token.ThrowIfCancellationRequested();
    //            IUncreatedSingleton[] singletons = Singletons.GetSingletons();
    //            for (int i = 0; i < singletons.Length; ++i)
    //            {
    //                if (singletons[i] is ITCPConnectedListener l)
    //                {
    //                    await l.OnConnected(tokens.Token).ConfigureAwait(false);
    //                    await UCWarfare.ToUpdate(tokens.Token);
    //                }
    //            }
    //            tokens.Token.ThrowIfCancellationRequested();
    //            if (ActionLog.Instance != null)
    //                await ActionLog.Instance.OnConnected(tokens.Token).ConfigureAwait(false);
    //        }
    //        finally
    //        {
    //            tokens.Dispose();
    //        }
    //    }, tokens, ctx: "Execute on client connected events.", timeout: 120000);
    //}
    
}