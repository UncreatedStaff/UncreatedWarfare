using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;
public class LeaderboardPlayer
{
    public WarfarePlayer Player { get; }
    public Team Team { get; }
    public float LastJoinedTeam { get; set; }
    public double[] Stats { get; set; }
    public PlayerGameStatsComponent Component { get; set; }
    public LeaderboardPlayer(WarfarePlayer player, Team team)
    {
        Player = player;
        Team = team;
        LastJoinedTeam = Time.realtimeSinceStartup;
        Component = player.Component<PlayerGameStatsComponent>();
        Stats = Component.Stats;
    }
}
