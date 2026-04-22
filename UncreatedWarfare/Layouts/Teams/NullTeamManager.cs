using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Team manager with no teams.
/// </summary>
public class NullTeamManager : ITeamManager<Team>
{
    /// <inheritdoc />
    public IReadOnlyList<Team> AllTeams { get; } = Array.Empty<Team>();

    /// <inheritdoc />
    public IReadOnlyList<uint> Factions => Array.Empty<uint>();

    /// <inheritdoc />
    public Team? FindTeam(string? teamSearch) => null;

    /// <inheritdoc />
    public Team GetTeam(CSteamID groupId) => Team.NoTeam;

    /// <inheritdoc />
    public UniTask InitializeAsync(IServiceProvider serviceProvider, CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask BeginAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask EndAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public CSteamID AdminGroupId => CSteamID.Nil;

    /// <inheritdoc />
    public IConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public UniTask JoinTeamAsync(WarfarePlayer player, Team team, bool wasByAdminCommand, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Vector4? GetSpawnPointWhenRespawningAtMain(IPlayer player, Team team, ZoneStore globalZoneStore)
    {
        return null;
    }
}
