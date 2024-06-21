using Steamworks;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Gamemodes.Teams;

/// <summary>
/// Represents a team or 'side' in a round.
/// </summary>
public class Team
{
    /// <summary>
    /// Unique ID for the team.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Information about the faction for this team.
    /// </summary>
    public required FactionInfo Faction { get; init; }

    /// <summary>
    /// Id of the in-game group used for this team.
    /// </summary>
    public CSteamID GroupId { get; init; }
}