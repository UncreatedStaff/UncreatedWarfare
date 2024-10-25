using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Events.Models.Fobs;
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
    private List<WarfarePlayer> _members { get; }
    public string Name { get; }
    public Team Team { get; }
    public ReadOnlyCollection<WarfarePlayer> Members { get; }
    public WarfarePlayer Leader => Members.First();
    public bool IsFull => _members.Count >= MaxMembers;
    public bool IsLocked { get; private set; }

    public Squad(WarfarePlayer squadLeader, string squadName)
    {
        _members = new List<WarfarePlayer>();
        Members = _members.AsReadOnly();
        Name = squadName;
        Team = squadLeader.Team;
        IsLocked = false;
        AddMember(squadLeader);
    }
    public bool AddMember(WarfarePlayer player)
    {
        if (_members.Contains(player))
            return false;

        bool isNewSquad = Members.Count == 0;
        _members.Add(player);
        player.Component<SquadPlayerComponent>().ChangeSquad(this);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberJoined { Squad = this, Player = player, IsNewSquad = isNewSquad });
        return true;
    }
    public bool RemoveMember(WarfarePlayer player)
    {
        if (!_members.Remove(player))
            return false;

        player.Component<SquadPlayerComponent>().ClearSquad();
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberLeft { Squad = this, Player = player });
        return true;
    }
    public void DisbandMembers()
    {
        foreach (WarfarePlayer player in _members)
        {
            player.Component<SquadPlayerComponent>().ClearSquad();
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new SquadMemberLeft { Squad = this, Player = player, IsForciblyDisbanded = true });
        }
        _members.Clear();
    }
    public bool ConstainsPlayer(WarfarePlayer player) => Members.Contains(player);
    public bool IsLeader(WarfarePlayer player) => Members.IndexOf(player) == 0;
    public static readonly SpecialFormat FormatColorName = new SpecialFormat("Colored Squad Name", "c");

    public static readonly SpecialFormat FormatName = new SpecialFormat("Squad Name", "n");

    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return FormatColorName.Match(in parameters)
            ? formatter.Colorize(Name, Team.Faction.Color, parameters.Options)
            : Name;
    }
}
