using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;

public static class LeaderboardEx
{
    public const string NO_PLAYER_NAME_PLACEHOLDER = "---";
    public const string NO_PLAYER_VALUE_PLACEHOLDER = "--";
    public static void RemoveLeaderboardModifiers(UCPlayer player)
    {
        player.Player.movement.sendPluginSpeedMultiplier(1f);
        player.Player.movement.sendPluginJumpMultiplier(1f);
        player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.Default);
    }
    public static void ApplyLeaderboardModifiers(UCPlayer player)
    {
        try
        {
            ulong team = player.GetTeam();
            player.Player.movement.sendPluginSpeedMultiplier(0f);
            player.Player.life.sendRevive();
            player.Player.movement.sendPluginJumpMultiplier(0f);
            player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.None);

            if (Data.Is(out IRevives r)) r.ReviveManager.RevivePlayer(player.Player);

            if (!player.Player.life.isDead)
            {
                Zone? zone = TeamManager.GetMain(team);
                player.Player.teleportToLocationUnsafe(zone != null ? zone.Center3D : TeamManager.LobbySpawn, TeamManager.GetMainYaw(team));
            }
            else
                player.Player.life.ServerRespawn(false);

            if (Data.Is(out IKitRequests req))
            {
                UCWarfare.RunTask(req.KitManager.ResupplyKit(player), ctx: "Resupplying " + player + "'s kit for leaderboard.");
            }
            if (Data.Is<IFlagRotation>())
                CTFUI.ClearFlagList(player.Connection);
        }
        catch (Exception ex)
        {
            L.LogError($"Error applying end screen conditions to {player.Steam64}.");
            L.LogError(ex);
        }
    }
}

public abstract class Leaderboard<Stats, StatTracker> : MonoBehaviour where Stats : BasePlayerStats where StatTracker : BaseStatTracker<Stats>
{
    public ulong Winner => _winner;
    protected ulong _winner;
    protected StatTracker tracker;
    private Coroutine endGameUpdateTimer;
    protected float secondsLeft;
    protected bool shuttingDown;
    protected string? shuttingDownMessage = null;
    protected abstract UnturnedUI UI { get; }
    public void SetShutdownConfig(bool isShuttingDown, string? reason = null)
    {
        shuttingDown = isShuttingDown;
        shuttingDownMessage = reason;
    }
    public VoidDelegate? OnLeaderboardExpired;
    public void StartLeaderboard(ulong winner, StatTracker tracker)
    {
        this._winner = winner;
        this.tracker = tracker;
        Calculate();
        SendLeaderboard();
        secondsLeft = Gamemode.Config.GeneralLeaderboardTime;
        endGameUpdateTimer = StartCoroutine(StartUpdatingTimer());
    }
    protected virtual IEnumerator<WaitForSeconds> StartUpdatingTimer()
    {
        while (secondsLeft > 0)
        {
            secondsLeft -= 1f;
            yield return new WaitForSeconds(1f);
            UpdateLeaderboardTimer();
        }
        if (shuttingDown)
        {
            Provider.shutdown(0, shuttingDownMessage);
        }
        else
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                LeaderboardEx.RemoveLeaderboardModifiers(PlayerManager.OnlinePlayers[i]);
            UI.ClearFromAllPlayers();
            OnLeaderboardExpired?.Invoke();
        }
    }
    public abstract void UpdateLeaderboardTimer();
    public abstract void Calculate();
    public abstract void SendLeaderboard();
    public abstract void OnPlayerJoined(UCPlayer player);
    protected virtual void Update() { }
}

public interface IStatTracker
{
    TimeSpan Duration { get; }
    void OnPlayerJoin(UCPlayer player);
    void Reset();
    float GetPresence(IPresenceStats stats);
    float GetPresence(ITeamPresenceStats stats, ulong team);
    void ClearAllStats();
    void StartTracking();
}
public abstract class BaseStatTracker<IndividualStats> : MonoBehaviour, IStatTracker where IndividualStats : BasePlayerStats
{
    private DateTime start;
    public TimeSpan Duration { get => DateTime.Now - start; }
    public int Ticks => coroutinect;
    protected int coroutinect;
    public List<IndividualStats> stats;
    protected Coroutine ticker;
    [UsedImplicitly]
    private void Awake() => Reset();
    public float GetPresence(IPresenceStats stats) => (float)stats.OnlineTicks / coroutinect;
    public float GetPresence(ITeamPresenceStats stats, ulong team) => team == 1 ? ((float)stats.OnlineTicksT1 / coroutinect) : (team == 2 ? (stats.OnlineTicksT2 / coroutinect) : 0f);
    public virtual void Reset()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        stats ??= new List<IndividualStats>();
        coroutinect = 0;

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            bool found = false;
            for (int j = 0; j < stats.Count; ++j)
            {
                if (stats[j].Steam64 == pl.Steam64)
                {
                    IndividualStats st = stats[j];
                    st.Player = pl;
                    if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                        pt.Stats = st;
                    st.Reset();
                    found = true;
                }
            }
            if (!found)
            {
                IndividualStats s = BasePlayerStats.New<IndividualStats>(pl);
                stats.Add(s);
                if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                    pt.Stats = s;
            }
        }

        for (int i = stats.Count - 1; i >= 0; --i)
        {
            IndividualStats s = stats[i];
            if (s.Player != null) continue;
            SteamPlayer player = PlayerTool.getSteamPlayer(s.Steam64);
            if (player == null) stats.RemoveAt(i);
        }
        StartTracking();
        L.LogDebug("Reset game stats, " + stats.Count + " trackers");
    }
    public void OnPlayerJoin(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            bool found = false;
            for (int j = 0; j < stats.Count; ++j)
            {
                if (stats[j].Steam64 == pl.Steam64)
                {
                    IndividualStats st = stats[j];
                    st.Player = pl;
                    if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                        pt.Stats = st;
                    found = true;
                }
            }
            if (!found)
            {
                IndividualStats s = BasePlayerStats.New<IndividualStats>(pl);
                stats.Add(s);
                if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                    pt.Stats = s;
            }
        }
        L.LogDebug(player.CharacterName + " added to playerstats, " + stats.Count + " trackers");
    }
    public void ClearAllStats()
    {
        for (int i = 0; i < stats.Count; ++i)
        {
            UCPlayer? pl = UCPlayer.FromID(stats[i].Steam64);
            if (pl != null && F.TryGetPlayerData(pl.Player, out UCPlayerData data))
                data.Stats = null!;
        }

        stats.Clear();
    }
    public virtual void StartTracking()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        start = DateTime.Now;
        coroutinect = 0;
        StartTicking();
    }
    protected virtual void OnTick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        coroutinect++;
    }
    protected void StopTicking()
    {
        if (ticker == null) return;
        StopCoroutine(ticker);
    }
    protected void StartTicking()
    {
        StopTicking();
        ticker = StartCoroutine(Ticker());
    }
    private IEnumerator<WaitForSeconds> Ticker()
    {
        while (true)
        {
            OnTick();
            yield return new WaitForSeconds(10f);
        }
    }
}

public abstract class TeamStatTracker<IndividualStats> : BaseStatTracker<IndividualStats> where IndividualStats : TeamPlayerStats
{
    public int casualtiesT1;
    public int casualtiesT2;
    public int teamkillsT1;
    public int teamkillsT2;
    protected int t1sizetotal;
    protected int t2sizetotal;
    public float AverageTeam1Size => t1sizetotal / (float)coroutinect;
    public float AverageTeam2Size => t1sizetotal / (float)coroutinect;
    public override void Reset()
    {
        base.Reset();
        casualtiesT1 = 0;
        casualtiesT2 = 0;
        teamkillsT1 = 0;
        teamkillsT2 = 0;
        t1sizetotal = 0;
        t2sizetotal = 0;
    }
    protected virtual void Start()
    {
        EventDispatcher.PlayerDied += OnPlayerDied;
    }
    protected virtual void OnDestroy()
    {
        EventDispatcher.PlayerDied -= OnPlayerDied;
    }
    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < stats.Count; i++)
            stats[i].Update(dt);
    }
    protected override void OnTick()
    {
        base.OnTick();
        for (int i = 0; i < stats.Count; i++)
            stats[i].Tick();

        for (int i = 0; i < Provider.clients.Count; i++)
        {
            int team = Provider.clients[i].GetTeamByte();
            if (team == 1)
                t1sizetotal++;
            else if (team == 2)
                t2sizetotal++;
        }
    }
    protected virtual void OnPlayerDied(PlayerDied e)
    {
        UCPlayerData c;
        if (e.Killer is not null)
        {
            if (e.WasTeamkill)
            {
                if (e.DeadTeam == 1)
                    ++teamkillsT1;
                else if (e.DeadTeam == 2)
                    ++teamkillsT2;
                if (e.Killer.Player.TryGetPlayerData(out c) && c.Stats is ITeamPVPModeStats tpvp)
                    tpvp.AddTeamkill();
            }
            else
            {
                if (e.Killer.Player.TryGetPlayerData(out c))
                {
                    if (c.Stats is IPVPModeStats kd)
                        kd.AddKill();
                    if (c.Stats is BaseCTFStats st && e.Killer.Player.IsOnFlag())
                        st.AddKillOnPoint();
                }
                if (this is ILongestShotTracker ls && e.Cause is EDeathCause.GUN or EDeathCause.SPLASH && 
                   (ls.LongestShot.Player == default || ls.LongestShot.Distance < e.KillDistance))
                {
                    ls.LongestShot = new LongestShot(e.Killer.Steam64, e.KillDistance, e.PrimaryAsset, e.KillerTeam, e.Killer.Name);
                }
            }
        }
        if (e.Player.Player.TryGetPlayerData(out c) && c.Stats is IPVPModeStats kd2)
            kd2.AddDeath();
        if (e.DeadTeam == 1)
            ++casualtiesT1;
        else if (e.DeadTeam == 2)
            ++casualtiesT2;
    }
}

public abstract class BasePlayerStats : IPresenceStats
{
    public PlayerNames? cachedNames;
    protected UCPlayer _player;
    public UCPlayer Player { get => _player; set => _player = value; }
    public int onlineCount;
    public readonly ulong _id;
    public int OnlineTicks => onlineCount;
    public ulong Steam64 => _id;
    public static T New<T>(UCPlayer player) where T : BasePlayerStats
    {
        return (T)Activator.CreateInstance(typeof(T), new object[] { player });
    }
    public static T New<T>(ulong player) where T : BasePlayerStats
    {
        return (T)Activator.CreateInstance(typeof(T), new object[] { player });
    }
    protected BasePlayerStats(UCPlayer player) : this(player.Steam64)
    {
        cachedNames = player.Name;
        _player = player;
    }
    protected BasePlayerStats(ulong player)
    {
        _id = player;
    }
    public virtual void Reset()
    {
        onlineCount = 0;
    }
    public virtual void Tick()
    {
        if (_player is null || !_player.IsOnline)
        {
            _player = UCPlayer.FromID(_id)!;
        }
        if (_player is not null)
        {
            OnlineTick();
        }
    }
    protected virtual void OnlineTick()
    {
        onlineCount++;
    }
}

public abstract class FFAPlayerStats : BasePlayerStats, IPVPModeStats
{
    public int kills;
    public int deaths;
    public float damage;
    public int Kills => kills;
    public int Deaths => deaths;
    public float DamageDone => damage;
    public float KDR => deaths == 0 ? kills : kills / (float)deaths;
    public void AddDamage(float amount) => damage += amount;
    public void AddDeath() => deaths++;
    public void AddKill() => kills++;
    protected FFAPlayerStats(UCPlayer player) : base(player) { }
    protected FFAPlayerStats(ulong player) : base(player) { }
    public override void Reset()
    {
        base.Reset();
        kills = 0;
        deaths = 0;
        damage = 0;
    }
}

public abstract class TeamPlayerStats : BasePlayerStats, ITeamPVPModeStats, ITeamPresenceStats
{
    public int onlineCount1;
    public int onlineCount2;
    public int kills;
    public int deaths;
    public int teamkills;
    public float damage;
    public float timeonpoint;
    public float timedeployed;
    public TeamPlayerStats(UCPlayer player) : base(player) { }
    public TeamPlayerStats(ulong player) : base(player) { }
    public int Teamkills => teamkills;
    public int Kills => kills;
    public int Deaths => deaths;
    public float DamageDone => damage;
    public float KDR => deaths == 0 ? kills : kills / (float)deaths;
    public int OnlineTicksT1 => onlineCount1;
    public int OnlineTicksT2 => onlineCount2;
    public void AddDamage(float amount) => damage += amount;
    public void AddDeath() => deaths++;
    public void AddKill() => kills++;
    public void AddTeamkill() => teamkills++;
    public virtual void Update(float dt)
    {
        if (_player is null || !_player.IsOnline) return;
        if (_player.Player.IsOnFlag())
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
        onlineCount1 = 0;
        onlineCount2 = 0;
        kills = 0;
        deaths = 0;
        damage = 0;
        teamkills = 0;
        timeonpoint = 0;
        timedeployed = 0;
    }
    protected override void OnlineTick()
    {
        base.OnlineTick();
        byte team = _player.Player.GetTeamByte();
        if (team == 1)
            onlineCount1++;
        else if (team == 2)
            onlineCount2++;
    }
}

public readonly struct LongestShot
{
    public static readonly LongestShot Nil = default;
    public readonly bool IsValue;
    public readonly ulong Player;
    public readonly PlayerNames Name;
    public readonly float Distance;
    public readonly Guid Gun;
    public readonly ulong Team;
    public LongestShot(ulong player, float distance, Guid gun, ulong team, PlayerNames name)
    {
        this.IsValue = true;
        Player = player;
        Distance = distance;
        Gun = gun;
        Team = team;
        Name = name;
    }
}