using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Defines team-specific behavior for a phase.
/// </summary>
public class PhaseTeamSettings
{
    /// <summary>
    /// Info about <see cref="Team"/> that will be fetched at initialization time.
    /// </summary>
    [UsedImplicitly]
    public Team? TeamInfo { get; internal set; }

    /// <summary>
    /// Can be (in order of priority) A faction name, a team ID, 'attack', or 'defense'.
    /// </summary>
    [UsedImplicitly]
    public string? Team { get; set; }

    /// <summary>
    /// If the team can't leave their base during the phase.
    /// </summary>
    [UsedImplicitly]
    public bool Grounded { get; set; }

    /// <summary>
    /// If the team can't take damage.
    /// </summary>
    [UsedImplicitly]
    public bool Invincible { get; set; }

    /// <summary>
    /// Display name on the toast specifically for this team, if any.
    /// </summary>
    [UsedImplicitly]
    public TranslationList? Name { get; set; }

    /// <summary>
    /// Extra configuration about the team, assign at initialization time.
    /// </summary>
    public IConfiguration? Configuration { get; internal set; }
}