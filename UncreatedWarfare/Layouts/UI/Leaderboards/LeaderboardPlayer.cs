using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;
public class LeaderboardPlayer
{
    public WarfarePlayer Player { get; }
    public Team Team { get; }
    public LeaderboardPlayer(WarfarePlayer player, Team team)
    {
        Player = player;
        Team = team;
    }
}
