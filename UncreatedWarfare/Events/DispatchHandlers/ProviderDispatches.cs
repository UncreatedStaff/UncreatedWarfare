using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management.Legacy;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Keeps track of data that's fetched during the connecting event.
    /// </summary>
    internal static readonly List<PendingAsyncData> PendingAsyncData = new List<PendingAsyncData>(4);

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

        int index = PendingAsyncData.FindIndex(x => x.Steam64.m_SteamID == steam64.m_SteamID);
        if (index == -1)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Unable to find async data from player: {0}.", steam64);
            Provider.kick(steam64, "Unable to find your async data. Contact a director.");
            return;
        }

        PendingAsyncData data = PendingAsyncData[index];

        PendingAsyncData.RemoveAt(index);
        PendingAsyncData.RemoveAll(x => !Provider.pending.Exists(y => y.playerID.steamID.m_SteamID == x.Steam64.m_SteamID));

        WarfarePlayer newPlayer = _playerService.CreateWarfarePlayer(steamPlayer.player);

        PlayerJoined args = new PlayerJoined
        {
            Player = newPlayer,
            SaveData = newPlayer.Save,
            IsNewPlayer = !newPlayer.Save.WasReadFromFile
        };

        _ = DispatchEventAsync(args, newPlayer.DisconnectToken);
    }

    /// <summary>
    /// Invoked by <see cref="Provider.onServerDisconnected"/> when a player is disconnected from the server.
    /// </summary>
    private void ProviderOnServerDisconnected(CSteamID steam64)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(steam64);
        if (player == null)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerDisconnected));
            logger.LogError("Unknown player disconnected from server: {0}.", steam64);
            return;
        }

        player.StartDisconnecting();

        Transform t = player.Transform;
        Transform aim = player.UnturnedPlayer.look.aim;
        CancellationTokenSource disconnectToken = new CancellationTokenSource();

        PlayerLeft args = new PlayerLeft
        {
            Player = player,
            Position = t.position,
            Rotation = t.rotation,
            LookPosition = aim.position,
            LookForward = aim.forward,
            Team = player.UnturnedPlayer == null ? null : new Team { GroupId = new CSteamID(player.GetTeam()), Faction = player.Faction, Id = (int)player.GetTeam() }, // todo
            DisconnectToken = disconnectToken.Token
        };

        _ = DispatchEventAsync(args, CancellationToken.None);

        disconnectToken.Dispose();

        try
        {
            _playerService.OnPlayerLeft(player);
        }
        catch (Exception ex)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerDisconnected));
            logger.LogError(ex, "Failed to remove player {0} from player manager.", steam64);
        }

        // collect player data, including the large voice buffer
        GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: true);
    }

    /// <summary>
    /// Invoked by <see cref="Provider.onBattlEyeKick"/> when a player gets kicked by BattlEye.
    /// </summary>
    private void ProviderOnBattlEyeKick(SteamPlayer client, string reason)
    {
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(client);
        if (player == null)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onBattlEyeKick));
            logger.LogError("Unknown player kicked by BattlEye: {0}, {1}.", client.playerID.steamID, reason);
            return;
        }

        BattlEyeKicked args = new BattlEyeKicked
        {
            Player = player,
            KickReason = reason,
            GlobalBanId = reason.StartsWith("Global Ban #", StringComparison.Ordinal) && reason.Length > 12 ? reason[12..] : null
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }
}