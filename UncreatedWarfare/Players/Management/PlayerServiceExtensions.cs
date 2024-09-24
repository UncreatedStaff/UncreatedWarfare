using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, [NotNullWhen(true)] Player? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNull(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, [NotNullWhen(true)] PlayerCaller? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNull(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>. Not to be invoked from any thread other than the game thread.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, [NotNullWhen(true)] SteamPlayer? steamPlayer)
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
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, [NotNullWhen(true)] Player? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNullThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, [NotNullWhen(true)] PlayerCaller? player)
    {
        return player == null ? null : playerService.GetOnlinePlayerOrNullThreadSafe(player.channel.owner.playerID.steamID.m_SteamID);
    }

    /// <summary>
    /// Get a player if they're online, otherwise <see langword="null"/>.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, [NotNullWhen(true)] SteamPlayer? steamPlayer)
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

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, string searchTerm, [InstantHandle] IEnumerable<WarfarePlayer> selection, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
        }

        return SearchPlayers(players, searchTerm, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, string searchTerm, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return playerService.GetOnlinePlayerOrNull(steamId);
        }

        ReadOnlyTrackingList<WarfarePlayer> players = playerService.OnlinePlayers;
        return SearchPlayers(players, searchTerm, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, string searchTerm, [InstantHandle] IEnumerable<WarfarePlayer> selection, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayerOrNull(searchTerm, preferredName);
        }

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
        }

        return SearchPlayers(players, searchTerm, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, string searchTerm, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayerOrNull(searchTerm, preferredName);
        }

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return playerService.GetOnlinePlayerOrNullThreadSafe(steamId);
        }

        IReadOnlyList<WarfarePlayer> players = playerService.GetThreadsafePlayerList();
        return SearchPlayers(players, searchTerm, preferredName);
    }

    private static WarfarePlayer? SearchPlayers([InstantHandle] IEnumerable<WarfarePlayer> players, ReadOnlySpan<char> searchTerm, PlayerNameType preferredName)
    {
        using IEnumerator<WarfarePlayer> enumerator = players.GetEnumerator();

        ReadOnlySpan<PlayerNameType> nameOrder = preferredName switch
        {
            PlayerNameType.CharacterName => [ PlayerNameType.CharacterName, PlayerNameType.NickName, PlayerNameType.PlayerName ],
            PlayerNameType.NickName => [ PlayerNameType.NickName, PlayerNameType.CharacterName, PlayerNameType.PlayerName ],
            PlayerNameType.PlayerName => [ PlayerNameType.PlayerName, PlayerNameType.CharacterName, PlayerNameType.NickName ],
            _ => default
        };

        const int levels = 6;
        for (int n = 0; n < 3; ++n)
        {
            PlayerNameType name = nameOrder[n];
            for (int i = 0; i < levels; i += 2)
            {
                WarfarePlayer? pl = SearchPlayersByNameType(enumerator, searchTerm, i, name);
                if (pl != null) return pl;
                pl = SearchPlayersByNameType(enumerator, searchTerm, i + 1, name);
                if (pl != null) return pl;
            }
        }

        return null;
    }

    private static WarfarePlayer? SearchPlayersByNameType([InstantHandle] IEnumerator<WarfarePlayer> enumerator, ReadOnlySpan<char> searchTerm, int level, PlayerNameType nameType)
    {
        switch (level)
        {
            case 0: // exact
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    if (name.AsSpan().Equals(searchTerm, StringComparison.Ordinal))
                    {
                        return player;
                    }
                }

                break;

            case 1: // exact case insensitive
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    if (name.AsSpan().Equals(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return player;
                    }
                }

                break;

            case 2: // contains
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    if (name.AsSpan().Contains(searchTerm, StringComparison.Ordinal))
                    {
                        return player;
                    }
                }

                break;

            case 3: // contains case insensitive
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    if (name.AsSpan().Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return player;
                    }
                }

                break;

            case 4: // fuzzy
                int maxScore = 2; // require 3 letter match at least
                WarfarePlayer? match = null;
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    int score = FormattingUtility.CompareStringsFuzzy(searchTerm, name, true);
                    
                    if (maxScore >= score)
                        continue;
                    
                    match = player;
                    maxScore = score;
                }

                if (match != null)
                    return match;

                break;

            case 5: // fuzzy case insensitive
                maxScore = 2; // require 3 letter match at least
                match = null;
                while (enumerator.MoveNext())
                {
                    WarfarePlayer player = enumerator.Current!;
                    string name = nameType switch
                    {
                        PlayerNameType.CharacterName => player.Names.CharacterName,
                        PlayerNameType.NickName => player.Names.NickName,
                        PlayerNameType.PlayerName => player.Names.PlayerName,
                        _ => null!
                    };
                    int score = FormattingUtility.CompareStringsFuzzy(searchTerm, name, false);
                    
                    if (maxScore >= score)
                        continue;

                    match = player;
                    maxScore = score;
                }

                if (match != null)
                    return match;

                break;
        }

        enumerator.Reset();
        return null;
    }
}
