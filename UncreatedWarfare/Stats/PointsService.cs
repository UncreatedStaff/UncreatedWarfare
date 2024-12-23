using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Stats;
public class PointsService : IEventListener<PlayerTeamChanged> // todo player equipment changed
{
    private readonly PointsConfiguration _configuration;
    private readonly IPointsStore _pointsSql;
    private readonly IPlayerService _playerService;
    private readonly ILogger<PointsService> _logger;
    private readonly PointsUI _ui;
    private readonly IConfigurationSection _event;
    private readonly PointsTranslations _translations;
    private readonly WarfareRank _startingRank;
    private readonly WarfareRank[] _ranks;

    private static readonly Color32 MessageColor = new Color32(173, 173, 173, 255);

    public double DefaultCredits => _configuration.GetValue<double>("DefaultCredits");
    public double GlobalMultiplier => _configuration.GetValue("GlobalPointMultiplier", 1d);


    /// <summary>
    /// Ordered list of all configured ranks.
    /// </summary>
    public IReadOnlyList<WarfareRank> Ranks { get; }

    public PointsService(
        PointsConfiguration configuration,
        IPointsStore pointsSql,
        IPlayerService playerService,
        TranslationInjection<PointsTranslations> translations,
        ILogger<PointsService> logger,
        PointsUI ui)
    {
        _translations = translations.Value;
        _configuration = configuration;
        _pointsSql = pointsSql;
        _playerService = playerService;
        _logger = logger;
        _ui = ui;
        _event = configuration.GetSection("Events");
        IConfigurationSection levelsSection = configuration.GetSection("Levels");

        int ct = 0;
        using (IEnumerator<IConfigurationSection> enumerator = levelsSection.GetChildren().GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("There must be at least one level defined in config.");

            // the value of Next is initialized in this constructor and it creates a linked list kind of structure of ranks
            _startingRank = new WarfareRank(null, enumerator, 0, ref ct);
        }

        _ranks = new WarfareRank[ct];

        ct = -1;
        for (WarfareRank? rank = _startingRank; rank != null; rank = rank.Next)
        {
            _ranks[++ct] = rank;
        }

        Ranks = new ReadOnlyCollection<WarfareRank>(_ranks);
    }

    /// <summary>
    /// Find rank info from a one-based level.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if a value less than or equal to 0 is inputted for <paramref name="level"/>.</exception>
    public WarfareRank? GetRankFromLevel(int level)
    {
        --level;

        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level));

        return level >= _ranks.Length ? null : _ranks[level];
    }

    /// <summary>
    /// Find rank info from an experience value.
    /// </summary>
    public WarfareRank GetRankFromExperience(double experience)
    {
        if (experience < 0)
            return _startingRank;

        for (WarfareRank? rank = _startingRank; rank != null; rank = rank.Next)
        {
            if (rank.CumulativeExperience <= experience)
                return rank;
        }

        return _ranks[^1];
    }

    /// <summary>
    /// Find rank info from its name, then its abbreviation.
    /// </summary>
    public WarfareRank? GetRankByName(string name)
    {
        for (WarfareRank? rank = _startingRank; rank != null; rank = rank.Next)
        {
            if (rank.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return rank;
        }

        for (WarfareRank? rank = _startingRank; rank != null; rank = rank.Next)
        {
            if (rank.Name.Replace(".", string.Empty).Equals(name.Replace(".", string.Empty), StringComparison.InvariantCultureIgnoreCase))
                return rank;
        }

        return null;
    }

    /// <summary>
    /// Get an event meant for admin commands. Adds raw point values.
    /// </summary>
    public ResolvedEventInfo GetAdminEvent(in LanguageSet set, double? xp, double? credits, double? reputation)
    {
        return new ResolvedEventInfo(default, xp, credits, reputation)
            .WithTranslation(_translations.XPToastFromOperator, set);
    }

    /// <summary>
    /// Get an event from settings.
    /// </summary>
    public EventInfo GetEvent(string eventId)
    {
        return new EventInfo(_event.GetSection(eventId));
    }

    /// <summary>
    /// Apply an event and trigger updates for the necessary UI for the current season.
    /// </summary>
    public Task ApplyEvent(CSteamID playerId, uint factionId, ResolvedEventInfo @event, CancellationToken token = default)
    {
        return ApplyEvent(playerId, factionId, WarfareModule.Season, @event, token);
    }

    /// <summary>
    /// Apply an event and trigger updates for the necessary UI.
    /// </summary>
    public async Task ApplyEvent(CSteamID playerId, uint factionId, int season, ResolvedEventInfo @event, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        bool hideToast = @event.HideToast || @event.Message == null;

        double xp = @event.XP;
        double rep = @event.Reputation;
        double credits = @event.Credits;

        if (!@event.IgnoresGlobalMultiplier)
        {
            double mod = GlobalMultiplier;
            xp *= mod;
            credits *= mod;
        }

        // todo XP boosts

        // only for display it's fine to use cached points
        PlayerPoints oldPoints;
        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(playerId);
        if (player is { CachedPoints.WasFound: true })
        {
            oldPoints = player.CachedPoints;
        }
        else
        {
            oldPoints = await _pointsSql.GetPointsAsync(playerId, factionId, season, token).ConfigureAwait(false);
        }


        PlayerPoints newPoints = await _pointsSql.AddToPointsAsync(playerId, factionId, season, xp, credits, token).ConfigureAwait(false);

        double newRep = await _pointsSql.AddToReputationAsync(playerId, rep, token).ConfigureAwait(false);

        _logger.LogConditional("Applied event {0}. XP: {1} -> {2}, Credits: {3} -> {4}. Reputation: {5}. Faction: {6}, Season: {7}.",
            @event.EventName,
            oldPoints.XP,
            newPoints.XP,
            oldPoints.Credits,
            newPoints.Credits,
            newRep,
            factionId,
            season
        );

        await UniTask.SwitchToMainThread(token);

        player = _playerService.GetOnlinePlayerOrNull(playerId);

        if (player is { IsOnline: true } && !hideToast)
        {
            // XP toast
            if (xp != 0)
            {
                string numberTxt = (xp > 0 ? _translations.XPToastGainXP : _translations.XPToastLoseXP).Translate(Math.Abs(xp), player);

                string text = !string.IsNullOrWhiteSpace(@event.Message)
                    ? numberTxt + "\n" + TranslationFormattingUtility.Colorize(@event.Message, MessageColor)
                    : numberTxt;

                player.SendToast(new ToastMessage(ToastMessageStyle.Mini, text));
            }

            // credits toast
            if (credits != 0)
            {
                string numberTxt = (credits > 0 ? _translations.XPToastGainCredits : _translations.XPToastLoseCredits).Translate(Math.Abs(credits), player);

                string text = !string.IsNullOrWhiteSpace(@event.Message)
                    ? numberTxt + "\n" + TranslationFormattingUtility.Colorize(@event.Message, MessageColor)
                    : numberTxt;

                player.SendToast(new ToastMessage(ToastMessageStyle.Mini, text));
            }
        }

        // update UI
        if (player is { IsOnline: true })
        {
            player.CachedPoints = newPoints;
            player.AddReputation((int)Math.Round(rep));
            _ui.UpdatePointsUI(player, this);

            if (!@event.ExcludeFromLeaderboard)
            {
                PlayerGameStatsComponent? comp = player.ComponentOrNull<PlayerGameStatsComponent>();
                if (comp != null)
                {
                    if (credits != 0)
                        comp.AddToStat(KnownStatNames.Credits, credits);
                    if (rep != 0)
                        comp.AddToStat(KnownStatNames.Reputation, rep);
                    if (xp != 0)
                        comp.AddToStat(KnownStatNames.XP, xp);
                }
            }
        }

        // todo 'promoted'/'demoted' message
    }

    public void HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        _ui.UpdatePointsUI(e.Player, this);
    }
}

public class ResolvedEventInfo
{
    public double XP { get; }
    public double Credits { get; }
    public double Reputation { get; }
    public bool HideToast { get; }
    public bool ExcludeFromLeaderboard { get; }
    public bool IgnoresBoosts { get; }
    public bool IgnoresGlobalMultiplier { get; }
    public string? Message { get; set; }
    public string? EventName { get; set; }
    public ResolvedEventInfo(EventInfo @event) : this(@event, null, null, null) { }
    public ResolvedEventInfo(EventInfo @event, double scaleFactor)
    : this(@event, @event.XP * scaleFactor, @event.Credits * scaleFactor, @event.Reputation * scaleFactor) { }
    public ResolvedEventInfo(EventInfo @event, double? overrideXp, double? overrideCredits, double? overrideReputation)
    {
        XP = overrideXp ?? @event.XP;
        Credits = overrideCredits ?? @event.Credits;
        Reputation = overrideReputation ?? @event.Reputation;
        EventName = @event.Name;

        IConfiguration? c = @event.Configuration;
        if (c == null)
            return;

        HideToast = c.GetValue("HideToast", false);
        ExcludeFromLeaderboard = c.GetValue("ExcludeFromLeaderboard", false);
        IgnoresBoosts = c.GetValue("IgnoresBoosts", false);
        IgnoresGlobalMultiplier = c.GetValue("IgnoresGlobalMultiplier", false);
    }

    public ResolvedEventInfo WithTranslation(Translation translation, WarfarePlayer? player)
    {
        Message = player == null ? translation.Translate() : translation.Translate(player);
        return this;
    }

    public ResolvedEventInfo WithTranslation(Translation translation, in LanguageSet set)
    {
        Message = translation.Translate(in set);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0>(Translation<T0> translation, T0 arg0, WarfarePlayer? player)
    {
        Message = player == null ? translation.Translate(arg0) : translation.Translate(arg0, player);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0>(Translation<T0> translation, T0 arg0, in LanguageSet set)
    {
        Message = translation.Translate(arg0, in set);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? player)
    {
        Message = player == null ? translation.Translate(arg0, arg1) : translation.Translate(arg0, arg1, player);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1, in LanguageSet set)
    {
        Message = translation.Translate(arg0, arg1, in set);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? player)
    {
        Message = player == null ? translation.Translate(arg0, arg1, arg2) : translation.Translate(arg0, arg1, arg2, player);
        return this;
    }

    public ResolvedEventInfo WithTranslation<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, in LanguageSet set)
    {
        Message = translation.Translate(arg0, arg1, arg2, in set);
        return this;
    }
}

public readonly struct EventInfo
{   
    public IConfigurationSection Configuration { get; }
    public string? Name => Configuration?.Key;
    public double XP => Configuration?.GetValue("XP", 0d) ?? 0d;
    public double Credits => ParsePercentageOrValueOfXP("Credits", 0.15);
    public double Reputation => ParsePercentageOrValueOfXP("Reputation", 0);
    public EventInfo(IConfigurationSection configuration)
    {
        Configuration = configuration;
    }

    private double ParsePercentageOrValueOfXP(string key, double defaultPercentage)
    {
        if (Configuration == null)
            return 0d;

        string? valueStr = Configuration[key];
        if (string.IsNullOrWhiteSpace(valueStr))
        {
            return XP * defaultPercentage;
        }

        ReadOnlySpan<char> span = valueStr.Trim();
        bool percent = span.Length != 0 && span[^1] == '%';

        double value = double.Parse(percent ? span[..^1] : span, NumberStyles.Number, CultureInfo.InvariantCulture);
        return percent ? value / 100 * XP : value;
    }


    public ResolvedEventInfo Resolve() => new ResolvedEventInfo(this);

    public static implicit operator ResolvedEventInfo(EventInfo @event)
    {
        return @event.Resolve();
    }
}

public class PointsTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Points";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastGainXP = new Translation<double>("+{0} XP", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastLoseXP = new Translation<double>("-{0} XP", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastGainCredits = new Translation<double>("+{0} <color=#b8ffc1>C</color>", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastPurchaseCredits = new Translation<double>("-{0} <color=#b8ffc1>C</color>", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastLoseCredits = new Translation<double>("-{0} <color=#d69898>C</color>", TranslationOptions.TMProUI, "F0");

    [TranslationData("Sent to a player when they move up to the next level.")]
    public readonly Translation ToastPromoted = new Translation("YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player when they move down to the previous level.")]
    public readonly Translation ToastDemoted = new Translation("YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup when XP or credits given from the console.")]
    public Translation XPToastFromOperator = new Translation("FROM OPERATOR", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup when XP or credits are given to them by an admin.")]
    public Translation XPToastFromPlayer = new Translation("FROM ADMIN", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they heal their teammate.")]
    public Translation XPToastHealedTeammate = new Translation("HEALED TEAMMATE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they revive their injured teammate.")]
    public Translation XPToastRevivedTeammate = new Translation("REVIVED TEAMMATE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they injure an enemy.")]
    public Translation XPToastEnemyInjured = new Translation("<color=#e3e3e3>DOWNED</color>", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they injure their teammate.")]
    public Translation XPToastFriendlyInjured = new Translation("<color=#e3e3e3>DOWNED FRIENDLY</color>", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they kill an enemy.")]
    public Translation XPToastEnemyKilled = new Translation("KILLED ENEMY", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they damage an enemy that was later killed by someone else.")]
    public Translation XPToastKillAssist = new Translation("ASSIST", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they damage a vehicle that was later destroyed by someone else.")]
    public Translation XPToastKillVehicleAssist = new Translation("VEHICLE ASSIST", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they drive a vehicle with a gunner that killed an enemy.")]
    public Translation XPToastKillDriverAssist = new Translation("DRIVER ASSIST", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they spot enemy forces.")]
    public Translation XPToastSpotterAssist = new Translation("SPOTTER", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they kill a friendly.")]
    public Translation XPToastFriendlyKilled = new Translation("TEAMKILLED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they kill themselves.")]
    public Translation XPToastSuicide = new Translation("SUICIDE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help shovel up a FOB.")]
    public Translation XPToastFOBBuilt = new Translation("FOB BUILT", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help shovel up a Repair Station.")]
    public Translation XPToastRepairStationBuilt = new Translation("REPAIR STATION BUILT", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help shovel up a fortfication.")]
    public Translation XPToastFortificationBuilt = new Translation("FORTIFICATION BUILT", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help shovel up an Emplacement.")]
    public Translation XPToastEmplacementBuilt = new Translation("EMPLACEMENT BUILT", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a FOB.")]
    public Translation XPToastFOBDestroyed = new Translation("FOB DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a friendly FOB.")]
    public Translation XPToastFriendlyFOBDestroyed = new Translation("FRIENDLY FOB DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a bunker.")]
    public Translation XPToastBunkerDestroyed = new Translation("BUNKER DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a friendly bunker.")]
    public Translation XPToastFriendlyBunkerDestroyed = new Translation("FRIENDLY BUNKER DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after someone spawns in a FOB or bunker they placed.")]
    public Translation XPToastPlayerDeployToFob = new Translation("FOB IN USE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help resupply a FOB with building or ammo supplies.")]
    public Translation XPToastResuppliedFob = new Translation("RESUPPLIED FOB", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after a teammate retreives ammo from an ammo crate or ammo bag they placed.")]
    public Translation XPToastResuppliedTeammate = new Translation("RESUPPLIED TEAMMATE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after a repair station they placed repairs a vehicle.")]
    public Translation XPToastRepairedVehicle = new Translation("REPAIRED VEHICLE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after a FOB they placed helps repair a vehicle.")]
    public Translation XPToastFOBRepairedVehicle = new Translation("FOB REPAIRED VEHICLE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a ground vehicle.")]
    public Translation<VehicleType> XPToastVehicleDestroyed = new Translation<VehicleType>("{0} DESTROYED", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to a player on the points popup after they destroy an air vehicle.")]
    public Translation<VehicleType> XPToastAircraftDestroyed = new Translation<VehicleType>("{0} SHOT DOWN", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to a player on the points popup after they destroy a friendly ground vehicle.")]
    public Translation<VehicleType> XPToastFriendlyVehicleDestroyed = new Translation<VehicleType>("FRIENDLY {0} DESTROYED", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to a player on the points popup after they destroy a friendly air vehicle.")]
    public Translation<VehicleType> XPToastFriendlyAircraftDestroyed = new Translation<VehicleType>("FRIENDLY {0} SHOT DOWN", TranslationOptions.TMProUI, UppercaseAddon.Instance);

    [TranslationData("Sent to a player on the points popup after they help drive other players.")]
    public Translation XPToastTransportedPlayer = new Translation("TRANSPORTING PLAYERS", TranslationOptions.TMProUI);

    // todo description
    [TranslationData]
    public Translation XPToastAceArmorRefund = new Translation("ACE ARMOR SHARE", TranslationOptions.TMProUI);


    [TranslationData("Sent to a player on the points popup after they help capture a flag.")]
    public Translation XPToastFlagCaptured = new Translation("FLAG CAPTURED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they help take a flag from the other team.")]
    public Translation XPToastFlagNeutralized = new Translation("FLAG NEUTRALIZED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup while they're capturing or neutralizing a flag owned by the other team.")]
    public Translation XPToastFlagTickAttack = new Translation("ATTACK", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup while they're defending a flag they own.")]
    public Translation XPToastFlagTickDefend = new Translation("DEFENSE", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy an enemy cache in Insurgency.")]
    public Translation XPToastCacheDestroyed = new Translation("CACHE DESTROYED", TranslationOptions.TMProUI);

    [TranslationData("Sent to a player on the points popup after they destroy a friendly cache in Insurgency.")]
    public Translation XPToastFriendlyCacheDestroyed = new Translation("FRIENDLY CACHE DESTROYED", TranslationOptions.TMProUI);


    [TranslationData("Sent to a player on the points popup after they get XP from a squad bonus.")]
    public Translation XPToastSquadBonus = new Translation("SQUAD BONUS", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation XPToastOnDuty = new Translation("ON DUTY", TranslationOptions.TMProUI);


    [TranslationData(IsPriorityTranslation = false)]
    public Translation<int> FOBToastGainBuild = new Translation<int>("<color=#f3ce82>+{0} BUILD</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<int> FOBToastLoseBuild = new Translation<int>("<color=#f3ce82>-{0} BUILD</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<int> FOBToastGainAmmo = new Translation<int>("<color=#e25d5d>+{0} AMMO</color>", TranslationOptions.TMProUI);

    [TranslationData(IsPriorityTranslation = false)]
    public Translation<int> FOBToastLoseAmmo = new Translation<int>("<color=#e25d5d>-{0} AMMO</color>", TranslationOptions.TMProUI);


    [TranslationData("Hint to tell a player to load supplies into the vehicle.")]
    public Translation FOBResourceToastLoadSupplies = new Translation("LOAD SUPPLIES");

    [TranslationData("Hint to tell a player to rearm a vehicle.")]
    public Translation FOBResourceToastRearmVehicle = new Translation("REARM VEHICLE");

    [TranslationData("Sent to a player on the points popup after they rearm a vehicle.")]
    public Translation FOBResourceToastRearmPlayer = new Translation("REARM");

    [TranslationData("Sent to a player on the points popup after they repair a vehicle.")]
    public Translation FOBResourceToastRepairVehicle = new Translation("REPAIR");

}