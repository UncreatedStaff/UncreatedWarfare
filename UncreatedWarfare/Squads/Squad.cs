using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Squads;

public class Squad : ITranslationArgument
{
    // todo: something is wrong with squads, a few times errors are being thrown for players not being in squads or being in more than one
    public const int MaxMembers = 6;
    private readonly List<WarfarePlayer> _members;
    private readonly SquadManager _squadManager;
    public string Name { get; private set; }
    /// <summary>
    /// A number starting at 1 that uniquely identifies the squad among the squads of a specific team.
    /// </summary>
    public byte TeamIdentificationNumber { get; }
    public Team Team { get; }
    public IReadOnlyList<WarfarePlayer> Members { get; }
    public WarfarePlayer Leader => _members[0];
    public bool IsFull => _members.Count >= MaxMembers;
    public bool IsLocked { get; private set; }

    internal Squad(Team team, string squadName, byte teamIdentificationNumber, SquadManager squadManager)
    {
        _members = new List<WarfarePlayer>();
        Members = _members.AsReadOnly();
        Name = squadName;
        TeamIdentificationNumber = teamIdentificationNumber;
        Team = team;
        IsLocked = false;
        _squadManager = squadManager;
    }

    public bool Disband()
    {
        return _squadManager.DisbandSquad(this);
    }

    internal void AddMemberWithoutNotify(WarfarePlayer player)
    {
        _members.Add(player);
        player.Component<SquadPlayerComponent>().ChangeSquad(this);
        player.Save.SquadTeamIdentificationNumber = (byte)TeamIdentificationNumber;
    }

    public bool TryAddMember(WarfarePlayer player)
    {
        if (player.IsInSquad())
        {
            return false;
        }

        if (!CanJoinSquad(player))
            return false;

        AddMember(player);
        return true;
    }

    public bool CanJoinSquad(WarfarePlayer player)
    {
        if (Members.Count >= MaxMembers || Members.Contains(player))
            return false;

        if (!IsLocked)
            return true;

        if (Members.Count == 0)
            return false;

        WarfarePlayer leader = Members[0];
        if (leader.UnturnedPlayer.channel.owner.playerID.group == player.UnturnedPlayer.channel.owner.playerID.group)
        {
            // same steam group
            return true;
        }

        // leader has player in friends
        if (Array.IndexOf(leader.SteamFriends, player.Steam64.m_SteamID) != -1)
            return true;

        // player has leader in friends (if the leader has a private friends list but the player doesn't this will sometimes fix it)
        if (Array.IndexOf(player.SteamFriends, leader.Steam64.m_SteamID) != -1)
            return true;

        return false;
    }

    public bool AddMember(WarfarePlayer player)
    {
        if (ContainsPlayer(player))
            return false;

        AddMemberWithoutNotify(player);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberJoined { Squad = this, Player = player, IsNewSquad = false });
        return true;
    }

    public void PromoteMember(WarfarePlayer member)
    {
        int index = _members.IndexOf(member);

        WarfarePlayer leader = _members[0];
        if (leader.Equals(member))
            return;

        _members[0] = member;
        _members[index] = leader;

        bool didUpdateName = false;
        if (Name.Equals($"{leader.Names.CharacterName}'s Squad", StringComparison.Ordinal))
        {
            didUpdateName = true;
            Name = $"{member.Names.CharacterName}'s Squad".TruncateWithEllipses(32);
        }

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLeaderUpdated { Squad = this, OldLeader = leader, NewLeader = member, DidUpdateName = didUpdateName });
    }

    public bool RemoveMember(WarfarePlayer player)
    {
        int index = _members.IndexOf(player);
        if (index == -1)
            return false;

        if (index == 0)
        {
            if (_members.Count == 1)
            {
                Disband();
                return true;
            }

            PromoteMember(_members[1]);
            if (!_members[1].Equals(player))
                return false;
            index = 1;
        }

        _members.RemoveAt(index);
        player.Save.SquadTeamIdentificationNumber = 0;
        player.Component<SquadPlayerComponent>().ClearSquad();
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberLeft { Squad = this, Player = player });
        return true;
    }

    public bool LockSquad(WarfarePlayer? instigator)
    {
        if (IsLocked)
            return false;

        IsLocked = true;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLockUpdated { Squad = this, NewLockState = true, Instigator = instigator });
        return true;
    }

    public bool UnlockSquad(WarfarePlayer? instigator)
    {
        if (!IsLocked)
            return false;

        IsLocked = false;
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadLockUpdated { Squad = this, NewLockState = false, Instigator = instigator });
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
    
    public ulong[] GetMemberList()
    {
        ulong[] arr = new ulong[_members.Count];
        for (int i = 0; i < arr.Length; ++i)
            arr[i] = _members[i].Steam64.m_SteamID;
        return arr;
    }

    internal void DisbandMembers()
    {
        foreach (WarfarePlayer player in _members)
        {
            player.Component<SquadPlayerComponent>().ClearSquad();
            player.Save.SquadTeamIdentificationNumber = 0;
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
        return $"sq-{TeamIdentificationNumber} \"{Name}\" @ {Team.Faction.Name} ({Members.Count}/{MaxMembers} members)";
    }
}