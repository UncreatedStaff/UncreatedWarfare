using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

public class CooldownManager : IHostedService, ILayoutHostedService
{
    internal List<Cooldown> Cooldowns = new List<Cooldown>(128);
    internal CooldownConfig Config = new CooldownConfig();

    private readonly IPlayerService _playerService;

    public CooldownManager(IPlayerService playerService)
    {
        _playerService = playerService;
    }


    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        Config.SetDefaults();
        return UniTask.CompletedTask;
    }
    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        Cooldowns.RemoveAll(x => ShouldResetOnGameStart(x.CooldownType));
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;
    UniTask ILayoutHostedService.StopAsync(CancellationToken token) => UniTask.CompletedTask;

    public static bool ShouldResetOnGameStart(CooldownType type) => type != CooldownType.Report;

    public void StartCooldown(WarfarePlayer player, CooldownType type, float seconds, params object[] data)
    {
        StartCooldown(player.Steam64, type, seconds, data);
    }

    public void StartCooldown(CSteamID player, CooldownType type, float seconds, params object[] data)
    {
        GameThread.AssertCurrent();

        if (seconds <= 0f || player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual) return;

        if (HasCooldown(player, type, out Cooldown? existing, data))
            existing.TimeAdded = Time.realtimeSinceStartup;
        else
            Cooldowns.Add(new Cooldown(player, type, seconds, data));
    }

    public bool HasCooldown(WarfarePlayer player, CooldownType type, params object[] data)
    {
        return HasCooldown(player.Steam64, type, out _, data);
    }
    public bool HasCooldown(WarfarePlayer player, CooldownType type, [MaybeNullWhen(false)] out Cooldown cooldown, params object[] data)
    {
        return HasCooldown(player.Steam64, type, out cooldown, data);
    }

    public bool HasCooldown(CSteamID player, CooldownType type, params object[] data)
    {
        return HasCooldown(player, type, out _, data);
    }
    public bool HasCooldown(CSteamID player, CooldownType type, [MaybeNullWhen(false)] out Cooldown cooldown, params object[] data)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.SecondsLeft <= 0f);

        if (player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            cooldown = null;
            return false;
        }

        cooldown = Cooldowns.Find(c => c.CooldownType == type && c.Player.m_SteamID == player.m_SteamID && data.SequenceEqual(c.Parameters));
        return cooldown != null;
    }

    public bool HasCooldownNoStateCheck(WarfarePlayer player, CooldownType type, out Cooldown cooldown)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Timeleft.Ticks <= 0);
        cooldown = Cooldowns.Find(c => c.CooldownType == type && c.Player.m_SteamID == player.Steam64.m_SteamID);
        return cooldown != null;
    }
    public void RemoveCooldown(WarfarePlayer player, CooldownType type)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Timeleft.Ticks <= 0 || c.Player.m_SteamID == player.Steam64.m_SteamID && c.CooldownType == type);
    }
    public void RemoveCooldown(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Timeleft.Ticks <= 0 || c.Player.m_SteamID == player.Steam64.m_SteamID);
    }
    public void RemoveCooldown(CooldownType type)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(x => x.Timeleft.Ticks <= 0 || x.CooldownType == type);
    }

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public float GetFOBDeployCooldown() => GetFOBDeployCooldown(_playerService.OnlinePlayers.Count(x => x.Team.IsValid));

    /// <returns>
    /// The deploy cooldown based on current player count.
    /// </returns>
    /// <remarks>Equation: <c>CooldownMin + (CooldownMax - CooldownMin) * (1 - Pow(1 - (PlayerCount - PlayersMin) * (1 / (PlayersMax - PlayersMin)), Alpha)</c>.</remarks>
    public float GetFOBDeployCooldown(int players)
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
public class CooldownConfig
{
    public float DeployFOBCooldownMin { get; set; }
    public float DeployFOBCooldownMax { get; set; }
    public int DeployFOBPlayersMin { get; set; }
    public int DeployFOBPlayersMax { get; set; }
    public float DeployFOBCooldownAlpha { get; set; }
    public float RequestKitCooldown { get; set; }
    public float RequestVehicleCooldown { get; set; }
    public float ReviveXPCooldown { get; set; }
    public float GlobalTraitCooldown { get; set; }
    public void SetDefaults()
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
public class Cooldown(CSteamID player, CooldownType cooldownType, float duration, params object[] cooldownParameters) : ITranslationArgument
{
    public CSteamID Player { get; } = player;
    public CooldownType CooldownType { get; } = cooldownType;
    public double TimeAdded { get; set; } = Time.realtimeSinceStartupAsDouble;
    public float Duration { get; set; } = duration;
    public object[] Parameters { get; } = cooldownParameters;
    public TimeSpan Timeleft => TimeSpan.FromSeconds(Math.Max(0d, Duration - (Time.realtimeSinceStartupAsDouble - TimeAdded)));
    public float SecondsLeft => Mathf.Max(0f, Duration - (Time.realtimeSinceStartup - (float)TimeAdded));
    public override string ToString()
    {
        double sec = Duration - (Time.realtimeSinceStartupAsDouble - TimeAdded);

        if (sec <= 1d) return "1s";

        string line = string.Empty;

        int i1 = (int)sec / 3600;
        if (i1 > 0)
            line += i1.ToString(CultureInfo.InvariantCulture) + "h ";
        sec -= i1 * 3600;

        i1 = (int)sec / 60;
        if (i1 > 0)
            line += i1.ToString(CultureInfo.InvariantCulture) + "m ";
        sec -= i1 * 60;

        i1 = (int)sec;
        if (i1 > 0)
            return line + i1.ToString(CultureInfo.InvariantCulture) + "s";
        if (line.Length == 0)
            return sec.ToString("F0", CultureInfo.InvariantCulture) + "s";
        return line;
    }

    /// <summary>Translated <see cref="ECooldownType"/>.</summary>
    public static readonly SpecialFormat FormatName = new SpecialFormat("Type (" + nameof(Players.CooldownType) + ")", "n");

    /// <summary>3 hours and 4 minutes</summary>
    public static readonly SpecialFormat FormatTimeLong = new SpecialFormat("Long Time", "tl1");

    /// <summary>3h 4m 20s</summary>
    public static readonly SpecialFormat FormatTimeShort = new SpecialFormat("Short Time (3h 40m)", "tl2");
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string? format = parameters.Format.Format;
        if (string.IsNullOrEmpty(format) || FormatTimeShort.Match(in parameters))
            return ToString();

        if (FormatName.Match(in parameters))
            return formatter.FormatEnum(CooldownType, parameters.Language);

        if (FormatTimeLong.Match(in parameters))
            return TimeAddon.ToLongTimeString(
                formatter.ServiceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value,
                (int)Timeleft.TotalSeconds,
                parameters.Language
            );

        return Timeleft.ToString(format);

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
    [Translatable("Execute Isolated Command")]
    IsolatedCommand,
    [Translatable("Interact Vehicle Seats")]
    InteractVehicleSeats
}
