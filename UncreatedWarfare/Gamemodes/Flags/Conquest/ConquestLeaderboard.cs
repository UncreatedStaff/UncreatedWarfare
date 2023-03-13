namespace Uncreated.Warfare.Gamemodes.Flags;
public class ConquestLeaderboard : ConventionalLeaderboard<ConquestStats, ConquestStatTracker>
{
    public override void Calculate()
    {
        Tracker.GetTopStats(14, out StatsTeam1, out StatsTeam2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        LeaderboardUI.SendConquestLeaderboard(set, Tracker.LongestShot, StatsTeam1, StatsTeam2, Tracker, ShuttingDown ? ShuttingDownMessage : null, Winner);
    }
}
