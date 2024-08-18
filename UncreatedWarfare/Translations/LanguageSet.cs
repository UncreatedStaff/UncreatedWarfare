using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Translations;

public struct LanguageSet : IEnumerable<WarfarePlayer>, IEnumerator<WarfarePlayer>
{
    private ArraySegment<WarfarePlayer> _players;
    private int _index;
    internal int StartIndex;
    internal int Count;
    public readonly ArraySegment<WarfarePlayer> Players => _players;
    public WarfarePlayer Next;
    public readonly WarfarePlayer Current => Next;
    readonly object IEnumerator.Current => Next;
    public LanguageInfo Language { get; }
    public CultureInfo Culture { get; }
    public bool IMGUI { get; }
    public Team Team { get; }
    public LanguageSet(LanguageInfo language, CultureInfo culture, bool imgui, Team team)
    {
        Language = language;
        Culture = culture;
        Team = team;
        IMGUI = imgui;
        _index = -1;

        Reset();
    }

    public LanguageSet(in LanguageSet set)
    {
        _players = set._players;
        Language = set.Language;
        Culture = set.Culture;
        Team = set.Team;
        IMGUI = set.IMGUI;
        _index = -1;

        Reset();
    }

    internal void SetPlayers(ArraySegment<WarfarePlayer> players) => _players = players;

    /// <summary>
    /// Check if a player's language settings is on par with this set.
    /// </summary>
    public readonly bool Includes(WarfarePlayer player)
    {
        return player.Save.IMGUI == IMGUI
               && player.Locale.LanguageInfo.Equals(Language)
               && player.Locale.CultureInfo.Equals(Culture)
               && player.Team == Team;
    }

    public readonly PooledTransportConnectionList GatherTransportConnections()
    {
        PooledTransportConnectionList list = Data.GetPooledTransportConnectionList(Players.Count);
        foreach (WarfarePlayer pl in Players)
        {
            if (pl.IsOnline)
                list.Add(pl.Connection);
        }

        return list;
    }

    public void Reset()
    {
        _index = -1;
    }

    public bool MoveNext()
    {
        ++_index;
        if (_index >= _players.Count)
            return false;

        Next = _players[_index];
        return true;
    }

    public LanguageSet GetEnumerator()
    {
        return this with { _index = -1 };
    }

    IEnumerator<WarfarePlayer> IEnumerable<WarfarePlayer>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly void Dispose() { }
}