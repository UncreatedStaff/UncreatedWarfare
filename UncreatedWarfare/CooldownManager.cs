using System;
using System.Collections.Generic;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare;

public class CooldownManager : ConfigSingleton<Config<CooldownConfig>, CooldownConfig>
{
    private static CooldownManager Singleton;
    public static new CooldownConfig Config => Singleton.IsLoaded() ? Singleton.ConfigurationFile.Data : null!;
    private List<Cooldown> cooldowns;
    public CooldownManager() : base ("cooldowns", Data.CooldownStorage, "config.json") { }
    public override void Load()
    {
        cooldowns = new List<Cooldown>(64);
        Singleton = this;
        base.Load();
    }
    public override void Unload()
    {
        base.Unload();
        Singleton = null!;
        cooldowns.Clear();
        cooldowns = null!;
    }

    /// <exception cref="SingletonUnloadedException"/>
    public static void StartCooldown(UCPlayer player, ECooldownType type, float seconds, params object[] data)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasCooldown(player, type, out Cooldown existing))
            existing.timeAdded = DateTime.Now;
        else
            Singleton.cooldowns.Add(new Cooldown(player, type, seconds, data));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static bool HasCooldown(UCPlayer player, ECooldownType type, out Cooldown cooldown, params object[] data)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.cooldowns.RemoveAll(c => c.player == null || c.Timeleft.TotalSeconds <= 0);
        cooldown = Singleton.cooldowns.Find(c => c.player.CSteamID == player.CSteamID && c.type == type && c.data.Equals(data));
        return cooldown != null;
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static bool HasCooldownNoStateCheck(UCPlayer player, ECooldownType type, out Cooldown cooldown)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.cooldowns.RemoveAll(c => c.player == null || c.Timeleft.TotalSeconds <= 0);
        cooldown = Singleton.cooldowns.Find(c => c.player.CSteamID == player.CSteamID);
        return cooldown != null;
    }
    public static void RemoveCooldown(UCPlayer player, ECooldownType type)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.cooldowns.RemoveAll(c => c.player == null || c.player.CSteamID == player.CSteamID && c.type == type);
    }
    public static void RemoveCooldown(UCPlayer player)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.cooldowns.RemoveAll(c => c.player.CSteamID == player.CSteamID);
    }
    public static void RemoveCooldown(ECooldownType type)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.cooldowns.RemoveAll(x => x.player == null || x.type == type);
    }

    public static void OnGameStarting()
    {
        RemoveCooldown(ECooldownType.REQUEST_KIT);
        RemoveCooldown(ECooldownType.PREMIUM_KIT);
        RemoveCooldown(ECooldownType.REQUEST_VEHICLE);
    }
}
public class CooldownConfig : ConfigData
{
    public bool EnableCombatLogger;
    public float CombatCooldown;
    public float DeployMainCooldown;
    public float DeployFOBCooldown;
    public float RequestKitCooldown;
    public float RequestVehicleCooldown;
    public override void SetDefaults()
    {
        EnableCombatLogger = true;
        CombatCooldown = 120;
        DeployMainCooldown = 3;
        DeployFOBCooldown = 30;
        RequestKitCooldown = 120;
        RequestVehicleCooldown = 240;
    }
    public CooldownConfig() { }
}
public class Cooldown
{
    public UCPlayer player;
    public ECooldownType type;
    public DateTime timeAdded;
    public float seconds;
    public object[] data;
    public TimeSpan Timeleft
    {
        get => TimeSpan.FromSeconds((seconds - (DateTime.Now - timeAdded).TotalSeconds) >= 0 ? (seconds - (DateTime.Now - timeAdded).TotalSeconds) : 0);
    }

    public Cooldown(UCPlayer player, ECooldownType type, float seconds, params object[] data)
    {
        this.player = player;
        this.type = type;
        timeAdded = DateTime.Now;
        this.seconds = seconds;
        this.data = data;
    }
    public override string ToString()
    {
        TimeSpan time = Timeleft;

        string line = string.Empty;
        if (time.Hours > 0)
            line += time.Hours + "h ";
        if (time.Minutes > 0)
            line += time.Minutes + "m ";
        if (time.Seconds > 0)
            line += time.Seconds + "s";
        return line;
    }
}
public enum ECooldownType
{
    COMBAT,
    DEPLOY,
    AMMO,
    PREMIUM_KIT,
    REQUEST_KIT,
    REQUEST_VEHICLE,
    COMMAND_BOOST,
    COMMAND_WARN,
    WARNINGS,
    AMMO_VEHICLE,
    CHANGE_TEAMS,
    REPORT
}
