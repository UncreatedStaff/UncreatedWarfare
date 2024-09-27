using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Players;

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
    public Team GetTeam(CSteamID groupId) => Team.NoTeam;

    /// <inheritdoc />
    public UniTask InitializeAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public CSteamID AdminGroupId => CSteamID.Nil;

    /// <inheritdoc />
    public IConfiguration Configuration { get; set; }

    /// <inheritdoc />
    public UniTask JoinTeamAsync(WarfarePlayer player, Team team, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}
