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
    public string Faction { get; set; }
}

public enum TwoSidedTeamRole
{
    /// <summary>
    /// No attacker or defender roles. Both teams must have this.
    /// </summary>
    None,

    /// <summary>
    /// Randomly pick this role at game start.
    /// </summary>
    Random,
    
    /// <summary>
    /// This team will always be the attacker.
    /// </summary>
    Attacker,
    
    /// <summary>
    /// This team will always be the defender.
    /// </summary>
    Defender
}