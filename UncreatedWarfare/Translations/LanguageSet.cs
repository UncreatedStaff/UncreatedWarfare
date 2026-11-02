using System.ComponentModel;
using System.Globalization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations;

/// <summary>
/// A group of one or more players with a common language, culture, time-zone, team, and IMGUI setting.
/// </summary>
public struct LanguageSet : IEnumerable<WarfarePlayer>, IEnumerator<WarfarePlayer>
{
    internal LanguageGroup? Group;
    public WarfarePlayer? Next;
    private int _index;

    public readonly WarfarePlayer? SingleOrNull
    {
        get
        {
            if (Group == null)
                return Next!;

            if (Group.Players.Count == 1)
                return Group.Players[0];

            return null;
        }
    }

    public readonly ArraySegment<WarfarePlayer> Players => Group?.Players ?? new ArraySegment<WarfarePlayer>(new WarfarePlayer[] { Next! });

    public readonly int Count => Group?.Count ?? 1;

#nullable disable
    public readonly WarfarePlayer Current => Next;
    readonly object IEnumerator.Current => Next;
#nullable restore
    public readonly LanguageInfo Language => Group?.Language ?? Next!.Locale.LanguageInfo;
    public readonly CultureInfo Culture => Group?.Culture ?? Next!.Locale.CultureInfo;
    public readonly TimeZoneInfo TimeZone => Group?.TimeZone ?? Next!.Locale.TimeZone;
    public readonly bool IMGUI => Group?.IMGUI ?? Next!.Save.IMGUI;
    public readonly Team Team => Group?.Team ?? Next!.Team;
    internal LanguageSet(LanguageGroup group)
    {
        Group = group;
        _index = -1;
    }

    public LanguageSet(LanguageInfo language, CultureInfo culture, TimeZoneInfo timeZone, bool imgui, Team team)
    {
        Group = new LanguageGroup(null);
        Group.Reset(language, culture, timeZone, imgui, team);
        Group.Players = new ArraySegment<WarfarePlayer>(Array.Empty<WarfarePlayer>());
    }

    public LanguageSet(WarfarePlayer player)
    {
        Next = player;
        Group = null;
        _index = -1;
    }

    [MustDisposeResource]
    public readonly LanguageSet Preserve()
    {
        if (Group == null)
        {
            return this;
        }

        WarfarePlayer[] players = Group.Players.ToArray();
        LanguageGroup group = Group.Service?.RentLanguageGroup() ?? new LanguageGroup(null);

        group.Players = new ArraySegment<WarfarePlayer>(players);
        group.Reset(Group.Language, Group.Culture, Group.TimeZone, Group.IMGUI, Group.Team);

        return new LanguageSet(group);
    }

    public readonly PooledTransportConnectionList GatherTransportConnections()
    {
        if (Group != null)
        {
            return Group.GatherTransportConnections();
        }

        PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(1);
        if (Next!.IsOnline)
            list.Add(Next.Connection);
        return list;

    }

    public readonly PooledTransportConnectionList GatherTransportConnections(byte x, byte y, byte area)
    {
        if (Group != null)
        {
            return Group.GatherTransportConnections(x, y, area);
        }

        PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(1);
        PlayerMovement movement = Next!.UnturnedPlayer.movement;
        if (Next.IsOnline && Regions.checkArea(movement.region_x, movement.region_y, x, y, area))
            list.Add(Next.Connection);
        return list;
    }

    public void Reset()
    {
        _index = -1;
    }

    [MemberNotNullWhen(true, nameof(Next))]
    public bool MoveNext()
    {
        if (Group == null)
        {
            if (_index != -1)
                return false;

            _index = 0;
#pragma warning disable CS8775
            return true;
#pragma warning restore CS8775

        }

        ++_index;
        if (_index >= Group.Players.Count)
            return false;

        Next = Group.Players[_index];
        return true;
    }

    public readonly LanguageSet GetEnumerator()
    {
        return this with { _index = -1 };
    }

    readonly IEnumerator<WarfarePlayer> IEnumerable<WarfarePlayer>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly void Dispose()
    {
        Group?.Return();
    }
}

internal sealed class LanguageGroup
{
    internal readonly LanguageSets? Service;
    internal ArraySegment<WarfarePlayer> Players;

    internal int StartIndex;
    internal int Count;

#nullable disable

    public LanguageInfo Language { get; private set; }
    public CultureInfo Culture { get; private set; }
    public TimeZoneInfo TimeZone { get; private set; }
    public bool IMGUI { get; private set; }
    public Team Team { get; private set; }

#nullable restore
    public LanguageGroup(LanguageSets? service)
    {
        Service = service;
    }

    internal void Reset(LanguageInfo language, CultureInfo culture, TimeZoneInfo timeZone, bool imgui, Team team)
    {
        Language = language;
        Culture = culture;
        TimeZone = timeZone;
        IMGUI = imgui;
        Team = team;
    }

    internal bool Includes(WarfarePlayer player, TranslationOptions options)
    {
        if ((options & TranslationOptions.PerTeamTranslation) != 0 && player.Team != Team)
        {
            return false;
        }

        if ((options & TranslationOptions.PerTimeZoneTranslation) != 0 && !player.Locale.TimeZone.Id.Equals(TimeZone.Id, StringComparison.Ordinal))
        {
            return false;
        }

        if ((options & TranslationOptions.PerCultureTranslation) != 0 && !player.Locale.CultureInfo.Name.Equals(Culture.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if ((options & TranslationOptions.PerLanguageTranslation) != 0 && !player.Locale.LanguageInfo.Equals(Language))
        {
            return false;
        }

        if ((options & TranslationOptions.ConsiderIMGUI) != 0 && player.Save.IMGUI != IMGUI)
        {
            return false;
        }

        return true;
    }

    internal PooledTransportConnectionList GatherTransportConnections()
    {
        PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(Players.Count);
        WarfarePlayer[] arr = Players.Array!;
        int i = Players.Offset;
        int ct = i + Players.Count;
        for (; i < ct; i++)
        {
            WarfarePlayer pl = arr[i];
            if (pl.IsOnline)
                list.Add(pl.Connection);
        }

        return list;
    }

    internal PooledTransportConnectionList GatherTransportConnections(byte x, byte y, byte area)
    {
        PooledTransportConnectionList list = TransportConnectionPoolHelper.Claim(Players.Count);
        WarfarePlayer[] arr = Players.Array!;
        int i = Players.Offset;
        int ct = i + Players.Count;
        for (; i < ct; i++)
        {
            WarfarePlayer pl = arr[i];
            PlayerMovement movement = pl.UnturnedPlayer.movement;
            if (pl.IsOnline && Regions.checkArea(movement.region_x, movement.region_y, x, y, area))
                list.Add(pl.Connection);
        }

        return list;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal void Return()
    {
        Service?.ReturnLanguageGroup(this);
    }
}