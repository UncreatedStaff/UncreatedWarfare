using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Squads;

public class Squad : ITranslationArgument
{
    public const int MaxMembers = 6;
    private readonly List<WarfarePlayer> _members;
    private readonly SquadManager _squadManager;
    public string Name { get; }
    public Team Team { get; }
    public IReadOnlyList<WarfarePlayer> Members { get; }
    public WarfarePlayer Leader => _members[0];
    public bool IsFull => _members.Count >= MaxMembers;
    public bool IsLocked { get; private set; }

    internal Squad(Team team, string squadName, SquadManager squadManager)
    {
        _members = new List<WarfarePlayer>();
        Members = _members.AsReadOnly();
        Name = squadName;
        Team = team;
        IsLocked = false;
        _squadManager = squadManager;
    }

    public bool Disband()
    {
        return _squadManager.DisbandSquad(this);
    }

    public bool AddMember(WarfarePlayer player)
    {
        if (ContainsPlayer(player))
            return false;

        bool isNewSquad = _members.Count == 0;
        _members.Add(player);
        player.Component<SquadPlayerComponent>().ChangeSquad(this);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberJoined { Squad = this, Player = player, IsNewSquad = isNewSquad });
        return true;
    }

    public bool RemoveMember(WarfarePlayer player)
    {
        int index = _members.IndexOf(player);
        if (index == -1)
            return false;

        WarfarePlayer oldLeader = _members[0];

        _members.RemoveAt(index);
        player.Component<SquadPlayerComponent>().ClearSquad();
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberLeft { Squad = this, Player = player });

        // leader removed
        if (index == 0 && _members.Count > 0)
        {
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLeaderUpdated { Squad = this, OldLeader = oldLeader, NewLeader = player });
        }

        if (_members.Count == 0)
        {
            _squadManager.DisbandSquad(this);
        }

        return true;
    }

    public bool LockSquad(WarfarePlayer player)
    {
        if (IsLocked)
            return false;

        IsLocked = true;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLockUpdated { Squad = this, NewLockState = true });
        return true;
    }

    public bool UnlockSquad(WarfarePlayer player)
    {
        if (!IsLocked)
            return false;

        IsLocked = false;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLockUpdated { Squad = this, NewLockState = false });
        return true;
    }

    public bool ContainsPlayer(WarfarePlayer player)
    {
        return _members.Contains(player);
    }

    public bool IsLeader(WarfarePlayer player)
    {
        return player.Equals(_members[0]);
    }

    internal void DisbandMembers()
    {
        foreach (WarfarePlayer player in _members)
        {
            player.Component<SquadPlayerComponent>().ClearSquad();
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberLeft { Squad = this, Player = player, IsForciblyDisbanded = true });
        }

        _members.Clear();
    }

    public static readonly SpecialFormat FormatColorName = new SpecialFormat("Colored Squad Name", "c");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Squad Name", "n");

    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return FormatColorName.Match(in parameters)
            ? formatter.Colorize(Name, Team.Faction.Color, parameters.Options)
            : Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Team.Id, Name);
    }

    public override string ToString()
    {
        return $"{Name}@{Team.Faction.Name}";
    }
}