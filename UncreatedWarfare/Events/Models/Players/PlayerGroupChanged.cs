using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles when a player's group (therefore their team) changes for any reason.
/// </summary>
public class PlayerGroupChanged : PlayerEvent
{
    public required CSteamID OldGroupId { get; init; }
    public required CSteamID NewGroupId { get; init; }
    public required EPlayerGroupRank OldRank { get; init; }
    public required EPlayerGroupRank NewRank { get; init; }
    public required Team OldTeam { get; init; }
    public required Team NewTeam { get; init; }
}
