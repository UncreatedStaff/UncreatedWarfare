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

        Reset();
    }

    public LanguageSet(in LanguageSet set)
    {
        _players = set._players;
        Language = set.Language;
        Culture = set.Culture;
        Team = set.Team;
        IMGUI = set.IMGUI;

        Reset();
    }
    internal void SetPlayers(ArraySegment<WarfarePlayer> players) => _players = players;
    public readonly bool Includes(WarfarePlayer player)
    {
        return player.Save.IMGUI == IMGUI
               && player.Locale.LanguageInfo.Equals(Language)
               && player.Locale.CultureInfo.Equals(Culture)
               && player.Team == Team;
    }

    public void Reset()
    {
        _enumerator = Players.GetEnumerator();
    }

    public bool MoveNext()
    {
        if (!_enumerator.MoveNext())
        {
            Next = null!;
            return false;
        }
        
        Next = _enumerator.Current;
        return true;
    }

    public readonly IEnumerator<WarfarePlayer> GetEnumerator()
    {
        return _enumerator;
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return _enumerator;
    }

    public readonly void Dispose() { }
}