
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
    public static readonly Regex ChatFilter = new Regex(@"(?:[nńǹňñṅņṇṋṉn̈ɲƞᵰᶇɳȵɴｎŋǌvṼṽṿʋᶌᶌⱱⱴᴠʌｖ\|\\\/]\W{0,}[il1ÍíìĭîǐïḯĩįīỉȉȋịḭɨᵻᶖiıɪɩｉﬁIĳ\|\!]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ](?!h|(?:an)|(?:[e|a|o]t)|(?:un)|(?:rab)|(?:rain)|(?:low)|(?:ue)|(?:uy))(?!n\shadi)\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{0,}\W{0,}[ae]{0,1}\W{0,}[r]{0,}(?:ia){0,})|(?:c\W{0,}h\W{0,}i{1,}\W{0,}n{1,}\W{0,}k{1,})|(?:[fḟƒᵮᶂꜰｆﬀﬃﬄﬁﬂ]\W{0,}[aáàâǎăãảȧạäåḁāąᶏⱥȁấầẫẩậắằẵẳặǻǡǟȃɑᴀɐɒａæᴁᴭᵆǽǣᴂ]\W{0,}[gqb96ǴǵğĝǧġģḡǥɠᶃɢȝｇŋɢɢɋƣʠｑȹḂḃḅḇƀɓƃᵬᶀʙｂȸ]{1,}\W{0,}o{0,}\W{0,}t{0,1}(?!ain))", RegexOptions.IgnoreCase);
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
    internal static ClientInstanceMethod<string> SendChangeText;
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