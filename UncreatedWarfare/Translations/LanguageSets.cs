using System;
using System.Collections.Generic;
using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Translations;

public class LanguageSets
{
    private readonly PlayerService _playerService;
    public LanguageSets(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public LanguageSetEnumerator AllPlayers() => PlayersWhere(null);

    public LanguageSetEnumerator AllPlayersExcept(ulong steam64) => PlayersWhere(pl => pl.Steam64.m_SteamID != steam64);
    public LanguageSetEnumerator AllPlayersExcept(CSteamID steam64) => PlayersWhere(pl => pl.Steam64.m_SteamID != steam64.m_SteamID);
    public LanguageSetEnumerator AllPlayersExcept(IPlayer player) => PlayersWhere(pl => pl.Steam64.m_SteamID != player.Steam64.m_SteamID);

    public LanguageSetEnumerator AllPlayersExcept(params ulong[] steam64s) => PlayersWhere(pl => Array.IndexOf(steam64s, pl.Steam64.m_SteamID) == -1);
    public LanguageSetEnumerator AllPlayersExcept(params CSteamID[] steam64s) => PlayersWhere(pl => Array.IndexOf(steam64s, pl.Steam64) == -1);
    public LanguageSetEnumerator AllPlayersExcept(params IPlayer[] players) => PlayersWhere(pl => Array.FindIndex(players, pl2 => pl.Steam64.m_SteamID == pl.Steam64.m_SteamID) == -1);

    public LanguageSetEnumerator PlayersOnTeam(Team team) => PlayersWhere(pl => pl.Team == team);

    public LanguageSetEnumerator PlayersInArea(byte x, byte y, byte area) => PlayersWhere(player =>
    {
        PlayerMovement movement = player.UnturnedPlayer.movement;
        return Regions.checkArea(movement.region_x, movement.region_y, x, y, area);
    });

    public LanguageSetEnumerator PlayersWhere(Func<WarfarePlayer, bool>? selector)
    {
        ThreadUtil.assertIsGameThread();

        List<LanguageSet> sets = ListPool<LanguageSet>.claim();
        List<WarfarePlayer> players = ListPool<WarfarePlayer>.claim();
        
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
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
            LanguageSet set = sets[i];
            set.SetPlayers(new ArraySegment<WarfarePlayer>(playersArray, set.StartIndex, set.Count));
            sets[i] = set;
        }

        return new LanguageSetEnumerator(sets, players);
    }
}