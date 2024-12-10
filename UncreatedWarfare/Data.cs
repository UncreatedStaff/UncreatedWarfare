
//#define SHOW_BYTES

using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare;

public static class Data
{
    public static class Paths
    {
        public static readonly string BaseDirectory = Path.Combine(Environment.CurrentDirectory, "Uncreated", "Warfare") + Path.DirectorySeparatorChar;
        public static readonly string LangStorage = Path.Combine(BaseDirectory, "Lang") + Path.DirectorySeparatorChar;
        public static readonly string Logs = Path.Combine(BaseDirectory, "Logs") + Path.DirectorySeparatorChar;
        public static readonly string ActionLog = Path.Combine(Logs, "ActionLog") + Path.DirectorySeparatorChar;
        public static readonly string Heartbeat = Path.Combine(BaseDirectory, "Stats", "heartbeat.dat");
        public static readonly string HeartbeatBackup = Path.Combine(BaseDirectory, "Stats", "heartbeat_last.dat");
    }


    public const string SuppressCategory = "Microsoft.Performance";
    public const string SuppressID = "IDE0051";
    public static CultureInfo LocalLocale = Languages.CultureEnglishUS; // todo set from config
    public static Dictionary<ulong, UCPlayerData> PlaytimeComponents;
    public static bool UseElectricalGrid;
    internal static ClientInstanceMethod<byte[]>? SendUpdateBarricadeState;
    internal static ClientStaticMethod<uint, byte, byte>? SendSwapVehicleSeats;
    // internal static ClientInstanceMethod? SendScreenshotDestination;

    internal static InstanceGetter<UseableGun, bool>? GetUseableGunReloading;
    internal static Action<Vector3, Vector3, string, Transform?, List<ITransportConnection>>? ServerSpawnLegacyImpact;
    internal static SteamPlayer NilSteamPlayer;


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