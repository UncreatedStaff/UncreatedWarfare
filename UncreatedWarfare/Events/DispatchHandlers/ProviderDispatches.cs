using Microsoft.Extensions.Logging;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Keeps track of data that's fetched during the connecting event.
    /// </summary>
    private static readonly List<PendingAsyncData> PendingAsyncData = new List<PendingAsyncData>(4);

    /// <summary>
    /// Invoked by <see cref="Provider.onServerConnected"/> when a player successfully joins the server.
    /// </summary>
    private void ProviderOnServerConnected(CSteamID steam64)
    {
        SteamPlayer? steamPlayer = PlayerTool.getSteamPlayer(steam64.m_SteamID);
        
        if (steamPlayer == null || steamPlayer.player == null)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Unknown player connected: {0}.", steam64);
            return;
        }

        int index = PendingAsyncData.FindIndex(x => x.Steam64 == steam64.m_SteamID);
        if (index == -1)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Unable to find async data from player: {0}.", steam64);
            Provider.kick(steam64, "Unable to find your async data. Contact a director.");
            return;
        }

        PendingAsyncData data = PendingAsyncData[index];

        PendingAsyncData.RemoveAt(index);
        PendingAsyncData.RemoveAll(x => !Provider.pending.Exists(y => y.playerID.steamID.m_SteamID == x.Steam64));

        // todo this will change
        UCPlayer player = PlayerManager.InvokePlayerConnected(steamPlayer.player, data, out bool isNewPlayer);

        PlayerJoined args = new PlayerJoined(player, isNewPlayer);

        _ = DispatchEventAsync(args, player.DisconnectToken);
    }
}