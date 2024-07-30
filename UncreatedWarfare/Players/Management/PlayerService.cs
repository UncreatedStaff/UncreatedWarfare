using Microsoft.Extensions.Logging;
using SDG.Framework.Utilities;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util.List;
using UnityEngine;

namespace Uncreated.Warfare.Players.Management;
public class PlayerService
{
    private readonly TrackingList<WarfarePlayer> _onlinePlayers;
    private readonly Dictionary<ulong, WarfarePlayer> _onlinePlayersDictionary;
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers { get; }
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayersOnTeam(Team team) => _onlinePlayers.Where(p => p.Team == team).ToTrackingList().AsReadOnly();

    private readonly ILoggerFactory _loggerFactory; 
    private readonly ILogger<PlayerService> _logger;
    

    public PlayerService(ILoggerFactory loggerFactory)
    {
        _onlinePlayers = new TrackingList<WarfarePlayer>();
        _onlinePlayersDictionary = new Dictionary<ulong, WarfarePlayer>();
        OnlinePlayers = _onlinePlayers.AsReadOnly();
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PlayerService>();
    }

    public WarfarePlayer CreateWarfarePlayer(Player player)
    {
        WarfarePlayer joined = new WarfarePlayer(player, _loggerFactory.CreateLogger<WarfarePlayer>());
        _onlinePlayers.Add(joined);
        _onlinePlayersDictionary.Add(joined.Steam64.m_SteamID, joined);
        _logger.LogInformation($"Player {player} joined the server");
        return joined;
    }

    public WarfarePlayer OnPlayerLeft(Player player)
    {
        WarfarePlayer left = GetOnlinePlayer(player.channel.owner.playerID.steamID.m_SteamID);
        _onlinePlayers.Remove(left);
        _onlinePlayersDictionary.Remove(left.Steam64.m_SteamID);
        _logger.LogInformation($"Player {player} left the server");
        return left;
    }
    public WarfarePlayer GetOnlinePlayer(ulong steam64)
    {
        if (!_onlinePlayersDictionary.TryGetValue(steam64, out WarfarePlayer? player))
            throw new PlayerNotOnlineException(steam64);

        return player;
    }
    public WarfarePlayer GetOnlinePlayer(Player player) => GetOnlinePlayer(player.channel.owner.playerID.steamID.m_SteamID);
    public WarfarePlayer GetOnlinePlayer(SteamPlayer steamPlayer) => GetOnlinePlayer(steamPlayer.playerID.steamID.m_SteamID);
    public WarfarePlayer GetOnlinePlayer(CSteamID steamId) => GetOnlinePlayer(steamId.m_SteamID);
}
public class PlayerNotOnlineException : Exception
{
    public PlayerNotOnlineException(ulong steamId)
        : base($"Could not get WarfarePlayer '{steamId}' because they were not found in the list of online players.")
    { }
}
