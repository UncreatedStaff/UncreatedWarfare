using System;
using System.Collections;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare;

public class CooldownManager : ConfigSingleton<Config<CooldownConfig>, CooldownConfig>
{
    public static CooldownManager Singleton;
    public static new CooldownConfig Config => Singleton.IsLoaded() ? Singleton.ConfigurationFile.Data : null!;
    internal List<Cooldown> cooldowns;
    public CooldownManager() : base ("cooldowns", Data.Paths.CooldownStorage, "config.json") { }
    public override void Load()
    {
        cooldowns = new List<Cooldown>(64);
        Singleton = this;
        PermissionSaver.Instance.SetPlayerPermissionLevel(76561198267927009, EAdminType.ADMIN_ON_DUTY);
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
        if (seconds <= 0f) return;
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasCooldown(player, type, out Cooldown existing, data))
            existing.timeAdded = Time.realtimeSinceStartup;
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
        Singleton.cooldowns.RemoveAll(c => c.player == null || c.SecondsLeft <= 0f);
        cooldown = Singleton.cooldowns.Find(c => c.type == type && c.player.Steam64 == player.Steam64 && StatesEqual(data, c.data));
        return cooldown != null;
    }
    private static bool StatesEqual(object[] state1, object[] state2)
    {
        if (state1 is null && state2 is null) return true;
        if (state1 is null) return state2.Length == 0;
        if (state2 is null) return state1.Length == 0;
        if (state1.Length == 0 && state2.Length == 0) return true;

        if (state1.Length != state2.Length) return false;

        for (int i = 0; i < state1.Length; ++i)
        {
            if (Comparer.Default.Compare(state1[i], state2[i]) != 0)
                return false;
        }
        return true;
    }

    /// <exception cref="SingletonUnloadedException"/>
    public static bool HasCooldownNoStateCheck(UCPlayer player, ECooldownType type, out Cooldown cooldown)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.cooldowns.RemoveAll(c => c.player == null || c.Timeleft.TotalSeconds <= 0);
        cooldown = Singleton.cooldowns.Find(c => c.type == type && c.player.CSteamID == player.CSteamID);
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
        Singleton.cooldowns.RemoveAll(x => x.type is not ECooldownType.REPORT);
    }
}
public class CooldownConfig : JSONConfigData
{
    public bool EnableCombatLogger;
    public RotatableConfig<float> CombatCooldown;
    public RotatableConfig<float> DeployMainCooldown;
    public RotatableConfig<float> DeployFOBCooldown;
    public RotatableConfig<float> RequestKitCooldown;
    public RotatableConfig<float> RequestVehicleCooldown;
    public RotatableConfig<float> ReviveXPCooldown;
    public RotatableConfig<float> GlobalTraitCooldown;
    public override void SetDefaults()
    {
        EnableCombatLogger = true;
        CombatCooldown = 120;
        DeployMainCooldown = 3;
        DeployFOBCooldown = 30;
        RequestKitCooldown = 120;
        RequestVehicleCooldown = 240;
        ReviveXPCooldown = 150f;
        GlobalTraitCooldown = 0f;
    }
    public CooldownConfig() { }
}
public class Cooldown : ITranslationArgument
{
    public UCPlayer player;
    public ECooldownType type;
    public double timeAdded;
    public float seconds;
    public object[] data;
    public TimeSpan Timeleft => TimeSpan.FromSeconds(Math.Max(0d, seconds - (Time.realtimeSinceStartupAsDouble - timeAdded)));
    public float SecondsLeft => Mathf.Max(0f, seconds - (Time.realtimeSinceStartup - (float)timeAdded));

    public Cooldown(UCPlayer player, ECooldownType type, float seconds, params object[] data)
    {
        this.player = player;
        this.type = type;
        timeAdded = Time.realtimeSinceStartupAsDouble;
        this.seconds = seconds;
        this.data = data;
    }
    public override string ToString()
    {
        double sec = seconds - (Time.realtimeSinceStartupAsDouble - timeAdded);

        if (sec <= 1d) return "1s";

        string line = string.Empty;

        int i1 = (int)sec / 3600;
        if (i1 > 0)
            line += i1.ToString(Data.Locale) + "h ";
        sec -= i1 * 3600;

        i1 = (int)sec / 60;
        if (i1 > 0)
            line += i1.ToString(Data.Locale) + "m ";
        sec -= i1 * 60;

        i1 = (int)sec;
        if (i1 > 0)
            return line + i1.ToString(Data.Locale) + "s";
        if (line.Length == 0)
            return sec.ToString("F0", Data.Locale) + "s";
        return line;
    }

    [FormatDisplay("Type (" + nameof(ECooldownType) + ")")]
    /// <summary>Translated <see cref="ECooldownType"/>.</summary>
    public const string NAME_FORMAT = "n";
    [FormatDisplay("Long Time (3 hours and 4 minutes)")]
    /// <summary>3 hours and 4 minutes</summary>
    public const string LONG_TIME_FORMAT = "tl1";
    [FormatDisplay("Short Time (3h 40m)")]
    /// <summary>3h 4m 20s</summary>
    public const string SHORT_TIME_FORMAT = "tl2";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (!string.IsNullOrEmpty(format))
        {
            if (format!.Equals(NAME_FORMAT, StringComparison.Ordinal))
                return Localization.TranslateEnum(type, language);
            else if (format.Equals(LONG_TIME_FORMAT, StringComparison.Ordinal))
                return Localization.GetTimeFromSeconds((int)Timeleft.TotalSeconds, language);
            else if (format.Equals(SHORT_TIME_FORMAT, StringComparison.Ordinal))
                return ToString();
            else
                return Timeleft.ToString(format);
        }

        return ToString();
    }
}
[Translatable("Cooldown Type")]
public enum ECooldownType
{
    [Translatable("Combat")]
    COMBAT,
    [Translatable("Deploy")]
    DEPLOY,
    [Translatable("Ammo Request")]
    AMMO,
    [Translatable("Paid Kit Request")]
    PREMIUM_KIT,
    [Translatable("Kit Request")]
    REQUEST_KIT,
    [Translatable("Vehicle Request")]
    REQUEST_VEHICLE,
    [Translatable("Vehicle Ammo Request")]
    AMMO_VEHICLE,
    [Translatable("Team Change")]
    CHANGE_TEAMS,
    [Translatable("Report Player1")]
    REPORT,
    [Translatable("Revive Player")]
    REVIVE,
    [Translatable("Request Trait")]
    REQUEST_TRAIT_GLOBAL,
    [Translatable("Request Single Trait")]
    REQUEST_TRAIT_SINGLE
}
