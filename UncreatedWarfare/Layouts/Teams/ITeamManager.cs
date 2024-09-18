using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Generic interface used to manage an abstract number of teams.
/// </summary>
public interface ITeamManager<out TTeam> where TTeam : Team
{
    /// <summary>
    /// List of all teams currently active.
    /// </summary>
    IReadOnlyList<TTeam> AllTeams { get; }

    /// <summary>
    /// Find a team from a string value, such as from config.
    /// </summary>
    /// <param name="teamSearch">Can be (in order of priority) A faction name, a team ID, 'blufor', or 'opfor'.</param>
    TTeam? FindTeam(string teamSearch);

    /// <summary>
    /// Will return the default team if <paramref name="groupId"/> doesn't correspond to a team.
    /// </summary>
    TTeam GetTeam(CSteamID groupId);

    /// <summary>
    /// Used to initialize team info from the layout and map configuration.
    /// </summary>
    UniTask InitializeAsync(CancellationToken token = default);

    /// <summary>
    /// The group admins can join when placing buildables or doing other duties where they shouldn't be in a group.
    /// </summary>
    CSteamID AdminGroupId { get; }

    /// <summary>
    /// Team manager extra configuration from config file.
    /// </summary>
    IConfigurationSection Configuration { get; internal set; }
}