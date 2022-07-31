using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Flags;
public class ConquestLeaderboard : ConventionalLeaderboard<ConquestStats, ConquestStatTracker>
{
    public override void Calculate()
    {
        tracker.GetTopStats(14, out statsT1, out statsT2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        LeaderboardUI.SendConquestLeaderboard(set, tracker.LongestShot, statsT1, statsT2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}
