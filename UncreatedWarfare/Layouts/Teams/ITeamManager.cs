using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

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
    /// <param name="teamSearch">Can be (in order of priority) A faction name, a team ID, 'attack', or 'defense'.</param>
    TTeam? FindTeam(string teamSearch);

    /// <summary>
    /// Used to initialize team info from the layout and map configuration.
    /// </summary>
    UniTask InitializeAsync(CancellationToken token = default);
}