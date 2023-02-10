using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
public class HardpointLeaderboard : ConventionalLeaderboard<HardpointPlayerStats, HardpointTracker>
{
    public HardpointLeaderboard()
    {
        LeaderboardOverrides = new StatValue[]
        {
            new StatValue(T.HardpointHeader0, (_, b, l) => b!.Kills.ToString(l)),
            new StatValue(T.HardpointHeader1, (_, b, l) => b!.Deaths.ToString(l)),
            new StatValue(T.HardpointHeader2, (_, b, l) => b!.XPGained.ToString(l)),
            new StatValue(T.HardpointHeader3, (_, b, l) => b!.Credits.ToString(l)),
            new StatValue(T.HardpointHeader4, (_, b, l) => b!.CaptureSeconds.ToString(l)),
            new StatValue(T.HardpointHeader5, (_, b, l) => b!.DamageDone.ToString(l))
        };
        PlayerStatOverrides = new StatValue[]
        {
            new StatValue(T.HardpointPlayerStats0, (_, b, l) => b!.Kills.ToString(l)),
            new StatValue(T.HardpointPlayerStats1, (_, b, l) => b!.Deaths.ToString(l)),
            new StatValue(T.HardpointPlayerStats2, (_, b, l) => b!.DamageDone.ToString(l)),
            new StatValue(T.HardpointPlayerStats3, (_, b, l) => b!.ObjectiveKills.ToString(l)),
            new StatValue(T.HardpointPlayerStats4, (_, b, l) => TimeSpan.FromSeconds(b!.timedeployed).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
            new StatValue(T.HardpointPlayerStats5, (_, b, l) => b!.XPGained.ToString(l)),
            new StatValue(T.HardpointPlayerStats6, (_, b, l) => b!.Revives.ToString(l)),
            new StatValue(T.HardpointPlayerStats7, (_, b, l) => b!.Hardpoints.ToString(l)),
            new StatValue(T.HardpointPlayerStats8, (_, b, l) => TimeSpan.FromSeconds(b!.CaptureSeconds).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
            new StatValue(T.HardpointPlayerStats9, (_, b, l) => b!.Teamkills.ToString(l)),
            new StatValue(T.HardpointPlayerStats10, (_, b, l) => b!.FOBsDestroyed.ToString(l)),
            new StatValue(T.HardpointPlayerStats11, (_, b, l) => b!.Credits.ToString(l))
        };
        WarStatOverrides = new StatValue[]
        {
            new StatValue(T.HardpointWarStats0, (a, _, l) => a!.Duration.ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
            new StatValue(T.HardpointWarStats1, (a, _, l) => a!.casualtiesT1.ToString(l), 1ul),
            new StatValue(T.HardpointWarStats2, (a, _, l) => a!.casualtiesT2.ToString(l), 2ul),
            new StatValue(T.HardpointWarStats3, (a, _, l) => TimeSpan.FromSeconds(a!.ContestingSeconds).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
            new StatValue(T.HardpointWarStats4, (a, _, l) => a!.AverageTeam1Size.ToString(ConventionalLeaderboardUI.StatFormatFloat, l), 1ul),
            new StatValue(T.HardpointWarStats5, (a, _, l) => a!.AverageTeam2Size.ToString(ConventionalLeaderboardUI.StatFormatFloat,l), 2ul),
            new StatValue(T.HardpointWarStats6, (a, _, l) => a!.FOBsPlacedT1.ToString(l), 1ul),
            new StatValue(T.HardpointWarStats7, (a, _, l) => a!.FOBsPlacedT2.ToString(l), 2ul),
            new StatValue(T.HardpointWarStats8, (a, _, l) => a!.FOBsDestroyedT1.ToString(l), 1ul),
            new StatValue(T.HardpointWarStats9, (a, _, l) => a!.FOBsDestroyedT2.ToString(l), 2ul),
            new StatValue(T.HardpointWarStats10, (a, _, l) => (a!.teamkillsT1 + a.teamkillsT2).ToString(l))
        };
    }
    public override void Calculate()
    {
        tracker.GetTopStats(14, out StatsTeam1, out StatsTeam2);
    }
    public override void SendLeaderboard(in LanguageSet set)
    {
        SendLeaderboardPreset(set);
        //LeaderboardUI.SendHardpointLeaderboard(set, in tracker._longestShot, StatsTeam1, StatsTeam2, tracker, shuttingDown ? shuttingDownMessage : null, _winner);
    }
}
public class HardpointTracker : TeamStatTracker<HardpointPlayerStats>, ILongestShotTracker, IFobsTracker
{
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    internal LongestShot _longestShot = LongestShot.Nil;
    internal float ContestingSeconds;
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

    [UsedImplicitly]
    protected override void Update()
    {
        if (Data.Is(out Hardpoint hp) && hp.ObjectiveState == 3)
            ContestingSeconds += Time.deltaTime;
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
            return false;
        });
        HardpointPlayerStats totalT1 = new HardpointPlayerStats(0UL);
        HardpointPlayerStats totalT2 = new HardpointPlayerStats(0UL);
        statsT1 = new List<HardpointPlayerStats>(stats.Count) { totalT1 };
        statsT2 = new List<HardpointPlayerStats>(stats.Count) { totalT2 };
        stats.Sort((a, b) => b.XPGained.CompareTo(a.XPGained));
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

public class HardpointPlayerStats : TeamPlayerStats, IExperienceStats, IFOBStats, IRevivesStats, IPlayerDeathListener
{
    protected int _xp;
    protected int _credits;
    protected int _fobsDestroyed;
    protected int _fobsPlaced;
    protected int _revives;
    internal float CaptureSeconds;
    internal int ObjectiveKills;
    internal int Hardpoints;
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
        if (obj != null && obj.PlayerInRange(_player.Position))
        {
            timeonpoint += dt;
            timedeployed += dt;
            if (Data.Is(out Hardpoint hp) && hp.ObjectiveState is 1 or 2 && hp.ObjectiveState == _player.GetTeam())
                CaptureSeconds += dt;
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
    void IPlayerDeathListener.OnPlayerDeath(PlayerDied e)
    {
        if (Data.Is(out Hardpoint hp) &&
            !e.WasTeamkill &&
            e.Killer != null &&
            hp.Objective.PlayerInRange(e.Killer.Player))
        {
            ++ObjectiveKills;
        }
    }
}