using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare;

public class CooldownManager : IHostedService
{
    internal List<Cooldown> Cooldowns;
    internal CooldownConfig Config = new CooldownConfig();

    private readonly IPlayerService _playerService;

    public CooldownManager(IPlayerService playerService)
    {
        _playerService = playerService;
    }
    public UniTask StartAsync(CancellationToken token)
    {
        Config.SetDefaults();
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    // todo
    private void OnGameStarting()
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(x => x.CooldownType is not CooldownType.Report);
    }

    /// <exception cref="SingletonUnloadedException"/>
    public void StartCooldown(WarfarePlayer player, CooldownType type, float seconds, params object[] data)
    {
        GameThread.AssertCurrent();

        if (seconds <= 0f) return;

        if (HasCooldown(player, type, out Cooldown existing, data))
            existing.TimeAdded = Time.realtimeSinceStartup;
        else
            Cooldowns.Add(new Cooldown(player, type, seconds, data));
    }
    /// <exception cref="SingletonUnloadedException"/>
    public bool HasCooldown(WarfarePlayer player, CooldownType type, out Cooldown cooldown, params object[] data)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Player == null || c.SecondsLeft <= 0f);
        cooldown = Cooldowns.Find(c => c.CooldownType == type && c.Player.Steam64 == player.Steam64 && StatesEqual(data, c.Parameters));
        return cooldown != null;
    }
    private bool StatesEqual(object[] state1, object[] state2)
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
    public bool HasCooldownNoStateCheck(UCPlayer player, CooldownType type, out Cooldown cooldown)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Player == null || c.Timeleft.TotalSeconds <= 0);
        cooldown = Cooldowns.Find(c => c.CooldownType == type && c.Player.Steam64 == player.CSteamID);
        return cooldown != null;
    }
    public void RemoveCooldown(UCPlayer player, CooldownType type)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Player == null || c.Player.Steam64 == player.CSteamID && c.CooldownType == type);
    }
    public void RemoveCooldown(UCPlayer player)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(c => c.Player.Steam64 == player.CSteamID);
    }
    public void RemoveCooldown(CooldownType type)
    {
        GameThread.AssertCurrent();

        Cooldowns.RemoveAll(x => x.Player == null || x.CooldownType == type);
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
public class CooldownConfig : JSONConfigData
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
public class Cooldown(WarfarePlayer player, CooldownType cooldownType, float duration, params object[] cooldownParameters) : ITranslationArgument
{
    public WarfarePlayer Player { get; } = player;
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

    /// <summary>Translated <see cref="ECooldownType"/>.</summary>
    public static readonly SpecialFormat FormatName = new SpecialFormat("Type (" + nameof(Warfare.CooldownType) + ")", "n");

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
            return TimeAddon.ToLongTimeString((int)Timeleft.TotalSeconds, parameters.Language);

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
