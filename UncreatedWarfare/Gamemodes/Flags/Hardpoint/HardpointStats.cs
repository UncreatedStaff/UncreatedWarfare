using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
public class HardpointLeaderboard : ConventionalLeaderboard<HardpointPlayerStats, HardpointTracker>
{
    public override void Calculate()
    {
        tracker.GetTopStats(14, out statsT1, out statsT2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        LeaderboardUI.SendHardpointLeaderboard(set, in tracker._longestShot, statsT1, statsT2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}
public class HardpointTracker : TeamStatTracker<HardpointPlayerStats>, ILongestShotTracker, IFobsTracker
{
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    internal LongestShot _longestShot = LongestShot.Nil;
    public int FOBsPlacedT1 { get => fobsPlacedT1; set => fobsPlacedT1 = value; }
    public int FOBsPlacedT2 { get => fobsPlacedT2; set => fobsPlacedT2 = value; }
    public int FOBsDestroyedT1 { get => fobsDestroyedT1; set => fobsDestroyedT1 = value; }
    public int FOBsDestroyedT2 { get => fobsDestroyedT2; set => fobsDestroyedT2 = value; }
    public LongestShot LongestShot { get => _longestShot; set => _longestShot = value; }
    public override void Reset()
    {
        base.Reset();
        fobsPlacedT1 = 0;
        fobsPlacedT2 = 0;
        fobsDestroyedT1 = 0;
        fobsDestroyedT2 = 0;
        _longestShot = LongestShot.Nil;
    }
    public virtual void GetTopStats(int count, out List<HardpointPlayerStats> statsT1, out List<HardpointPlayerStats> statsT2)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<HardpointPlayerStats> stats = this.stats.ToList();

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
        HardpointPlayerStats totalT1 = new HardpointPlayerStats(0UL);
        HardpointPlayerStats totalT2 = new HardpointPlayerStats(0UL);
        statsT1 = new List<HardpointPlayerStats>(stats.Count) { totalT1 };
        statsT2 = new List<HardpointPlayerStats>(stats.Count) { totalT2 };
        stats.Sort((HardpointPlayerStats a, HardpointPlayerStats b) => b.XPGained.CompareTo(a.XPGained));
        for (int i = 0; i < stats.Count; ++i)
        {
            HardpointPlayerStats stat = stats[i];

            ulong team = stat.Player.GetTeam();
            if (team == 1)
            {
                totalT1.kills += stat.kills;
                totalT1.deaths += stat.deaths;
                totalT1.AddXP(stat.XPGained);
                totalT1.AddCredits(stat.Credits);
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
                totalT2.AddDamage(stat.DamageDone);
                if (statsT2.Count <= count)
                    statsT2.Add(stat);
            }
        }
    }
}

public class HardpointPlayerStats : TeamPlayerStats, IExperienceStats, IFOBStats, IRevivesStats
{
    protected int _xp;
    protected int _credits;
    protected int _fobsDestroyed;
    protected int _fobsPlaced;
    protected int _revives;
    internal int _killsAttack;
    internal int _killsDefense;
    public int XPGained => _xp;
    public int Credits => _credits;
    public int FOBsDestroyed => _fobsDestroyed;
    public int FOBsPlaced => _fobsPlaced;
    public int Revives => _revives;
    public int KillsAttack => _killsAttack;
    public int KillsDefense => _killsDefense;
    public HardpointPlayerStats(UCPlayer player) : base(player) { }
    public HardpointPlayerStats(ulong player) : base(player) { }
    public void AddFOBDestroyed() => _fobsDestroyed++;
    public void AddFOBPlaced() => _fobsPlaced++;
    public void AddCredits(int amount) => _credits += amount;
    public void AddXP(int amount) => _xp += amount;
    public void AddRevive() => _revives++;
    public override void Update(float dt)
    {
        if (_player is null || !_player.IsOnline || Data.Gamemode is not IFlagObjectiveGamemode gm) return;
        Flag? obj = gm.Objective;
        if (obj != null && obj.ZoneData.IsInside(_player.Position))
        {
            timeonpoint += dt;
            timedeployed += dt;
        }
        else if (!_player.Player.IsInMain())
            timedeployed += dt;
    }
    public override void Reset()
    {
        base.Reset();
        _xp = 0;
        _credits = 0;
        _fobsDestroyed = 0;
        _fobsPlaced = 0;
        _revives = 0;
        _killsAttack = 0;
        _killsDefense = 0;
    }
}