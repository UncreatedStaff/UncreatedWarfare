
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
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Sessions;
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
        public static readonly string LangStorage = Path.Combine(BaseDirectory, "Lang") + Path.DirectorySeparatorChar;
        public static readonly string Logs = Path.Combine(BaseDirectory, "Logs") + Path.DirectorySeparatorChar;
        public static readonly string Sync = Path.Combine(BaseDirectory, "Sync") + Path.DirectorySeparatorChar;
        public static readonly string ActionLog = Path.Combine(Logs, "ActionLog") + Path.DirectorySeparatorChar;
        public static readonly string Heartbeat = Path.Combine(BaseDirectory, "Stats", "heartbeat.dat");
        public static readonly string HeartbeatBackup = Path.Combine(BaseDirectory, "Stats", "heartbeat_last.dat");
    }


    public const string SuppressCategory = "Microsoft.Performance";
    public const string SuppressID = "IDE0051";
    public static readonly Regex PluginKeyMatch = new Regex(@"\<plugin_\d\/\>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static CultureInfo LocalLocale = Languages.CultureEnglishUS; // todo set from config
    public static Dictionary<ulong, UCPlayerData> PlaytimeComponents;
    public static bool UseFastKits;
    public static bool UseElectricalGrid;
    internal static ClientInstanceMethod<byte[]>? SendUpdateBarricadeState;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearShirt;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearPants;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearHat;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearBackpack;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearVest;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearMask;
    internal static ClientInstanceMethod<Guid, byte, byte[], bool>? SendWearGlasses;
    internal static ClientStaticMethod<uint, byte, byte>? SendSwapVehicleSeats;
    internal static ClientInstanceMethod? SendInventory;
    // internal static ClientInstanceMethod? SendScreenshotDestination;

    internal static InstanceSetter<PlayerInventory, bool> SetOwnerHasInventory;
    internal static InstanceGetter<PlayerInventory, bool> GetOwnerHasInventory;
    internal static InstanceGetter<Items, bool[,]> GetItemsSlots;
    internal static InstanceGetter<UseableGun, bool>? GetUseableGunReloading;
    internal static InstanceGetter<PlayerLife, CSteamID>? GetRecentKiller;
    internal static Action<Vector3, Vector3, string, Transform?, List<ITransportConnection>>? ServerSpawnLegacyImpact;
    internal static Action<PlayerInventory, SteamPlayer> SendInitialInventoryState;
    internal static Func<PooledTransportConnectionList>? PullFromTransportConnectionListPool;
    internal static SteamPlayer NilSteamPlayer;


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
                CommandWindow.LogError(ex);
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
                CommandWindow.LogError(ex);
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