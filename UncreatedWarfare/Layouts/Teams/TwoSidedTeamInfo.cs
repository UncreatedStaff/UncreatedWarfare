using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Teams;

/// <summary>
/// Configuration class for team info.
/// </summary>
public class TwoSidedTeamInfo
{
    /// <summary>
    /// Role of the team, such as attacker, defender, etc.
    /// </summary>
    public TwoSidedTeamRole Role { get; set; }

    /// <summary>
    /// Faction to use for the team.
    /// </summary>
    /// <remarks>If it's 'Map 1' or 'Map 2', the value will be taken from the map's config info. Otherwise it should equal the internal faction id, such as 'usa', etc.</remarks>
    public string? Faction { get; set; }
}

/// <summary>
/// The role of teams in a <see cref="TwoSidedTeamManager"/>.
/// </summary>
[Translatable("Two-Sided Team Role", Description = "The role of teams in a 2-sided gamemode.")]
public enum TwoSidedTeamRole
{
    /// <summary>
    /// No attacker or defender roles. Both teams must have this.
    /// </summary>
    [TranslatableValue(Description = "Not applicable, meaning there is no attack or defense in this gamemode.")]
    None,

    /// <summary>
    /// Randomly pick this role at game start.
    /// </summary>
    [TranslatableValue(IsPrioritizedTranslation = false)]
    Random,

    /// <summary>
    /// This team represents the 'good guys' who are probably NATO affiliated and likely to be spreading their Western influence forcibly.
    /// </summary>
    [TranslatableValue("Defense")]
    Blufor,

    /// <summary>
    /// This team represents the 'bad guys' who are probably Eastern and likely to be defending something that's against Western interests.
    /// </summary>
    [TranslatableValue("Attack")]
    Opfor
}