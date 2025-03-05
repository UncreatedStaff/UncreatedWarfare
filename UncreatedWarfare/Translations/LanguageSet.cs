using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations;

public struct LanguageSet : IEnumerable<WarfarePlayer>, IEnumerator<WarfarePlayer>
{
    private ArraySegment<WarfarePlayer> _players;
    private int _index;
    private bool _isSinglePlayer;
    internal int StartIndex;
    internal int Count;

    public WarfarePlayer Next;

    public readonly ArraySegment<WarfarePlayer> Players => _players;
    public readonly WarfarePlayer Current => Next;
    readonly object IEnumerator.Current => Next;
    public LanguageInfo Language { get; }
    public CultureInfo Culture { get; }
    public TimeZoneInfo TimeZone { get; }
    public bool IMGUI { get; }
    public Team Team { get; }
    public LanguageSet(LanguageInfo language, CultureInfo culture, TimeZoneInfo timeZone, bool imgui, Team team)
    {
        Language = language;
        Culture = culture;
        TimeZone = timeZone;
        Team = team;
        IMGUI = imgui;
        _index = -1;
    }

    public LanguageSet(in LanguageSet set)
    {
        _players = set._players;
        Language = set.Language;
        Culture = set.Culture;
        TimeZone = set.TimeZone;
        Team = set.Team;
        IMGUI = set.IMGUI;
        _index = -1;
    }

    public LanguageSet(WarfarePlayer player)
    {
        Next = player;
        _isSinglePlayer = true;
        _index = -1;
        IMGUI = player.Save.IMGUI;
        Language = player.Locale.LanguageInfo;
        Culture = player.Locale.CultureInfo;
        TimeZone = player.Locale.TimeZone;
        Team = player.Team;
    }

    public readonly LanguageSet Preserve()
    {
        LanguageSet set = new LanguageSet(Language, Culture, TimeZone, IMGUI, Team);
        set.SetPlayers(_isSinglePlayer
            ? _players
            : new ArraySegment<WarfarePlayer>(_players.ToArray())
        );
        set.StartIndex = 0;
        set.Count = _players.Count;
        return set;
    }

    internal void SetPlayers(ArraySegment<WarfarePlayer> players)
    {
        if (players.Count == 1)
        {
            _isSinglePlayer = true;
            Next = players[0];
        }
        else
        {
            _isSinglePlayer = false;
            _players = players;
        }
    }

    /// <summary>
    /// Check if a player's language settings is on par with this set.
    /// </summary>
    public readonly bool Includes(WarfarePlayer player)
    {
        if (_isSinglePlayer)
            return Next.Equals(player);

        return player.Save.IMGUI == IMGUI
               && player.Locale.LanguageInfo.Equals(Language)
               && player.Locale.CultureInfo.Name.Equals(Culture.Name, StringComparison.Ordinal)
               && player.Locale.TimeZone.Id.Equals(TimeZone.Id, StringComparison.Ordinal)
               && player.Team == Team;
    }

    public readonly PooledTransportConnectionList GatherTransportConnections()
    {
        if (_isSinglePlayer)
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(1);
            if (Next.IsOnline)
                list.Add(Next.Connection);
            return list;
        }
        else
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(_players.Count);
            WarfarePlayer[] arr = _players.Array!;
            int i = _players.Offset;
            int ct = i + _players.Count;
            for (; i < ct; i++)
            {
                WarfarePlayer pl = arr[i];
                if (pl.IsOnline)
                    list.Add(pl.Connection);
            }

            return list;
        }
    }

    public readonly PooledTransportConnectionList GatherTransportConnections(byte x, byte y, byte area)
    {
        if (_isSinglePlayer)
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(1);
            PlayerMovement movement = Next.UnturnedPlayer.movement;
            if (Next.IsOnline && Regions.checkArea(movement.region_x, movement.region_y, x, y, area))
                list.Add(Next.Connection);
            return list;
        }
        else
        {
            PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(_players.Count);
            WarfarePlayer[] arr = _players.Array!;
            int i = _players.Offset;
            int ct = i + _players.Count;
            for (; i < ct; i++)
            {
                WarfarePlayer pl = arr[i];
                PlayerMovement movement = pl.UnturnedPlayer.movement;
                if (pl.IsOnline && Regions.checkArea(movement.region_x, movement.region_y, x, y, area))
                    list.Add(pl.Connection);
            }

            return list;
        }
    }

    public void Reset()
    {
        _index = -1;
    }

    public bool MoveNext()
    {
        if (_isSinglePlayer)
        {
            if (_index != -1)
                return false;

            _index = 0;
            return true;

        }

        ++_index;
        if (_index >= _players.Count)
            return false;

        Next = _players[_index];
        return true;
    }

    public readonly LanguageSet GetEnumerator()
    {
        return this with { _index = -1 };
    }

    readonly IEnumerator<WarfarePlayer> IEnumerable<WarfarePlayer>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly void Dispose() { }
}