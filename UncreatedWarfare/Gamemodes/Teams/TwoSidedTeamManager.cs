using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Gamemodes.Teams;

/// <summary>
/// Supports exactly 2 teams, with an optional 'attacker' and 'defender'.
/// </summary>
public class TwoSidedTeamManager : ITeamManager<Team>
{
    private readonly ILogger<TwoSidedTeamManager> _logger;
    private readonly Team[] _teams = new Team[2];
    private int _attacker;
    private int _defender;

    /// <inheritdoc />
    public IReadOnlyList<Team> AllTeams { get; }

    /// <summary>
    /// Info about all teams, binded from configuration.
    /// </summary>
    public TwoSidedTeamInfo[] Teams { get; set; }

    /// <summary>
    /// If there are attacking and defending teams specified.
    /// </summary>
    public bool HasAttackDefense { get; private set; }

    /// <summary>
    /// The attacking team.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no attacking team.</exception>
    public Team Attacker => _attacker == -1 ? throw new InvalidOperationException("This layout does not have an attacker.") : _teams[_attacker];

    /// <summary>
    /// The defending team.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no defending team.</exception>
    public Team Defender => _defender == -1 ? throw new InvalidOperationException("This layout does not have an attacker.") : _teams[_defender];

    public TwoSidedTeamManager(ILogger<TwoSidedTeamManager> logger)
    {
        _logger = logger;
        AllTeams = new ReadOnlyCollection<Team>(_teams);
    }

    /// <inheritdoc />
    public async UniTask InitializeAsync(CancellationToken token = default)
    {
        if (Teams is not { Length: 2 })
            throw new LayoutConfigurationException(this, "Expected exactly 2 team info's in the 'Teams' section.");

        FactionInfo? factionInfo1, factionInfo2;
        await using (IGameDataDbContext dbContext = new WarfareDbContext())
        {
            Faction f1 = await TeamUtility.ResolveTeamFactionHint(Teams[0].Faction, dbContext, this, token);
            Faction f2 = await TeamUtility.ResolveTeamFactionHint(Teams[1].Faction, dbContext, this, token);

            // todo this will change once TeamManager is replaced
            factionInfo1 = TeamManager.GetFactionInfo(f1);
            factionInfo2 = TeamManager.GetFactionInfo(f2);
        }

        _teams[0] = new Team
        {
            Faction = factionInfo1 ?? throw new LayoutConfigurationException($"Unknown faction: {Teams[0].Faction}."),
            Id = 1,
            GroupId = new CSteamID(1)
        };
        
        _teams[1] = new Team
        {
            Faction = factionInfo2 ?? throw new LayoutConfigurationException($"Unknown faction: {Teams[1].Faction}."),
            Id = 2,
            GroupId = new CSteamID(2)
        };

        DecideTeams(out TwoSidedTeamRole team1Role, out TwoSidedTeamRole team2Role);

        if (team1Role == TwoSidedTeamRole.Attacker)
            _attacker = 0;
        else if (team1Role == TwoSidedTeamRole.Defender)
            _defender = 0;

        if (team2Role == TwoSidedTeamRole.Attacker)
            _attacker = 1;
        else if (team2Role == TwoSidedTeamRole.Defender)
            _defender = 1;

        _logger.LogInformation("Teams: {0} (Role: {1}) vs {2} (Role: {3})", _teams[0].Faction.Name, team1Role, _teams[1].Faction.Name, team2Role);
    }

    private void DecideTeams(out TwoSidedTeamRole team1Role, out TwoSidedTeamRole team2Role)
    {
        TwoSidedTeamRole role1 = Teams[0].Role;
        TwoSidedTeamRole role2 = Teams[1].Role;

        if ((role1 == TwoSidedTeamRole.None) != (role2 == TwoSidedTeamRole.None))
        {
            throw new LayoutConfigurationException(this, "One team can't attack while the other has no role.");
        }

        if (role1 == TwoSidedTeamRole.None || role2 == TwoSidedTeamRole.None)
        {
            team1Role = TwoSidedTeamRole.None;
            team2Role = TwoSidedTeamRole.None;
            Teams[0].Role = team1Role;
            Teams[1].Role = team2Role;
            HasAttackDefense = false;
            return;
        }

        if (role1 == TwoSidedTeamRole.Random && role2 == TwoSidedTeamRole.Random)
        {
            bool team1Attacks = RandomUtility.GetBoolean();

            team1Role = team1Attacks ? TwoSidedTeamRole.Attacker : TwoSidedTeamRole.Defender;
            team2Role = team1Attacks ? TwoSidedTeamRole.Defender : TwoSidedTeamRole.Attacker;
            Teams[0].Role = team1Role;
            Teams[1].Role = team2Role;
            HasAttackDefense = true;
            return;
        }

        if (role1 == TwoSidedTeamRole.Random && role2 is TwoSidedTeamRole.Attacker or TwoSidedTeamRole.Defender)
        {
            role1 = role2 == TwoSidedTeamRole.Attacker ? TwoSidedTeamRole.Defender : TwoSidedTeamRole.Attacker;
        }
        else if (role2 == TwoSidedTeamRole.Random && role1 is TwoSidedTeamRole.Attacker or TwoSidedTeamRole.Defender)
        {
            role2 = role1 == TwoSidedTeamRole.Attacker ? TwoSidedTeamRole.Defender : TwoSidedTeamRole.Attacker;
        }
        else if (role1 is not TwoSidedTeamRole.Attacker and not TwoSidedTeamRole.Defender || role2 is not TwoSidedTeamRole.Attacker and not TwoSidedTeamRole.Defender || role1 == role2)
        {
            throw new LayoutConfigurationException(this, "Invalid role configuration for team 1 or 2.");
        }

        team1Role = role1;
        team2Role = role2;
        Teams[0].Role = role1;
        Teams[1].Role = role2;
        HasAttackDefense = true;
    }
}
