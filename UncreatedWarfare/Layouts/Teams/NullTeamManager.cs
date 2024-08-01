using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Team manager with no teams.
/// </summary>
public class NullTeamManager : ITeamManager<Team>
{
    /// <inheritdoc />
    public IReadOnlyList<Team> AllTeams { get; } = Array.Empty<Team>();

    /// <inheritdoc />
    public Team? FindTeam(string teamSearch) => null;
    
    /// <inheritdoc />
    public UniTask InitializeAsync(CancellationToken token = default) => UniTask.CompletedTask;
}
