using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Zones;

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
    /// Array of all active factions without duplicates, used for kit queries.
    /// </summary>
    IReadOnlyList<uint> Factions { get; }

    /// <summary>
    /// Find a team from a string value, such as from config.
    /// </summary>
    /// <param name="teamSearch">Can be (in order of priority) A faction name, a team ID, 'blufor', or 'opfor'.</param>
    TTeam? FindTeam([NotNullWhen(true)] string? teamSearch);

    /// <summary>
    /// Will return the default team if <paramref name="groupId"/> doesn't correspond to a team, otherwise the "no team" default.
    /// </summary>
    TTeam GetTeam(CSteamID groupId);

    /// <summary>
    /// Gets the role of the team in the current layout.
    /// </summary>
    LayoutRole GetLayoutRole(Team team) => LayoutRole.NotApplicable;

    /// <summary>
    /// Used to initialize team info from the layout and map configuration.
    /// </summary>
    UniTask InitializeAsync(IServiceProvider serviceProvider, CancellationToken token = default);

    /// <summary>
    /// Activates the team manager. This happens just before the first phase is activated.
    /// </summary>
    UniTask BeginAsync(CancellationToken token = default);

    /// <summary>
    /// Deactivates the team manager.
    /// </summary>
    UniTask EndAsync(CancellationToken token = default);

    /// <summary>
    /// The group admins can join when placing buildables or doing other duties where they shouldn't be in a group.
    /// </summary>
    CSteamID AdminGroupId { get; }

    /// <summary>
    /// Team manager extra configuration from config file.
    /// </summary>
    IConfiguration? Configuration { get; internal set; }

    /// <summary>
    /// Joins a player into a team if they're not already.
    /// </summary>
    UniTask JoinTeamAsync(WarfarePlayer player, Team team, bool wasByAdminCommand, CancellationToken token = default);

    /// <summary>
    /// Gets the desired spawn point when a player needs to respawn at main base.
    /// </summary>
    /// <remarks>The player may not have joined the server yet.</remarks>
    /// <returns>The spawn position as a <see cref="Vector4"/>, where <see cref="Vector4.w"/> is yaw rotation.</returns>
    Vector4? GetSpawnPointWhenRespawningAtMain(IPlayer player, Team team, ZoneStore globalZoneStore);
}