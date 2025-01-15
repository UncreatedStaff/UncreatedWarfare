using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    public static TrackingWhereEnumerable<WarfarePlayer> OnlinePlayersOnTeam(this IPlayerService playerService, Team team) => TrackingListExtensions.Where(playerService.OnlinePlayers, team.PlayerSelector);

    /// <summary>
    /// Get the <see cref="IPlayer"/> object of a player that may or may not be offline.
    /// </summary>
    public static ValueTask<IPlayer> GetOfflinePlayer(this IPlayerService playerService, CSteamID steam64, IUserDataService userDataService, CancellationToken token = default)
    {
        return GetOfflinePlayer(playerService, steam64.m_SteamID, userDataService, token);
    }

    /// <summary>
    /// Get the <see cref="IPlayer"/> object of a player that may or may not be offline.
    /// </summary>
    public static async ValueTask<IPlayer> GetOfflinePlayer(this IPlayerService playerService, ulong steam64, IUserDataService userDataService, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        WarfarePlayer? onlinePlayer = playerService.GetOnlinePlayerOrNullThreadSafe(steam64);

        if (onlinePlayer != null)
            return onlinePlayer;

        PlayerNames names = await userDataService.GetUsernamesAsync(steam64, token);
        return new OfflinePlayer(in names);
    }

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
    /// Quickly check if a player is online.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public static bool IsPlayerOnline(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.IsPlayerOnline(steamId.m_SteamID);
    }

    /// <summary>
    /// Quickly check if a player is online.
    /// </summary>
    public static bool IsPlayerOnlineThreadSafe(this IPlayerService playerService, CSteamID steamId)
    {
        return playerService.IsPlayerOnlineThreadSafe(steamId.m_SteamID);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, string searchTerm, [InstantHandle] IEnumerable<WarfarePlayer> selection, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
        }

        return SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNull(this IPlayerService playerService, string searchTerm, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return playerService.GetOnlinePlayerOrNull(steamId.m_SteamID);
        }

        ReadOnlyTrackingList<WarfarePlayer> players = playerService.OnlinePlayers;
        return SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, string searchTerm, [InstantHandle] IEnumerable<WarfarePlayer> selection, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayerOrNull(searchTerm, culture, preferredName);
        }

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
        }

        return SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(this IPlayerService playerService, string searchTerm, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayerOrNull(searchTerm, culture, preferredName);
        }

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return playerService.GetOnlinePlayerOrNullThreadSafe(steamId.m_SteamID);
        }

        IReadOnlyList<WarfarePlayer> players = playerService.GetThreadsafePlayerList();
        return SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName);
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static int GetOnlinePlayers(this IPlayerService playerService, string searchTerm, [InstantHandle] IList<WarfarePlayer> output, [InstantHandle] IEnumerable<WarfarePlayer> selection, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            WarfarePlayer? pl = players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
            if (pl != null)
                output.Add(pl);

            return pl != null ? 1 : 0;
        }

        int stCt = output.Count;
        SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName, output);
        return output.Count - stCt;
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static int GetOnlinePlayers(this IPlayerService playerService, string searchTerm, [InstantHandle] IList<WarfarePlayer> output, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        GameThread.AssertCurrent();

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            WarfarePlayer? pl = playerService.GetOnlinePlayerOrNull(steamId.m_SteamID);
            if (pl != null)
                output.Add(pl);

            return pl != null ? 1 : 0;
        }

        ReadOnlyTrackingList<WarfarePlayer> players = playerService.OnlinePlayers;
        int stCt = output.Count;
        SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName, output);
        return output.Count - stCt;
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static int GetOnlinePlayersThreadSafe(this IPlayerService playerService, string searchTerm, [InstantHandle] IList<WarfarePlayer> output, [InstantHandle] IEnumerable<WarfarePlayer> selection, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayers(searchTerm, output, culture, preferredName);
        }

        List<WarfarePlayer> players = selection.ToList();
        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            WarfarePlayer? pl = players.Find(x => x.Steam64.m_SteamID == steamId.m_SteamID);
            if (pl != null)
                output.Add(pl);

            return pl != null ? 1 : 0;
        }

        int stCt = output.Count;
        SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName, output);
        return output.Count - stCt;
    }

    /// <summary>
    /// Search for a player by their name.
    /// </summary>
    public static int GetOnlinePlayersThreadSafe(this IPlayerService playerService, string searchTerm, [InstantHandle] IList<WarfarePlayer> output, CultureInfo? culture, PlayerNameType preferredName = PlayerNameType.CharacterName)
    {
        if (GameThread.IsCurrent)
        {
            return playerService.GetOnlinePlayers(searchTerm, output, culture, preferredName);
        }

        if (FormattingUtility.TryParseSteamId(searchTerm, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            WarfarePlayer? pl = playerService.GetOnlinePlayerOrNullThreadSafe(steamId.m_SteamID);
            if (pl != null)
                output.Add(pl);

            return pl != null ? 1 : 0;
        }

        IReadOnlyList<WarfarePlayer> players = playerService.GetThreadsafePlayerList();
        int stCt = output.Count;
        SearchPlayers(players, searchTerm, culture ?? CultureInfo.InvariantCulture, preferredName, output);
        return output.Count - stCt;
    }

    private static WarfarePlayer? SearchPlayers([InstantHandle] IEnumerable<WarfarePlayer> players, string searchTerm, CultureInfo culture, PlayerNameType preferredName)
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
                WarfarePlayer? pl = SearchPlayersByNameType(enumerator, searchTerm, culture, i, name);
                if (pl != null) return pl;
                pl = SearchPlayersByNameType(enumerator, searchTerm, culture, i + 1, name);
                if (pl != null) return pl;
            }
        }

        return null;
    }

    private static void SearchPlayers([InstantHandle] IEnumerable<WarfarePlayer> players, string searchTerm, CultureInfo culture, PlayerNameType preferredName, IList<WarfarePlayer> output)
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
                SearchPlayersByNameType(enumerator, searchTerm, i, culture, name, output);
                SearchPlayersByNameType(enumerator, searchTerm, i + 1, culture, name, output);
            }
        }
    }

    private static WarfarePlayer? SearchPlayersByNameType([InstantHandle] IEnumerator<WarfarePlayer> enumerator, string searchTerm, CultureInfo culture, int level, PlayerNameType nameType)
    {
        CompareInfo cultureInfo = culture.CompareInfo;
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
                    if (cultureInfo.Compare(name, searchTerm) == 0)
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
                    if (cultureInfo.Compare(name, searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType) == 0)
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
                    if (cultureInfo.IndexOf(name, searchTerm) >= 0)
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
                    if (cultureInfo.IndexOf(name, searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType) >= 0)
                    {
                        return player;
                    }
                }

                break;

            case 4: // fuzzy
                int minScore = (int)Math.Ceiling(searchTerm.Length * 0.6d); // match must be at least 60% of the original search term
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
                    int score = StringUtility.LevenshteinDistance(searchTerm, name, culture, LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace | LevenshteinOptions.AutoComplete);
                    
                    if (minScore <= score)
                        continue;
                    match = player;
                    
                    minScore = score;
                }

                if (match != null)
                    return match;

                break;

            case 5: // fuzzy case insensitive
                minScore = (int)Math.Ceiling(searchTerm.Length * 0.6d); // match must be at least 60% of the original search term
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
                    int score = StringUtility.LevenshteinDistance(name, searchTerm, culture, LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace | LevenshteinOptions.AutoComplete);

                    if (minScore <= score)
                        continue;
                    match = player;

                    minScore = score;
                }

                if (match != null)
                    return match;

                break;
        }

        enumerator.Reset();
        return null;
    }

    private static void SearchPlayersByNameType([InstantHandle] IEnumerator<WarfarePlayer> enumerator, string searchTerm, int level, CultureInfo culture, PlayerNameType nameType, IList<WarfarePlayer> output)
    {
        CompareInfo cultureInfo = culture.CompareInfo;
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
                    if (cultureInfo.Compare(name, searchTerm) == 0 && !output.Contains(player))
                    {
                        output.Add(player);
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
                    if (cultureInfo.Compare(name, searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType) == 0 && !output.Contains(player))
                    {
                        output.Add(player);
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
                    if (cultureInfo.IndexOf(name, searchTerm) >= 0 && !output.Contains(player))
                    {
                        output.Add(player);
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
                    if (cultureInfo.IndexOf(name, searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType) >= 0 && !output.Contains(player))
                    {
                        output.Add(player);
                    }
                }

                break;

            case 4: // fuzzy
                int stepStart = output.Count;
                int minScore = (int)Math.Ceiling(searchTerm.Length * 0.6d); // match must be at least 60% of the original search term
                List<int> scores = new List<int>(16);
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

                    if (output.Contains(player))
                        continue;

                    int score = StringUtility.LevenshteinDistance(name, searchTerm, culture, LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace | LevenshteinOptions.AutoComplete);


                    if (minScore < score)
                        continue;

                    bool added = false;
                    for (int i = stepStart; i < output.Count; ++i)
                    {
                        int otherScore = scores[i - stepStart];
                        if (otherScore <= score)
                            continue;

                        output.Insert(i, player);
                        scores.Insert(i - stepStart, score);
                        added = true;
                        break;
                    }

                    if (added)
                        continue;

                    output.Add(player);
                    scores.Add(score);
                }

                break;

            case 5: // fuzzy case insensitive
                stepStart = output.Count;
                minScore = (int)Math.Ceiling(searchTerm.Length * 0.6d); // match must be at least 60% of the original search term
                scores = new List<int>(16);
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

                    if (output.Contains(player))
                        continue;

                    int score = StringUtility.LevenshteinDistance(name, searchTerm, culture, LevenshteinOptions.IgnoreCase | LevenshteinOptions.IgnorePunctuation | LevenshteinOptions.IgnoreWhitespace | LevenshteinOptions.AutoComplete);

                    if (minScore < score)
                        continue;

                    bool added = false;
                    for (int i = stepStart; i < output.Count; ++i)
                    {
                        int otherScore = scores[i - stepStart];
                        if (otherScore <= score)
                            continue;

                        output.Insert(i, player);
                        scores.Insert(i - stepStart, score);
                        added = true;
                        break;
                    }

                    if (added)
                        continue;

                    output.Add(player);
                    scores.Add(score);
                }
                break;
        }

        enumerator.Reset();
    }
}
