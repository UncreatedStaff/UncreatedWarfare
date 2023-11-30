using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare;

public class CooldownManager : ConfigSingleton<Config<CooldownConfig>, CooldownConfig>
{
    public static CooldownManager Singleton;
    public new static bool IsLoaded => Singleton.IsLoaded();
    public new static CooldownConfig Config => Singleton.IsLoaded() ? Singleton.ConfigurationFile.Data : null!;
    internal List<Cooldown> Cooldowns;
    public CooldownManager() : base("cooldowns", Data.Paths.CooldownStorage, "config.json") { }
    public override void Load()
    {
        Cooldowns = new List<Cooldown>(64);
        Singleton = this;
        PermissionSaver.Instance.SetPlayerPermissionLevel(76561198267927009, EAdminType.ADMIN_ON_DUTY);
        base.Load();
    }
    public override void Unload()
    {
        base.Unload();
        Singleton = null!;
        Cooldowns.Clear();
        Cooldowns = null!;
    }

    /// <exception cref="SingletonUnloadedException"/>
    public static void StartCooldown(UCPlayer player, CooldownType type, float seconds, params object[] data)
    {
        if (seconds <= 0f) return;
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasCooldown(player, type, out Cooldown existing, data))
            existing.TimeAdded = Time.realtimeSinceStartup;
        else
            Singleton.Cooldowns.Add(new Cooldown(player, type, seconds, data));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public static bool HasCooldown(UCPlayer player, CooldownType type, out Cooldown cooldown, params object[] data)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.Cooldowns.RemoveAll(c => c.Player == null || c.SecondsLeft <= 0f);
        cooldown = Singleton.Cooldowns.Find(c => c.CooldownType == type && c.Player.Steam64 == player.Steam64 && StatesEqual(data, c.Parameters));
        return cooldown != null;
    }
    private static bool StatesEqual(object[] state1, object[] state2)
    {
        if (state1 is null && state2 is null) return true;
        if (state1 is null) return state2.Length == 0;
        if (state2 is null) return state1.Length == 0;

        if (state1.Length != state2.Length) return false;

        for (int i = 0; i < state1.Length; ++i)
        {
            object objA = state1[i], objB = state2[i];

            if (!ReferenceEquals(objA, objB))
                return false;
            if ((objA is IComparable || objB is IComparable) && Comparer.Default.Compare(objA, objB) != 0)
                return false;
            if (objA != null ^ objB != null)
            {
                if (objA != null && !objA.Equals(objB))
                    return false;
            }
        }
        return true;
    }

    /// <exception cref="SingletonUnloadedException"/>
    public static bool HasCooldownNoStateCheck(UCPlayer player, CooldownType type, out Cooldown cooldown)
    {
        Singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Singleton.Cooldowns.RemoveAll(c => c.Player == null || c.Timeleft.TotalSeconds <= 0);
        cooldown = Singleton.Cooldowns.Find(c => c.CooldownType == type && c.Player.CSteamID == player.CSteamID);
        return cooldown != null;
    }
    public static void RemoveCooldown(UCPlayer player, CooldownType type)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.Cooldowns.RemoveAll(c => c.Player == null || c.Player.CSteamID == player.CSteamID && c.CooldownType == type);
    }
    public static void RemoveCooldown(UCPlayer player)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.Cooldowns.RemoveAll(c => c.Player.CSteamID == player.CSteamID);
    }
    public static void RemoveCooldown(CooldownType type)
    {
        if (!Singleton.IsLoaded()) return;
        Singleton.Cooldowns.RemoveAll(x => x.Player == null || x.CooldownType == type);
    }

    public static void OnGameStarting()
    {
        Singleton.Cooldowns.RemoveAll(x => x.CooldownType is not CooldownType.Report);
    }

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public static float GetFOBDeployCooldown() => GetFOBDeployCooldown(Provider.clients.Count(x => x.GetTeam() is 1 or 2));

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public static float GetFOBDeployCooldown(int players)
    {
        players = Mathf.Clamp(players, Config.DeployFOBPlayersMin, Config.DeployFOBPlayersMax);

        float a = Config.DeployFOBCooldownAlpha;
        if (a == 0f)
            a = 2f;

        // (LaTeX)
        // base function: f\left(x\right)=\left(1-\left(1-t\right)^{a}\right)
        // scaled: \left(C_{2}-C_{1}\right)f\left(\left(x-P_{1}\right)\frac{1}{\left(P_{2}-P_{1}\right)}\right)+C_{1}

        return Config.DeployFOBCooldownMin +
               (Config.DeployFOBCooldownMax - Config.DeployFOBCooldownMin) *
               (1f - Mathf.Pow(1 -
                   /* t = */ (players - Config.DeployFOBPlayersMin) * (1f / (Config.DeployFOBPlayersMax - Config.DeployFOBPlayersMin))
                   , a)
               );
    }
}
public class CooldownConfig : JSONConfigData
{
    public RotatableConfig<float> DeployFOBCooldownMin { get; set; }
    public RotatableConfig<float> DeployFOBCooldownMax { get; set; }
    public RotatableConfig<int> DeployFOBPlayersMin { get; set; }
    public RotatableConfig<int> DeployFOBPlayersMax { get; set; }
    public RotatableConfig<float> DeployFOBCooldownAlpha { get; set; }
    public RotatableConfig<float> RequestKitCooldown { get; set; }
    public RotatableConfig<float> RequestVehicleCooldown { get; set; }
    public RotatableConfig<float> ReviveXPCooldown { get; set; }
    public RotatableConfig<float> GlobalTraitCooldown { get; set; }
    public override void SetDefaults()
    {
        DeployFOBCooldownMin = 60;
        DeployFOBCooldownMax = 90;
        DeployFOBPlayersMin = 24;
        DeployFOBPlayersMax = 60;
        DeployFOBCooldownAlpha = 2f;
        RequestKitCooldown = 120;
        RequestVehicleCooldown = 240;
        ReviveXPCooldown = 150f;
        GlobalTraitCooldown = 0f;
    }
}
public class Cooldown : ITranslationArgument
{
    public UCPlayer Player { get; }
    public CooldownType CooldownType { get; }
    public double TimeAdded { get; set; }
    public float Duration { get; set; }
    public object[] Parameters { get; }
    public TimeSpan Timeleft => TimeSpan.FromSeconds(Math.Max(0d, Duration - (Time.realtimeSinceStartupAsDouble - TimeAdded)));
    public float SecondsLeft => Mathf.Max(0f, Duration - (Time.realtimeSinceStartup - (float)TimeAdded));

    public Cooldown(UCPlayer player, CooldownType cooldownType, float duration, params object[] parameters)
    {
        Player = player;
        CooldownType = cooldownType;
        TimeAdded = Time.realtimeSinceStartupAsDouble;
        Duration = duration;
        Parameters = parameters;
    }
    public override string ToString()
    {
        double sec = Duration - (Time.realtimeSinceStartupAsDouble - TimeAdded);

        if (sec <= 1d) return "1s";

        string line = string.Empty;

        int i1 = (int)sec / 3600;
        if (i1 > 0)
            line += i1.ToString(Data.LocalLocale) + "h ";
        sec -= i1 * 3600;

        i1 = (int)sec / 60;
        if (i1 > 0)
            line += i1.ToString(Data.LocalLocale) + "m ";
        sec -= i1 * 60;

        i1 = (int)sec;
        if (i1 > 0)
            return line + i1.ToString(Data.LocalLocale) + "s";
        if (line.Length == 0)
            return sec.ToString("F0", Data.LocalLocale) + "s";
        return line;
    }

    [FormatDisplay("Type (" + nameof(Warfare.CooldownType) + ")")]
    /// <summary>Translated <see cref="ECooldownType"/>.</summary>
    public const string FormatName = "n";
    [FormatDisplay("Long Time (3 hours and 4 minutes)")]
    /// <summary>3 hours and 4 minutes</summary>
    public const string FormatTimeLong = "tl1";
    [FormatDisplay("Short Time (3h 40m)")]
    /// <summary>3h 4m 20s</summary>
    public const string FormatTimeShort = "tl2";
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (!string.IsNullOrEmpty(format))
        {
            if (format!.Equals(FormatName, StringComparison.Ordinal))
                return Localization.TranslateEnum(CooldownType, language);
            if (format.Equals(FormatTimeLong, StringComparison.Ordinal))
                return Localization.GetTimeFromSeconds((int)Timeleft.TotalSeconds, language, culture);
            if (format.Equals(FormatTimeShort, StringComparison.Ordinal))
                return ToString();
            return Timeleft.ToString(format);
        }

        return ToString();
    }
}

[Translatable("Cooldown Type", IsPrioritizedTranslation = false)]
public enum CooldownType
{
    [Translatable("Combat")]
    Combat,
    [Translatable("Deploy")]
    Deploy,
    [Translatable("Ammo Request")]
    Ammo,
    [Translatable("Paid Kit Request")]
    PremiumKit,
    [Translatable("Kit Request")]
    RequestKit,
    [Translatable("Vehicle Request")]
    RequestVehicle,
    [Translatable("Vehicle Ammo Request")]
    AmmoVehicle,
    [Translatable("Team Change")]
    ChangeTeams,
    [Translatable("Report Player1")]
    Report,
    [Translatable("Revive Player")]
    Revive,
    [Translatable("Request Trait")]
    GlobalRequestTrait,
    [Translatable("Request Single Trait")]
    IndividualRequestTrait,
    [Translatable("Announce Action")]
    AnnounceAction,
    [Translatable("Rally")]
    Rally,
    [Translatable("Execute Command")]
    Command,
    [Translatable("Execute Command Portion")]
    PortionCommand,
    [Translatable("Interact Vehicle Seats")]
    InteractVehicleSeats
}
