using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Events.Models.Players;

public class PlayerTeamChanged : PlayerEvent
{
    public required CSteamID GroupId { get; init; }
    public required Team Team { get; init; }
    public required Team OldTeam { get; init; }
    public bool DidLeave => Team.IsValid;
}