using System;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="Provider.onEnemyConnected"/> when a player successfully joins the server but just before <see cref="ProviderOnServerConnected"/>.
    /// </summary>
    private void ProviderOnServerConnectedEarly(SteamPlayer steamPlayer)
    {
        CSteamID steam64 = steamPlayer.playerID.steamID;
        if (_playerService is not PlayerService implPlayerService)
        {
            ILogger logger = GetLogger(typeof(Provider), nameof(Provider.onServerConnected));
            logger.LogError("Invalid IPlayerService implementation player connecting: {0}.", steam64);
            Provider.kick(steam64, "Invalid player service setup.");
            return;
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

        for (int i = implPlayerService.PendingTasks.Count - 1; i >= 0; --i)
        {
            PlayerService.PlayerTaskData d = implPlayerService.PendingTasks[i];
            if (Provider.pending.Exists(y => y.playerID.steamID.m_SteamID == d.Player.Steam64.m_SteamID))
                continue;

            implPlayerService.PendingTasks.RemoveAt(i);
            if (d.Scope == null)
                continue;

            Task.Run(async () =>
            {
                try
                {
                    await d.Scope.DisposeAsync();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing player task scope for {0}.", d.Player.Steam64);
                }
            }, CancellationToken.None);
        }

        ILifetimeScope? scope = data.Scope;

        WarfarePlayer player = implPlayerService.CreateWarfarePlayer(steamPlayer.player, in data);
        player.Data["PendingDataScope"] = scope;

        PlayerEarlyJoined args = new PlayerEarlyJoined
        {
            Player = player,
            SaveData = player.Save,
            IsNewPlayer = !player.Save.WasReadFromFile
        };

        _ = DispatchEventAsync(args, CancellationToken.None, allowAsync: false);
    }

    /// <summary>
    /// Invoked by <see cref="Provider.onServerConnected"/> when a player successfully joins the server.
    /// </summary>
    private void ProviderOnServerConnected(CSteamID steam64)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(steam64);

        SubscribePlayerEvents(player);

        ((PlayerService)_playerService).FinishConnectingPlayer(player);

        PlayerJoined args = new PlayerJoined
        {
            Player = player,
            SaveData = player.Save,
            IsNewPlayer = !player.Save.WasReadFromFile
        };

        UniTask.Create(async () =>
        {
            await DispatchEventAsync(args, args.Player.DisconnectToken);
            if (args.Player.Data.TryRemove("PendingDataScope", out object? obj) && obj is ILifetimeScope scope)
                await scope.DisposeAsync();
            args.Player.EndConnecting();
        });

        player.UnturnedPlayer.sendTerminalRelay("michael smells");
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
            TimeOnline = DateTime.UtcNow - player.JoinTime,
            LookPosition = aim.position,
            LookForward = aim.forward,
            Team = player.Team,
            DisconnectToken = disconnectToken.Token
        };

        _ = DispatchEventAsync(args, CancellationToken.None);

        args.Player.Save.Save(); // remember to save the player before they leave. This should happen once and only once (here and not in any other PlayerLeft listeners)

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

        if (args.Player.Data.TryRemove("PendingDataScope", out object? obj) && obj is ILifetimeScope scope)
        {
            Task.Run(async () =>
            {
                try
                {
                    await scope.DisposeAsync();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing player task scope for {0}.", args.Player.Steam64);
                }
            }, CancellationToken.None);
        }
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