using Uncreated.Warfare.Gamemodes.Teams;

namespace Uncreated.Warfare.Gamemodes.Phases;

/// <summary>
/// Defines team-specific behavior for a phase.
/// </summary>
public class PhaseTeamSettings
{
    /// <summary>
    /// Info about <see cref="Team"/> that will be fetched at initialization time.
    /// </summary>
    public Team? TeamInfo { get; internal set; }

    /// <summary>
    /// Can be (in order of priority) A faction name, a team ID, 'attack', or 'defense'.
    /// </summary>
    public string Team { get; set; }

    /// <summary>
    /// If the team can't leave their base during the phase.
    /// </summary>
    public bool Grounded { get; set; }

    /// <summary>
    /// Display name on the toast, if any.
    /// </summary>
    public TranslationList? Name { get; set; }
}