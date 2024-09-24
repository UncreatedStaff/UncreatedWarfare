using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Lobby;
public interface ITeamSelectorBehavior
{
    /// <summary>
    /// Array of all available teams to be updated as team counts change.
    /// </summary>
    TeamInfo[] Teams { get; }

    /// <summary>
    /// If a player can join a team, optionally specifying the team they're currently on.
    /// </summary>
    bool CanJoinTeam(int index, int currentTeam = -1);
}

public struct TeamInfo
{
    public int PlayerCount { get; set; }
    public FactionInfo Faction { get; }
    public TeamInfo(FactionInfo faction)
    {
        Faction = faction;
    }
}