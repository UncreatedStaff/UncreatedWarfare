using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags;

public class BaseCTFLeaderboard<Stats, StatTracker> : ConventionalLeaderboard<Stats, StatTracker> where Stats : BaseCTFStats where StatTracker : BaseCTFTracker<Stats>
{
    public override void Calculate()
    {
        tracker.GetTopStats(14, out StatsTeam1, out StatsTeam2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        LeaderboardUI.SendCTFLeaderboard(set, in tracker.LongestShot, StatsTeam1, StatsTeam2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}

public class BaseCTFStats : TeamPlayerStats, IExperienceStats, IFlagStats, IFOBStats, IRevivesStats
{
    public BaseCTFStats(UCPlayer player) : base(player) { }
    public BaseCTFStats(ulong player) : base(player) { }

    protected int _xp;
    protected int _credits;
    protected int _caps;
    protected int _fobsDestroyed;
    protected int _fobsPlaced;
    protected int _revives;
    protected int _killsOnPoint;
    public int XPGained => _xp;
    public int Credits => _credits;
    public int Captures => _caps;
    public int FOBsDestroyed => _fobsDestroyed;
    public int FOBsPlaced => _fobsPlaced;
    public int Revives => _revives;
    public int KillsOnPoint => _killsOnPoint;
    public void AddCapture() => _caps++;
    public void AddCaptures(int amount) => _caps += amount;
    public void AddFOBDestroyed() => _fobsDestroyed++;
    public void AddFOBPlaced() => _fobsPlaced++;
    public void AddCredits(int amount) => _credits += amount;
    public void AddXP(int amount) => _xp += amount;
    public void AddRevive() => _revives++;
    public void AddKillOnPoint() => _killsOnPoint++;
    public override void Reset()
    {
        base.Reset();
        _xp = 0;
        _credits = 0;
        _caps = 0;
        _fobsDestroyed = 0;
        _fobsPlaced = 0;
        _revives = 0;
        _killsOnPoint = 0;
    }
}

public abstract class BaseCTFTracker<T> : TeamStatTracker<T>, ILongestShotTracker where T : BaseCTFStats
{
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    public int flagOwnerChanges;
    public LongestShot LongestShot = LongestShot.Nil;
    LongestShot ILongestShotTracker.LongestShot { get => LongestShot; set => LongestShot = value; }

    public override void Reset()
    {
        base.Reset();
        fobsPlacedT1 = 0;
        fobsPlacedT2 = 0;
        fobsDestroyedT1 = 0;
        fobsDestroyedT2 = 0;
        flagOwnerChanges = 0;
        LongestShot = LongestShot.Nil;
    }
    public virtual void GetTopStats(int count, out List<T> statsT1, out List<T> statsT2)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<T> stats = this.stats.ToList();

        stats.RemoveAll(p =>
        {
            if (p == null) return true;
            if (p.Player is null || !p.Player.IsOnline)
            {
                p.Player = UCPlayer.FromID(p._id)!;
                return p.Player is null || !p.Player.IsOnline;
            }
            else return false;
        });

        T totalT1 = BasePlayerStats.New<T>(0UL);
        T totalT2 = BasePlayerStats.New<T>(0UL);
        stats.Sort((T a, T b) => b.XPGained.CompareTo(a.XPGained));
        statsT1 = new List<T>(stats.Count) { totalT1 };
        statsT2 = new List<T>(stats.Count) { totalT2 };
        for (int i = 0; i < stats.Count; ++i)
        {
            T stat = stats[i];
            ulong team = stat.Player.GetTeam();
            if (team == 1)
            {
                totalT1.kills += stat.kills;
                totalT1.deaths += stat.deaths;
                totalT1.AddXP(stat.XPGained);
                totalT1.AddCredits(stat.Credits);
                totalT1.AddCaptures(stat.Captures);
                totalT1.AddDamage(stat.DamageDone);
                if (statsT1.Count <= count)
                    statsT1.Add(stat);
            }
            else if (team == 2)
            {
                totalT2.kills += stat.kills;
                totalT2.deaths += stat.deaths;
                totalT2.AddXP(stat.XPGained);
                totalT2.AddCredits(stat.Credits);
                totalT2.AddCaptures(stat.Captures);
                totalT2.AddDamage(stat.DamageDone);
                if (statsT2.Count <= count)
                    statsT2.Add(stat);
            }
        }
    }
}

public class TeamCTFTracker : BaseCTFTracker<BaseCTFStats>
{

}
public class InvasionTracker : BaseCTFTracker<BaseCTFStats>
{

}
