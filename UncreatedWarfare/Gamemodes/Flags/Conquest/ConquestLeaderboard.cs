namespace Uncreated.Warfare.Gamemodes.Flags;
public class ConquestLeaderboard : ConventionalLeaderboard<ConquestStats, ConquestStatTracker>
{
    public override void Calculate()
    {
        tracker.GetTopStats(14, out StatsTeam1, out StatsTeam2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        LeaderboardUI.SendConquestLeaderboard(set, tracker.LongestShot, StatsTeam1, StatsTeam2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}
