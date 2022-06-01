using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags;

public class BaseCTFLeaderboard<Stats, StatTracker> : ConventionalLeaderboard<Stats, StatTracker> where Stats : BaseCTFStats where StatTracker : BaseCTFTracker<Stats>
{
    public override void Calculate()
    {
        tracker.GetTopStats(14, out statsT1, out statsT2);
    }
    public override void SendLeaderboard(LanguageSet set)
    {
        LeaderboardUI.SendCTFLeaderboard(set, ref tracker.LongestShot, statsT1, statsT2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}

public class BaseCTFStats : TeamPlayerStats, IExperienceStats, IFlagStats, IFOBStats, IRevivesStats
{
    public BaseCTFStats(Player player) : base(player) { }
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
        List<T> stats = this.stats.Values.ToList();

        stats.RemoveAll(p =>
        {
            if (p == null) return true;
            if (p.Player == null)
            {
                SteamPlayer player = PlayerTool.getSteamPlayer(p.Steam64);
                if (player == default || player.player == default) return true;
                else p.Player = player.player;
                return false;
            }
            else return false;
        });

        T totalT1 = BasePlayerStats.New<T>(0UL);
        T totalT2 = BasePlayerStats.New<T>(0UL);
        IEnumerator<T> enumerator = stats.GetEnumerator();
        while (enumerator.MoveNext())
        {
            T stat = enumerator.Current;

            if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 1)
            {
                totalT1.kills += stat.kills;
                totalT1.deaths += stat.deaths;
                totalT1.AddXP(stat.XPGained);
                totalT1.AddCredits(stat.Credits);
                totalT1.AddCaptures(stat.Captures);
                totalT1.AddDamage(stat.DamageDone);
            }
            else if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 2)
            {
                totalT2.kills += stat.kills;
                totalT2.deaths += stat.deaths;
                totalT2.AddXP(stat.XPGained);
                totalT2.AddCredits(stat.Credits);
                totalT2.AddCaptures(stat.Captures);
                totalT2.AddDamage(stat.DamageDone);
            }
        }
        enumerator.Dispose();

        stats.Sort((T a, T b) => b.XPGained.CompareTo(a.XPGained));

        statsT1 = stats.Where(p => p.Player.GetTeam() == 1).ToList();
        statsT2 = stats.Where(p => p.Player.GetTeam() == 2).ToList();
        statsT1.Take(count);
        statsT2.Take(count);
        statsT1.Insert(0, totalT1);
        statsT2.Insert(0, totalT2);
    }
}

public class TeamCTFTracker : BaseCTFTracker<BaseCTFStats>
{

}
public class InvasionTracker : BaseCTFTracker<BaseCTFStats>
{

}
