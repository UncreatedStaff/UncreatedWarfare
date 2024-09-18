using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Models.Factions;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Supports exactly 2 teams, with an optional 'attacker' and 'defender'.
/// </summary>
public class TwoSidedTeamManager : ITeamManager<Team>
{
    private readonly ILogger<TwoSidedTeamManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Team[] _teams = new Team[2];
    private int _blufor;
    private int _opfor;

    /// <inheritdoc />
    public IReadOnlyList<Team> AllTeams { get; }

    /// <inheritdoc />
    public CSteamID AdminGroupId { get; } = new CSteamID(3);

    /// <summary>
    /// Info about all teams, binded from configuration.
    /// </summary>
    public TwoSidedTeamInfo[] Teams { get; set; }

    /// <summary>
    /// Team manager extra configuration from config file.
    /// </summary>
    public IConfigurationSection Configuration { get; set; }

    /// <summary>
    /// If both Blufor and Opfor teams are specified.
    /// </summary>
    public bool HasBothTeams { get; private set; }

    /// <summary>
    /// The 'good guys' who are spreading their influence forcibly.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no Blufor team.</exception>
    public Team Blufor => _blufor == -1 ? throw new InvalidOperationException("This layout does not have an Blufor team.") : _teams[_blufor];

    /// <summary>
    /// The 'bad guys' who are defending something against the interests of the other team.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no Opfor team.</exception>
    public Team Opfor => _opfor == -1 ? throw new InvalidOperationException("This layout does not have an Opfor team.") : _teams[_opfor];

    public TwoSidedTeamManager(ILogger<TwoSidedTeamManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        AllTeams = new ReadOnlyCollection<Team>(_teams);
    }

    /// <inheritdoc />
    public Team GetTeam(CSteamID groupId)
    {
        return Array.Find(_teams, team => team.GroupId.m_SteamID == groupId.m_SteamID);
    }

    /// <inheritdoc />
    public async UniTask InitializeAsync(CancellationToken token = default)
    {
        if (Teams is not { Length: 2 })
            throw new LayoutConfigurationException(this, "Expected exactly 2 team infos in the 'Teams' section.");

        FactionInfo? factionInfo1, factionInfo2;
        using (IServiceScope scope = _serviceProvider.CreateScope())
        await using (IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>())
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
            GroupId = new CSteamID(1),
            Configuration = Configuration.GetSection("Teams:0")
        };
        
        _teams[1] = new Team
        {
            Faction = factionInfo2 ?? throw new LayoutConfigurationException($"Unknown faction: {Teams[1].Faction}."),
            Id = 2,
            GroupId = new CSteamID(2),
            Configuration = Configuration.GetSection("Teams:1")
        };

        DecideTeams(out TwoSidedTeamRole team1Role, out TwoSidedTeamRole team2Role);

        if (team1Role == TwoSidedTeamRole.Blufor)
            _blufor = 0;
        else if (team1Role == TwoSidedTeamRole.Opfor)
            _opfor = 0;

        if (team2Role == TwoSidedTeamRole.Blufor)
            _blufor = 1;
        else if (team2Role == TwoSidedTeamRole.Opfor)
            _opfor = 1;

        _logger.LogInformation("Teams: {0} (Role: {1}) vs {2} (Role: {3})", _teams[0].Faction.Name, team1Role, _teams[1].Faction.Name, team2Role);

        await UniTask.SwitchToMainThread(token);
        CreateInGameGroups();
    }

    /// <inheritdoc />
    public Team? FindTeam(string teamSearch)
    {
        if (teamSearch.Equals("blufor", StringComparison.InvariantCultureIgnoreCase))
        {
            return _blufor == -1 ? null : _teams[_blufor];
        }

        if (teamSearch.Equals("opfor", StringComparison.InvariantCultureIgnoreCase))
        {
            return _opfor == -1 ? null : _teams[_opfor];
        }

        if (int.TryParse(teamSearch, NumberStyles.Number, CultureInfo.InvariantCulture, out int teamId))
        {
            return teamId is not 1 and not 2 ? null : _teams[teamId];
        }

        int index = F.StringIndexOf(AllTeams, x => x.Faction.Name, teamSearch);
        if (index != -1)
        {
            return AllTeams[index];
        }

        index = F.StringIndexOf(AllTeams, x => x.Faction.Abbreviation, teamSearch);
        if (index != -1)
        {
            return AllTeams[index];
        }

        index = F.StringIndexOf(AllTeams, x => x.Faction.ShortName, teamSearch);

        return index != -1 ? AllTeams[index] : null;
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
            HasBothTeams = false;
            return;
        }

        if (role1 == TwoSidedTeamRole.Random && role2 == TwoSidedTeamRole.Random)
        {
            bool team1Attacks = RandomUtility.GetBoolean();

            team1Role = team1Attacks ? TwoSidedTeamRole.Blufor : TwoSidedTeamRole.Opfor;
            team2Role = team1Attacks ? TwoSidedTeamRole.Opfor : TwoSidedTeamRole.Blufor;
            Teams[0].Role = team1Role;
            Teams[1].Role = team2Role;
            HasBothTeams = true;
            return;
        }

        if (role1 == TwoSidedTeamRole.Random && role2 is TwoSidedTeamRole.Blufor or TwoSidedTeamRole.Opfor)
        {
            role1 = role2 == TwoSidedTeamRole.Blufor ? TwoSidedTeamRole.Opfor : TwoSidedTeamRole.Blufor;
        }
        else if (role2 == TwoSidedTeamRole.Random && role1 is TwoSidedTeamRole.Blufor or TwoSidedTeamRole.Opfor)
        {
            role2 = role1 == TwoSidedTeamRole.Blufor ? TwoSidedTeamRole.Opfor : TwoSidedTeamRole.Blufor;
        }
        else if (role1 is not TwoSidedTeamRole.Blufor and not TwoSidedTeamRole.Opfor || role2 is not TwoSidedTeamRole.Blufor and not TwoSidedTeamRole.Opfor || role1 == role2)
        {
            throw new LayoutConfigurationException(this, "Invalid role configuration for team 1 or 2.");
        }

        team1Role = role1;
        team2Role = role2;
        Teams[0].Role = role1;
        Teams[1].Role = role2;
        HasBothTeams = true;
    }

    private void CreateInGameGroups()
    {
        object? groupsFieldValue = typeof(GroupManager)
            .GetField("knownGroups", BindingFlags.Static | BindingFlags.NonPublic)?
            .GetValue(null);

        if (groupsFieldValue is not Dictionary<CSteamID, GroupInfo> knownGroups)
            return;

        // also kicks all players from all groups.
        foreach (CSteamID existingGroup in knownGroups.Keys.ToList())
        {
            GroupManager.deleteGroup(existingGroup);
        }

        GroupManager.addGroup(new CSteamID(1), AllTeams[0].Faction.Name);
        GroupManager.addGroup(new CSteamID(2), AllTeams[1].Faction.Name);
        GroupManager.addGroup(new CSteamID(3), "Admins");
    }
}
