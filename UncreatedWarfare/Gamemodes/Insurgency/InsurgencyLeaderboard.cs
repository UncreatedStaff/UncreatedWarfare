using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Insurgency;

public class InsurgencyLeaderboard : ConventionalLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>
{
    public InsurgencyLeaderboard()
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
           new StatValue(T.InsurgencyPlayerStats3, (_, b, l) => Data.Gamemode is IAttackDefense atk ? (b!.Player.GetTeam() == atk.AttackingTeam ? b.KillsAttack : b.KillsDefense).ToString(l) : LeaderboardEx.EmptyFieldPlaceholder),
           new StatValue(T.InsurgencyPlayerStats4, (_, b, l) => TimeSpan.FromSeconds(b!.timedeployed).ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
           new StatValue(T.InsurgencyPlayerStats5, (_, b, l) => b!.XPGained.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats6, (_, b, l) => b!._intelligencePointsCollected.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats7, (_, b, l) => b!._cachesDiscovered.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats8, (_, b, l) => b!._cachesDestroyed.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats9, (_, b, l) => b!.Teamkills.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats10, (_, b, l) => b!.FOBsDestroyed.ToString(l)),
           new StatValue(T.InsurgencyPlayerStats11, (_, b, l) => b!.Credits.ToString(l))
        };
        WarStatOverrides = new StatValue[]
        {
           new StatValue(T.InsurgencyWarStats0, (a, _, l) => a!.Duration.ToString(ConventionalLeaderboardUI.StatFormatTime, l)),
           new StatValue(T.InsurgencyWarStats1, (a, _, l) => a!.casualtiesT1.ToString(l), 1ul),
           new StatValue(T.InsurgencyWarStats2, (a, _, l) => a!.casualtiesT2.ToString(l), 2ul),
           new StatValue(T.InsurgencyWarStats3, (a, _, l) => a!.intelligenceGathered.ToString(l)),
           new StatValue(T.InsurgencyWarStats4, (a, _, l) => a!.AverageTeam1Size.ToString(ConventionalLeaderboardUI.StatFormatFloat, l), 1ul),
           new StatValue(T.InsurgencyWarStats5, (a, _, l) => a!.AverageTeam2Size.ToString(ConventionalLeaderboardUI.StatFormatFloat,l), 2ul),
           new StatValue(T.InsurgencyWarStats6, (a, _, l) => a!.FOBsPlacedT1.ToString(l), 1ul),
           new StatValue(T.InsurgencyWarStats7, (a, _, l) => a!.FOBsPlacedT2.ToString(l), 2ul),
           new StatValue(T.InsurgencyWarStats8, (a, _, l) => a!.FOBsDestroyedT1.ToString(l), 1ul),
           new StatValue(T.InsurgencyWarStats9, (a, _, l) => a!.FOBsDestroyedT2.ToString(l), 2ul),
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
public class InsurgencyTracker : TeamStatTracker<InsurgencyPlayerStats>, ILongestShotTracker, IFobsTracker
{
    public int fobsPlacedT1;
    public int fobsPlacedT2;
    public int fobsDestroyedT1;
    public int fobsDestroyedT2;
    public int intelligenceGathered;
    public int FOBsPlacedT1 { get => fobsPlacedT1; set => fobsPlacedT1 = value; }
    public int FOBsPlacedT2 { get => fobsPlacedT2; set => fobsPlacedT2 = value; }
    public int FOBsDestroyedT1 { get => fobsDestroyedT1; set => fobsDestroyedT1 = value; }
    public int FOBsDestroyedT2 { get => fobsDestroyedT2; set => fobsDestroyedT2 = value; }
    internal LongestShot _longestShot = LongestShot.Nil;
    public LongestShot LongestShot { get => _longestShot; set => _longestShot = value; }
    public override void Reset()
    {
        base.Reset();
        fobsPlacedT1 = 0;
        fobsPlacedT2 = 0;
        fobsDestroyedT1 = 0;
        fobsDestroyedT2 = 0;
        intelligenceGathered = 0;
        _longestShot = LongestShot.Nil;
    }
    public virtual void GetTopStats(int count, out List<InsurgencyPlayerStats> statsT1, out List<InsurgencyPlayerStats> statsT2)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<InsurgencyPlayerStats> stats = this.stats.ToList();

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
        InsurgencyPlayerStats totalT1 = new InsurgencyPlayerStats(0UL);
        InsurgencyPlayerStats totalT2 = new InsurgencyPlayerStats(0UL);
        statsT1 = new List<InsurgencyPlayerStats>(stats.Count) { totalT1 };
        statsT2 = new List<InsurgencyPlayerStats>(stats.Count) { totalT2 };
        stats.Sort((InsurgencyPlayerStats a, InsurgencyPlayerStats b) => b.XPGained.CompareTo(a.XPGained));
        for (int i = 0; i < stats.Count; ++i)
        {
            InsurgencyPlayerStats stat = stats[i];

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
    protected override void OnPlayerDied(PlayerDied e)
    {
        if (!e.WasTeamkill && e.Killer is not null && Data.Is(out Insurgency ins))
        {
            Vector3 pos;
            if (e.KillerTeam == ins.DefendingTeam)
            {
                pos = e.Killer.Position;
                for (int i = 0; i < ins.Caches.Count; i++)
                {
                    Insurgency.CacheData d = ins.Caches[i];
                    if (d.IsActive && !d.IsDestroyed && d.Cache != null &&
                        (d.Cache.Position - pos)
                        .sqrMagnitude <=
                        Gamemode.ConfigObj.Data.InsurgencyCacheDiscoverRange *
                        Gamemode.ConfigObj.Data.InsurgencyCacheDiscoverRange)
                    {
                        if (e.Killer.Player.TryGetPlayerData(out UCPlayerData comp) &&
                            comp.Stats is InsurgencyPlayerStats ps) ps._killsDefense++;
                    }
                }
            }
            else if (e.KillerTeam == ins.AttackingTeam)
            {
                pos = e.Player.Position;
                for (int i = 0; i < ins.Caches.Count; i++)
                {
                    Insurgency.CacheData d = ins.Caches[i];
                    if (d.IsActive && !d.IsDestroyed && d.Cache != null &&
                        (d.Cache.Position - pos)
                        .sqrMagnitude <=
                        Gamemode.ConfigObj.Data.InsurgencyCacheDiscoverRange *
                        Gamemode.ConfigObj.Data.InsurgencyCacheDiscoverRange)
                    {
                        if (e.Killer.Player.TryGetPlayerData(out UCPlayerData comp) &&
                            comp.Stats is InsurgencyPlayerStats ps) ps._killsAttack++;
                    }
                }
            }
        }
        base.OnPlayerDied(e);
    }
}

public class InsurgencyPlayerStats : TeamPlayerStats, IExperienceStats, IFOBStats, IRevivesStats
{
    public InsurgencyPlayerStats(UCPlayer player) : base(player) { }
    public InsurgencyPlayerStats(ulong player) : base(player) { }

    protected int _xp;
    protected int _credits;
    protected int _fobsDestroyed;
    protected int _fobsPlaced;
    protected int _revives;
    internal int _killsAttack;
    internal int _killsDefense;
    internal int _cachesDestroyed;
    internal int _cachesDiscovered;
    internal int _intelligencePointsCollected;
    public int XPGained => _xp;
    public int Credits => _credits;
    public int FOBsDestroyed => _fobsDestroyed;
    public int FOBsPlaced => _fobsPlaced;
    public int Revives => _revives;
    public int KillsAttack => _killsAttack;
    public int KillsDefense => _killsDefense;
    public void AddFOBDestroyed() => _fobsDestroyed++;
    public void AddFOBPlaced() => _fobsPlaced++;
    public void AddCredits(int amount) => _credits += amount;
    public void AddXP(int amount) => _xp += amount;
    public void AddRevive() => _revives++;
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
