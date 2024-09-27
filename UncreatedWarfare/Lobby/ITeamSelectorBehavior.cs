using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Lobby;
public interface ITeamSelectorBehavior
{
    /// <summary>
    /// Array of all available teams to be updated as team counts change.
    /// </summary>
    TeamInfo[] Teams { get; set; }

    /// <summary>
    /// If a player can join a team, optionally specifying the team they're currently on.
    /// </summary>
    bool CanJoinTeam(int index, int currentTeam = -1);

    /// <summary>
    /// Trigger a full update for all player counts.
    /// </summary>
    void UpdateTeams();
}

public struct TeamInfo
{
    public int PlayerCount { get; set; }
    public Team Team { get; internal set; }
}