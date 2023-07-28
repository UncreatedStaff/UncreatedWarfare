using System;
using Uncreated.Warfare.Gamemodes.UI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public class ConquestLeaderboard : ConventionalLeaderboard<ConquestStats, ConquestStatTracker>
{
    public ConquestLeaderboard()
    {
        LeaderboardOverrides = new StatValue[]
        {
          new StatValue(T.ConquestHeader0, (_, b, l) => b!.Kills.ToString(l)),
          new StatValue(T.ConquestHeader1, (_, b, l) => b!.Deaths.ToString(l)),
          new StatValue(T.ConquestHeader2, (_, b, l) => b!.XPGained.ToString(l)),
          new StatValue(T.ConquestHeader3, (_, b, l) => b!.Captures.ToString(l)),
          new StatValue(T.ConquestHeader4, (_, b, l) => b!.VehicleKills.ToString(l)),
          new StatValue(T.ConquestHeader5, (_, b, l) => b!.AircraftKills.ToString(l))
        };
        PlayerStatOverrides = new StatValue[]
        {
          new StatValue(T.ConquestPlayerStats0, (_, b, l) => b!.Kills.ToString(l)),
          new StatValue(T.ConquestPlayerStats1, (_, b, l) => b!.Deaths.ToString(l)),
          new StatValue(T.ConquestPlayerStats2, (_, b, l) => b!.DamageDone.ToString(l)),
          new StatValue(T.ConquestPlayerStats3, (_, b, l) => b!.KillsOnPoint.ToString(l)),
          new StatValue(T.ConquestPlayerStats4, (_, b, l) => TimeSpan.FromSeconds(b!.timedeployed).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.ConquestPlayerStats5, (_, b, l) => b!.XPGained.ToString(l)),
          new StatValue(T.ConquestPlayerStats6, (_, b, l) => b!.Revives.ToString(l)),
          new StatValue(T.ConquestPlayerStats7, (_, b, l) => b!.Captures.ToString(l)),
          new StatValue(T.ConquestPlayerStats8, (_, b, l) => TimeSpan.FromSeconds(b!.timeonpoint).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.ConquestPlayerStats9, (_, b, l) => b!.Teamkills.ToString(l)),
          new StatValue(T.ConquestPlayerStats10, (_, b, l) => b!.FOBsDestroyed.ToString(l)),
          new StatValue(T.ConquestPlayerStats11, (_, b, l) => b!.Credits.ToString(l))
        };
        WarStatOverrides = new StatValue[]
        {
          new StatValue(T.ConquestWarStats0, (a, _, l) => a!.Duration.ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.ConquestWarStats1, (a, _, l) => a!.casualtiesT1.ToString(l), 1ul),
          new StatValue(T.ConquestWarStats2, (a, _, l) => a!.casualtiesT2.ToString(l), 2ul),
          new StatValue(T.ConquestWarStats3, (a, _, l) => TimeSpan.FromSeconds(a!.timecontested).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
          new StatValue(T.ConquestWarStats4, (a, _, l) => a!.AverageTeam1Size.ToString(ConventionalLeaderboardUI.StatFormatFloat, l), 1ul),
          new StatValue(T.ConquestWarStats5, (a, _, l) => a!.AverageTeam2Size.ToString(ConventionalLeaderboardUI.StatFormatFloat,l), 2ul),
          new StatValue(T.ConquestWarStats6, (a, _, l) => a!.fobsPlacedT1.ToString(l), 1ul),
          new StatValue(T.ConquestWarStats7, (a, _, l) => a!.fobsPlacedT2.ToString(l), 2ul),
          new StatValue(T.ConquestWarStats8, (a, _, l) => a!.fobsDestroyedT1.ToString(l), 1ul),
          new StatValue(T.ConquestWarStats9, (a, _, l) => a!.fobsDestroyedT2.ToString(l), 2ul),
          new StatValue(T.ConquestWarStats10, (a, _, l) => (a!.teamkillsT1 + a.teamkillsT2).ToString(l))
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
