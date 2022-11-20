using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags;

public sealed class ConquestStatTracker : TeamStatTracker<ConquestStats>, ILongestShotTracker
{
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    public int flagOwnerChanges;
    public LongestShot LongestShot { get; set; } = LongestShot.Nil;
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
    public void GetTopStats(int count, out List<ConquestStats> statsT1, out List<ConquestStats> statsT2)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<ConquestStats> stats = this.stats.ToList();

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

        ConquestStats totalT1 = BasePlayerStats.New<ConquestStats>(0UL);
        ConquestStats totalT2 = BasePlayerStats.New<ConquestStats>(0UL);
        stats.Sort((ConquestStats a, ConquestStats b) => b.XPGained.CompareTo(a.XPGained));
        statsT1 = new List<ConquestStats>(stats.Count) { totalT1 };
        statsT2 = new List<ConquestStats>(stats.Count) { totalT2 };
        for (int i = 0; i < stats.Count; ++i)
        {
            ConquestStats stat = stats[i];
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

public sealed class ConquestStats : TeamPlayerStats, IExperienceStats, IFlagStats, IFOBStats, IRevivesStats
{
    private int _xpGained;
    private int _credsGained;
    private int _captures;
    private int _fobsDestroyed;
    private int _fobsPlaced;
    private int _revives;
    private int _killsOnPt;

    public ConquestStats(UCPlayer player) : base(player) { }
    public ConquestStats(ulong player) : base(player) { }

    public int XPGained => _xpGained;
    public int Credits => _credsGained;
    public int Captures => _captures;
    public int FOBsDestroyed => _fobsDestroyed;
    public int FOBsPlaced => _fobsPlaced;
    public int Revives => _revives;
    public int KillsOnPoint => _killsOnPt;
    public void AddCaptures(int amount)
    {
        _captures += amount;
    }
    public void AddCredits(int amount)
    {
        _credsGained += amount;
    }
    public void AddXP(int amount)
    {
        _xpGained += amount;
    }
    public void AddCapture()
    {
        ++_captures;
    }
    public void AddFOBDestroyed()
    {
        ++_fobsDestroyed;
    }
    public void AddKillOnPoint()
    {
        ++_killsOnPt;
    }
    public void AddFOBPlaced()
    {
        ++_fobsPlaced;
    }
    public void AddRevive()
    {
        ++_revives;
    }
    public override void Reset()
    {
        base.Reset();
        _xpGained = 0;
        _credsGained = 0;
        _captures = 0;
        _fobsDestroyed = 0;
        _fobsPlaced = 0;
        _revives = 0;
        _killsOnPt = 0;
    }
}