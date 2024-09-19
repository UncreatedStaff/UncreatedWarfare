using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Made these as extensions so if <see cref="IPlayerService"/> is used elsewhere the reliance on Assembly-CSharp isn't necessarily required.
/// </summary>
public static class PlayerServiceExtensions
{
    /// <summary>
    /// List of all online players on a given team. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static TrackingWhereEnumerable<WarfarePlayer> OnlinePlayersOnTeam(this IPlayerService playerService, Team team) => playerService.OnlinePlayers.Where(p => p.Team == team);

    /// <summary>
    /// Gets an array of the Steam64 IDs of all online players as an array for database queries.
    /// </summary>
    public static ulong[] GetOnlinePlayerSteam64Array(this IPlayerService playerService)
    {
        IReadOnlyList<WarfarePlayer> plList;
        if (GameThread.IsCurrent)
        {
            if (playerService is PlayerService)
            {
                List<SteamPlayer> players = Provider.clients;
                ulong[] ids = new ulong[players.Count];
                for (int i = 0; i < ids.Length; ++i)
                {
                    ids[i] = players[i].playerID.steamID.m_SteamID;
                }

                return ids;
            }

            plList = playerService.OnlinePlayers;
        }
        else
        {
            plList = playerService.GetThreadsafePlayerList();
        }

        ulong[] outIds = new ulong[plList.Count];
        int ind = -1;
        foreach (WarfarePlayer player in plList)
        {
            outIds[++ind] = player.Steam64.m_SteamID;
        }

        return outIds;
    }

    /// <summary>
    /// Get a player who's known to be online. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayer(this IPlayerService playerService, Player player)
    {
        return playerService.GetOnlinePlayer(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayer(this IPlayerService playerService, PlayerCaller player)
    {
        return playerService.GetOnlinePlayer(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayer(this IPlayerService playerService, SteamPlayer steamPlayer)
    {
        return playerService.GetOnlinePlayer(steamPlayer.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayer(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.GetOnlinePlayer(steamId.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, Player? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNull(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, PlayerCaller? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNull(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, SteamPlayer? steamPlayer)
    {
        return steamPlayer == null ? null : playerService.GetOnlinePlayerOrNull(steamPlayer.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.GetOnlinePlayerOrNull(steamId.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online.
    /// </summary>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayerThreadSafe(this IPlayerService playerService, Player player)
    {
        return playerService.GetOnlinePlayerThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online.
    /// </summary>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayerThreadSafe(this IPlayerService playerService, PlayerCaller player)
    {
        return playerService.GetOnlinePlayerThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online.
    /// </summary>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayerThreadSafe(this IPlayerService playerService, SteamPlayer steamPlayer)
    {
        return playerService.GetOnlinePlayerThreadSafe(steamPlayer.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player who's known to be online.
    /// </summary>
    /// <exception cref="PlayerOfflineException"/>
    public static WarfarePlayer GetOnlinePlayerThreadSafe(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.GetOnlinePlayerThreadSafe(steamId.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, Player? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNullThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, PlayerCaller? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNullThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, SteamPlayer? steamPlayer)
    {
        return steamPlayer == null ? null : playerService.GetOnlinePlayerOrNullThreadSafe(steamPlayer.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.GetOnlinePlayerOrNullThreadSafe(steamId.m_SteamID);
    }
}
