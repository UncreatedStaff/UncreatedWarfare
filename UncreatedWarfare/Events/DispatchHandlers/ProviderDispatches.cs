using System;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Invoked by <see cref="Provider.onServerConnected"/> when a player successfully joins the server.
    /// </summary>
    private void ProviderOnServerConnected(CSteamID steam64)
    {
        SteamPlayer? steamPlayer = PlayerTool.getSteamPlayer(steam64.m_SteamID);

        if (_playerService is not PlayerService implPlayerService)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Invalid IPlayerService implementation player connecting: {0}.", steam64);
            Provider.kick(steam64, "Invalid player service setup.");
            return;
        }
        
        if (steamPlayer == null || steamPlayer.player == null)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Unknown player connected: {0}.", steam64);
            Provider.kick(steam64, "Can't find player.");
            return;
        }

        // update team
        Team team = Team.NoTeam;
        if (steamPlayer.player.quests.isMemberOfAGroup)
        {
            steamPlayer.player.quests.leaveGroup(true);
            /* todo make some kind of 'joining previous team...' section of the lobby
            if (!_warfare.IsLayoutActive())
            {
                steamPlayer.player.quests.leaveGroup(true);
            }
            else
            {
                team = _warfare.GetActiveLayout().TeamManager.GetTeam(steamPlayer.player.quests.groupID);
                if (team == Team.NoTeam)
                    steamPlayer.player.quests.leaveGroup(true);
            }
            */
        }

        ulong s64 = steam64.m_SteamID;

        int index = implPlayerService.PendingTasks.FindIndex(x => x.Player.Steam64.m_SteamID == s64);

        if (index == -1)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Can't find player's task data: {0}.", steam64);
            Provider.kick(steam64, "Can't find task data.");
            return;
        }

        PlayerService.PlayerTaskData data = implPlayerService.PendingTasks[index];
        implPlayerService.PendingTasks.RemoveAt(index);
        implPlayerService.PendingTasks.RemoveAll(x => !Provider.pending.Exists(y => y.playerID.steamID.m_SteamID == x.Player.Steam64.m_SteamID));

        WarfarePlayer newPlayer = implPlayerService.CreateWarfarePlayer(steamPlayer.player, in data);
        newPlayer.UpdateTeam(team);

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

        if (_playerService is not PlayerService implPlayerService)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerDisconnected));
            logger.LogError("Invalid IPlayerService implementation player disconnecting: {0}.", steam64);
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
            Team = player.Team,
            DisconnectToken = disconnectToken.Token
        };

        _ = DispatchEventAsync(args, CancellationToken.None);

        disconnectToken.Dispose();

        try
        {
            implPlayerService.OnPlayerLeft(player);
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