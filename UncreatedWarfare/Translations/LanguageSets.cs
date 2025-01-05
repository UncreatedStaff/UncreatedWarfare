using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations;

public class LanguageSets
{
    private readonly IPlayerService _playerService;
    public LanguageSets(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    public LanguageSetEnumerator AllPlayers() => PlayersWhere(null);

    public LanguageSetEnumerator AllPlayersExcept(ulong steam64) => PlayersWhere(pl => pl.Steam64.m_SteamID != steam64);
    public LanguageSetEnumerator AllPlayersExcept(CSteamID steam64) => PlayersWhere(pl => pl.Steam64.m_SteamID != steam64.m_SteamID);
    public LanguageSetEnumerator AllPlayersExcept(IPlayer player) => PlayersWhere(pl => pl.Steam64.m_SteamID != player.Steam64.m_SteamID);

    // ReSharper disable InconsistentNaming
    public LanguageSetEnumerator AllPlayersExcept(params ulong[] steam64s) => PlayersWhere(pl => Array.IndexOf(steam64s, pl.Steam64.m_SteamID) == -1);
    public LanguageSetEnumerator AllPlayersExcept(params CSteamID[] steam64s) => PlayersWhere(pl => Array.IndexOf(steam64s, pl.Steam64) == -1);
    public LanguageSetEnumerator AllPlayersExcept(params IPlayer[] players) => PlayersWhere(pl => Array.FindIndex(players, pl2 => pl.Steam64.m_SteamID == pl2.Steam64.m_SteamID) == -1);
    // ReSharper restore InconsistentNaming

    public LanguageSetEnumerator PlayersOnTeam(Team team) => PlayersWhere(pl => pl.Team == team);

    public LanguageSetEnumerator PlayersOnTeam() => PlayersWhere(pl => pl.Team.IsValid);

    public LanguageSetEnumerator PlayersInArea(byte x, byte y, byte area) => PlayersWhere(player =>
    {
        PlayerMovement movement = player.UnturnedPlayer.movement;
        return Regions.checkArea(movement.region_x, movement.region_y, x, y, area);
    });

    public LanguageSetEnumerator PlayersWhere(Func<WarfarePlayer, bool>? selector)
    {
        IReadOnlyList<WarfarePlayer> playerList = GameThread.IsCurrent ? _playerService.OnlinePlayers : _playerService.GetThreadsafePlayerList();

        bool pool = GameThread.IsCurrent;

        List<LanguageSet> sets = pool ? ListPool<LanguageSet>.claim() : new List<LanguageSet>(selector == null ? 8 : 4);
        List<WarfarePlayer> players = pool ? ListPool<WarfarePlayer>.claim() : new List<WarfarePlayer>(selector == null ? Provider.clients.Count : Provider.clients.Count / 2);
        
        foreach (WarfarePlayer player in playerList)
        {
            if (selector != null && !selector(player))
                continue;

            bool found = false;
            for (int i = 0; i < sets.Count; ++i)
            {
                LanguageSet languageSet = sets[i];
                if (!languageSet.Includes(player))
                {
                    continue;
                }
                
                players.Insert(languageSet.StartIndex + languageSet.Count, player);
                ++languageSet.Count;
                sets[i] = languageSet;
                for (int j = i + 1; j < sets.Count; ++j)
                {
                    languageSet = sets[j];
                    ++languageSet.StartIndex;
                    sets[j] = languageSet;
                }

                found = true;
                break;
            }

            if (found)
                continue;

            LanguageSet newSet = new LanguageSet(player.Locale.LanguageInfo, player.Locale.CultureInfo, player.Save.IMGUI, player.Team);
            newSet.StartIndex = players.Count;
            newSet.Count = 1;
            players.Add(player);
            sets.Add(newSet);
        }
        
        WarfarePlayer[] playersArray = players.GetUnderlyingArrayOrCopy();
        for (int i = 0; i < sets.Count; ++i)
        {
            LanguageSet langSet = sets[i];
            langSet.SetPlayers(new ArraySegment<WarfarePlayer>(playersArray, langSet.StartIndex, langSet.Count));
            sets[i] = langSet;
        }

        return new LanguageSetEnumerator(sets, players, pool);
    }

    public LanguageSetEnumerator PlayersIn(IEnumerable<WarfarePlayer> set)
    {
        bool pool = GameThread.IsCurrent;

        List<LanguageSet> sets = pool ? ListPool<LanguageSet>.claim() : new List<LanguageSet>(4);
        List<WarfarePlayer> players = pool ? ListPool<WarfarePlayer>.claim() : new List<WarfarePlayer>(8);

        foreach (WarfarePlayer player in set)
        {
            if (!player.IsOnline)
                continue;

            bool found = false;
            for (int i = 0; i < sets.Count; ++i)
            {
                LanguageSet languageSet = sets[i];
                if (!languageSet.Includes(player))
                {
                    continue;
                }

                players.Insert(languageSet.StartIndex + languageSet.Count, player);
                ++languageSet.Count;
                sets[i] = languageSet;
                for (int j = i + 1; j < sets.Count; ++j)
                {
                    languageSet = sets[j];
                    ++languageSet.StartIndex;
                    sets[j] = languageSet;
                }

                found = true;
                break;
            }

            if (found)
                continue;

            LanguageSet newSet = new LanguageSet(player.Locale.LanguageInfo, player.Locale.CultureInfo, player.Save.IMGUI, player.Team);
            newSet.StartIndex = players.Count;
            newSet.Count = 1;
            players.Add(player);
            sets.Add(newSet);
        }

        WarfarePlayer[] playersArray = players.GetUnderlyingArrayOrCopy();
        for (int i = 0; i < sets.Count; ++i)
        {
            LanguageSet langSet = sets[i];
            langSet.SetPlayers(new ArraySegment<WarfarePlayer>(playersArray, langSet.StartIndex, langSet.Count));
            sets[i] = langSet;
        }

        return new LanguageSetEnumerator(sets, players, pool);
    }
}