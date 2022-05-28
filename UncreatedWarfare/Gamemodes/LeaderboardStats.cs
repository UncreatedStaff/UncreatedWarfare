using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
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
                player.Player.teleportToLocationUnsafe(team.GetBaseSpawnFromTeam(), team.GetBaseAngle());
            else
                player.Player.life.ServerRespawn(false);

            if (Data.Is<IKitRequests>(out _) && string.IsNullOrEmpty(player.KitName))
            {
                if (KitManager.KitExists(player.KitName, out Kit kit))
                    KitManager.ResupplyKit(player, kit);
            }
            if (Data.Is<IFlagRotation>(out _))
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
        secondsLeft = Gamemode.Config.GeneralConfig.LeaderboardTime;
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

public abstract class BaseStatTracker<IndividualStats> : MonoBehaviour where IndividualStats : BasePlayerStats
{
    private DateTime start;
    public TimeSpan Duration { get => DateTime.Now - start; }
    
    public int coroutinect;
    public Dictionary<ulong, IndividualStats> stats;
    protected Coroutine ticker;
    public void Awake() => Reset();
    public virtual void Reset()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (stats == null)
            stats = new Dictionary<ulong, IndividualStats>();
        coroutinect = 0;

        for (int i = 0; i < Provider.clients.Count; i++)
        {
            if (stats.TryGetValue(Provider.clients[i].playerID.steamID.m_SteamID, out IndividualStats p))
            {
                p.Player = Provider.clients[i].player;
                p.Reset();
            }
            else
            {
                IndividualStats s = BasePlayerStats.New<IndividualStats>(Provider.clients[i].player);
                stats.Add(Provider.clients[i].playerID.steamID.m_SteamID, s);
                if (Provider.clients[i].player.TryGetPlaytimeComponent(out PlaytimeComponent pt))
                    pt.stats = s;
            }
        }
        foreach (KeyValuePair<ulong, IndividualStats> c in stats.ToList())
        {
            if (c.Value.Player != null) continue;
            SteamPlayer player = PlayerTool.getSteamPlayer(c.Key);
            if (player == null) stats.Remove(c.Key);
        }
        StartTracking();
        L.Log("Reset game stats, " + stats.Count + " trackers");
    }
    public void OnPlayerJoin(Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!stats.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out IndividualStats s))
        {
            s = BasePlayerStats.New<IndividualStats>(player);
            stats.Add(player.channel.owner.playerID.steamID.m_SteamID, s);
            if (player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                c.stats = s;
        }
        else
        {
            s.Player = player;
            if (player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                c.stats = s;
        }
        L.LogDebug(player.name + " added to playerstats, " + stats.Count + " trackers");
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
        ticker = StartCoroutine(CompileAverages());
    }
    private IEnumerator<WaitForSeconds> CompileAverages()
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
    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        foreach (TeamPlayerStats s in stats.Values)
            s.Update(dt);
    }
    protected override void OnTick()
    {
        base.OnTick();
        foreach (IndividualStats s in stats.Values)
            s.Tick();
        for (int i = 0; i < Provider.clients.Count; i++)
        {
            byte team = Provider.clients[i].GetTeamByte();
            if (team == 1)
                t1sizetotal++;
            else if (team == 2)
                t2sizetotal++;
        }
    }
}

public abstract class BasePlayerStats : IStats
{
    protected Player _player;
    public Player Player { get => _player; set => _player = value; }
    public int onlineCount;
    public readonly ulong _id;
    public ulong Steam64 => _id;
    public static T New<T>(Player player) where T : BasePlayerStats
    {
        return (T)Activator.CreateInstance(typeof(T), new object[1] { player });
    }
    public static T New<T>(ulong player) where T : BasePlayerStats
    {
        return (T)Activator.CreateInstance(typeof(T), new object[1] { player });
    }
    public BasePlayerStats(Player player) : this(player.channel.owner.playerID.steamID.m_SteamID) { }
    public BasePlayerStats(ulong player)
    {
        _id = player;
    }
    public virtual void Reset()
    {
        onlineCount = 0;
    }
    public virtual void Tick()
    {
        if (_player == null)
        {
            SteamPlayer pl = PlayerTool.getSteamPlayer(_id);
            if (pl == null) return;
            else _player = pl.player;
        }
        if (_player != null)
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
    public FFAPlayerStats(Player player) : base(player) { }
    public FFAPlayerStats(ulong player) : base(player) { }
    public override void Reset()
    {
        base.Reset();
        kills = 0;
        deaths = 0;
        damage = 0;
    }
}

public abstract class TeamPlayerStats : BasePlayerStats, ITeamPVPModeStats
{
    public int onlineCount1;
    public int onlineCount2;
    public int kills;
    public int deaths;
    public int teamkills;
    public float damage;
    public float timeonpoint;
    public float timedeployed;
    public TeamPlayerStats(Player player) : base(player) { }
    public TeamPlayerStats(ulong player) : base(player) { }
    public int Teamkills => teamkills;
    public int Kills => kills;
    public int Deaths => deaths;
    public float DamageDone => damage;
    public float KDR => deaths == 0 ? kills : kills / (float)deaths;
    public void AddDamage(float amount) => damage += amount;
    public void AddDeath() => deaths++;
    public void AddKill() => kills++;
    public void AddTeamkill() => teamkills++;
    public void Update(float dt)
    {
        if (_player == null) return;
        if (_player.IsOnFlag())
        {
            timeonpoint += dt;
            timedeployed += dt;
        }
        else if (!_player.IsInMain())
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
        byte team = _player.GetTeamByte();
        if (team == 1)
            onlineCount1++;
        else if (team == 2)
            onlineCount2++;
    }
}

public readonly struct LongestShot
{
    public static readonly LongestShot Nil = new LongestShot();
    public readonly bool IsValue;
    public readonly ulong Player;
    public readonly float Distance;
    public readonly Guid Gun;
    public readonly ulong Team;
    public LongestShot()
    {
        IsValue = false;
        Player = 0;
        Distance = 0;
        Gun = default;
        Team = 0;
    }
    public LongestShot(ulong player, float distance, Guid gun, ulong team)
    {
        this.IsValue = true;
        Player = player;
        Distance = distance;
        Gun = gun;
        Team = team;
    }
}