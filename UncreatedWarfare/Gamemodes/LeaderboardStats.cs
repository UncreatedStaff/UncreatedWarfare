﻿using JetBrains.Annotations;
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
    public const string EmptyFieldNamePlaceholder = "---";
    public const string EmptyFieldPlaceholder = "--";
    public static bool LeaderboardUp(this Gamemode? gamemode) => gamemode is IEndScreen { IsScreenUp: true };
    public static bool LeaderboardUp(this Gamemode? gamemode, out ILeaderboard lb)
    {
        if (gamemode is IImplementsLeaderboard { State: not State.Active and not State.Staging, IsScreenUp: true, Leaderboard: { } lb2 }
            && (lb2 is not MonoBehaviour o || o.isActiveAndEnabled))
        {
            lb = lb2;
            return true;
        }

        lb = null!;
        return false;
    }
    public static bool LeaderboardUp<TStats, TStatTracker>(this Gamemode? gamemode, out ILeaderboard<TStats, TStatTracker> lb) where TStats : BasePlayerStats where TStatTracker : BaseStatTracker<TStats>
    {
        if (gamemode is IImplementsLeaderboard<TStats, TStatTracker> { State: not State.Active and not State.Staging, IsScreenUp: true, Leaderboard: { } lb2 }
            && (lb2 is not MonoBehaviour o || o.isActiveAndEnabled))
        {
            lb = lb2;
            return true;
        }

        lb = null!;
        return false;
    }
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
                UCWarfare.RunTask(req.KitManager.Requests.ResupplyKit(player), ctx: "Resupplying " + player + "'s kit for leaderboard.");
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

public interface ILeaderboard<in TStats, in TStatTracker> : ILeaderboard where TStats : BasePlayerStats where TStatTracker : BaseStatTracker<TStats>
{
    void StartLeaderboard(ulong winner, TStatTracker tracker);
}
public interface ILeaderboard
{
    ulong Winner { get; }
    void OnPlayerJoined(UCPlayer player);
    void SetShutdownConfig(bool isShuttingDown, string? reason = null);
    void UpdateLeaderboardTimer();
    void Calculate();
    void SendLeaderboard();

    event VoidDelegate? OnLeaderboardExpired;
}

public abstract class Leaderboard<TStats, TStatTracker> : MonoBehaviour, ILeaderboard<TStats, TStatTracker> where TStats : BasePlayerStats where TStatTracker : BaseStatTracker<TStats>
{
    public ulong Winner { get; protected set; }
    protected TStatTracker Tracker;
    protected float SecondsLeft;
    protected bool ShuttingDown;
    protected string? ShuttingDownMessage;
    protected abstract UnturnedUI UI { get; }
    public void SetShutdownConfig(bool isShuttingDown, string? reason = null)
    {
        ShuttingDown = isShuttingDown;
        ShuttingDownMessage = reason;
    }
    public event VoidDelegate? OnLeaderboardExpired;
    public void StartLeaderboard(ulong winner, TStatTracker tracker)
    {
        Winner = winner;
        Tracker = tracker;
        Calculate();
        SendLeaderboard();
        SecondsLeft = Gamemode.Config.GeneralLeaderboardTime;
        StartCoroutine(StartUpdatingTimer());
    }
    protected virtual IEnumerator<WaitForSeconds> StartUpdatingTimer()
    {
        while (SecondsLeft > 0)
        {
            SecondsLeft -= 1f;
            yield return new WaitForSeconds(1f);
            UpdateLeaderboardTimer();
        }
        if (ShuttingDown)
        {
            Provider.shutdown(0, ShuttingDownMessage);
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
    object? GetPlayerStats(ulong player);
}
public abstract class BaseStatTracker<TIndividualStats> : MonoBehaviour, IStatTracker where TIndividualStats : BasePlayerStats
{
    private DateTime start;
    public TimeSpan Duration { get => DateTime.Now - start; }
    public int Ticks => coroutinect;
    protected int coroutinect;
    public List<TIndividualStats> stats;
    protected Coroutine ticker;
    protected float deltaTime;
    private float lastTickTime;
    [UsedImplicitly]
    private void Awake() => Reset();
    public float GetPresence(IPresenceStats stats) => (float)stats.OnlineTicks / coroutinect;
    public float GetPresence(ITeamPresenceStats stats, ulong team) => team == 1 ? ((float)stats.OnlineTicksT1 / coroutinect) : (team == 2 ? (stats.OnlineTicksT2 / coroutinect) : 0f);
    public virtual void Reset()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        stats ??= new List<TIndividualStats>();
        coroutinect = 0;

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            bool found = false;
            for (int j = 0; j < stats.Count; ++j)
            {
                if (stats[j].Steam64 == pl.Steam64)
                {
                    TIndividualStats st = stats[j];
                    st.Player = pl;
                    if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                        pt.Stats = st;
                    st.Reset();
                    found = true;
                }
            }
            if (!found)
            {
                TIndividualStats s = BasePlayerStats.New<TIndividualStats>(pl);
                stats.Add(s);
                if (pl.Player.TryGetPlayerData(out UCPlayerData pt))
                    pt.Stats = s;
            }
        }

        for (int i = stats.Count - 1; i >= 0; --i)
        {
            TIndividualStats s = stats[i];
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
        bool found = false;
        for (int j = 0; j < stats.Count; ++j)
        {
            if (stats[j].Steam64 == player.Steam64)
            {
                TIndividualStats st = stats[j];
                st.Player = player;
                if (player.Player.TryGetPlayerData(out UCPlayerData pt))
                    pt.Stats = st;
                found = true;
            }
        }
        if (!found)
        {
            TIndividualStats s = BasePlayerStats.New<TIndividualStats>(player);
            stats.Add(s);
            if (player.Player.TryGetPlayerData(out UCPlayerData pt))
                pt.Stats = s;
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
    private IEnumerator<WaitForSecondsRealtime> Ticker()
    {
        lastTickTime = Time.realtimeSinceStartup;
        while (true)
        {
            OnTick();
            lastTickTime = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(BasePlayerStats.TickTime);
        }
    }
    public TIndividualStats? GetPlayerStats(ulong player)
    {
        if (stats != null)
        {
            for (int i = 0; i < stats.Count; ++i)
            {
                if (stats[i].Steam64 == player) return stats[i];
            }
        }
        return null;
    }
    object? IStatTracker.GetPlayerStats(ulong player) => GetPlayerStats(player);
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
    public const float TickTime = 10f;
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
    public int vehicleKills;
    public int aircraftKills;
    public float damage;
    public int Kills => kills;
    public int Deaths => deaths;
    public int VehicleKills => vehicleKills;
    public int AircraftKills => aircraftKills;
    public float DamageDone => damage;
    public float KDR => deaths == 0 ? kills : kills / (float)deaths;
    public void AddDamage(float amount) => damage += amount;
    public void AddDeath() => deaths++;
    public void AddKill() => kills++;
    public void AddVehicleKill() => vehicleKills++;
    public void AddAircraftKill() => aircraftKills++;
    protected FFAPlayerStats(UCPlayer player) : base(player) { }
    protected FFAPlayerStats(ulong player) : base(player) { }
    public override void Reset()
    {
        base.Reset();
        kills = 0;
        deaths = 0;
        vehicleKills = 0;
        aircraftKills = 0;
        damage = 0;
    }
}

public abstract class TeamPlayerStats : BasePlayerStats, ITeamPVPModeStats, ITeamPresenceStats
{
    public int onlineCount1;
    public int onlineCount2;
    public int kills;
    public int deaths;
    public int vehicleKills;
    public int aircraftKills;
    public int teamkills;
    public float damage;
    public float timeonpoint;
    public float timedeployed;
    public float timeinvehicle;
    public TeamPlayerStats(UCPlayer player) : base(player) { }
    public TeamPlayerStats(ulong player) : base(player) { }
    public int Teamkills => teamkills;
    public int Kills => kills;
    public int Deaths => deaths;
    public int VehicleKills => vehicleKills;
    public int AircraftKills => aircraftKills;
    public float DamageDone => damage;
    public float KDR => deaths == 0 ? kills : kills / (float)deaths;
    public int OnlineTicksT1 => onlineCount1;
    public int OnlineTicksT2 => onlineCount2;
    public void AddDamage(float amount) => damage += amount;
    public void AddDeath() => deaths++;
    public void AddKill() => kills++;
    public void AddVehicleKill() => vehicleKills++;
    public void AddAircraftKill() => aircraftKills++;
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

        if (_player.IsInVehicle)
            timeinvehicle += dt;
    }

    public override void Reset()
    {
        base.Reset();
        onlineCount1 = 0;
        onlineCount2 = 0;
        kills = 0;
        deaths = 0;
        vehicleKills = 0;
        aircraftKills = 0;
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
        IsValue = true;
        Player = player;
        Distance = distance;
        Gun = gun;
        Team = team;
        Name = name;
    }
}