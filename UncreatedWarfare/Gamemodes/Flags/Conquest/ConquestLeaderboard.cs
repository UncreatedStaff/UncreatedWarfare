using System;
using Uncreated.Warfare.Gamemodes.UI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public class ConquestLeaderboard : ConventionalLeaderboard<ConquestStats, ConquestStatTracker>
{
    public ConquestLeaderboard()
    {
        LeaderboardOverrides = new StatValue[]
        {
          new StatValue(T.InsurgencyHeader0, (_, b, l) => b!.Kills.ToString(l)),
          new StatValue(T.InsurgencyHeader1, (_, b, l) => b!.Deaths.ToString(l)),
          new StatValue(T.InsurgencyHeader2, (_, b, l) => b!.XPGained.ToString(l)),
          new StatValue(T.InsurgencyHeader3, (_, b, l) => b!.Credits.ToString(l)),
          new StatValue(T.InsurgencyHeader4, (_, b, l) => b!.Deaths == 0 ? b.Kills.ToString(l) : (b.Kills / (float)b.Deaths).ToString(ConventionalLeaderboardUI.StatFormatPrecisionFloat, l)),
          new StatValue(T.InsurgencyHeader5, (_, b, l) => b!.DamageDone.ToString(l))
        };
        PlayerStatOverrides = new StatValue[]
        {
          new StatValue(T.InsurgencyPlayerStats0, (_, b, l) => b!.Kills.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats1, (_, b, l) => b!.Deaths.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats2, (_, b, l) => b!.DamageDone.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats3, (_, b, l) => b!.KillsOnPoint.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats4, (_, b, l) => TimeSpan.FromSeconds(b!.timedeployed).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.InsurgencyPlayerStats5, (_, b, l) => b!.XPGained.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats6, (_, b, l) => b!.Revives.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats7, (_, b, l) => b!.Captures.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats8, (_, b, l) => TimeSpan.FromSeconds(b!.timeonpoint).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.InsurgencyPlayerStats9, (_, b, l) => b!.Teamkills.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats10, (_, b, l) => b!.FOBsDestroyed.ToString(l)),
          new StatValue(T.InsurgencyPlayerStats11, (_, b, l) => b!.Credits.ToString(l))
        };
        WarStatOverrides = new StatValue[]
        {
          new StatValue(T.InsurgencyWarStats0, (a, _, l) => a!.Duration.ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.InsurgencyWarStats1, (a, _, l) => a!.casualtiesT1.ToString(l), 1ul),
          new StatValue(T.InsurgencyWarStats2, (a, _, l) => a!.casualtiesT2.ToString(l), 2ul),
          new StatValue(T.InsurgencyWarStats3, (a, _, l) => TimeSpan.FromSeconds(a!.timecontested).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.InsurgencyWarStats4, (a, _, l) => a!.AverageTeam1Size.ToString(ConventionalLeaderboardUI.StatFormatFloat, l), 1ul),
          new StatValue(T.InsurgencyWarStats5, (a, _, l) => a!.AverageTeam2Size.ToString(ConventionalLeaderboardUI.StatFormatFloat,l), 2ul),
          new StatValue(T.InsurgencyWarStats6, (a, _, l) => a!.fobsPlacedT1.ToString(l), 1ul),
          new StatValue(T.InsurgencyWarStats7, (a, _, l) => a!.fobsPlacedT2.ToString(l), 2ul),
          new StatValue(T.InsurgencyWarStats8, (a, _, l) => a!.fobsDestroyedT1.ToString(l), 1ul),
          new StatValue(T.InsurgencyWarStats9, (a, _, l) => a!.fobsDestroyedT2.ToString(l), 2ul),
          new StatValue(T.InsurgencyWarStats10, (a, _, l) => (a!.teamkillsT1 + a.teamkillsT2).ToString(l))
        };
    }
    public override void Calculate()
    {
        Tracker.GetTopStats(14, out StatsTeam1, out StatsTeam2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        SendLeaderboardPreset(set);
    }
}
