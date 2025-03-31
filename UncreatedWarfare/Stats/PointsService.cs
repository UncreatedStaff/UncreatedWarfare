using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Stats;

public class PointsService : IEventListener<PlayerTeamChanged> // todo player equipment changed
{
    private readonly PointsConfiguration _configuration;
    private readonly WarfareModule _module;
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

    public Color32 CreditsColor => HexStringHelper.TryParseColor32(_configuration["CreditsColor"], CultureInfo.InvariantCulture, out Color32 color)
                                       ? color with { a = 255 }
                                       : new Color32(184, 255, 193, 255);

    public Color32 ExperienceColor => HexStringHelper.TryParseColor32(_configuration["ExperienceColor"], CultureInfo.InvariantCulture, out Color32 color)
                                          ? color with { a = 255 }
                                          : new Color32(244, 205, 87, 255);


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
        PointsUI ui,
        WarfareModule module)
    {
        _translations = translations.Value;
        _configuration = configuration;
        _pointsSql = pointsSql;
        _playerService = playerService;
        _logger = logger;
        _ui = ui;
        _module = module;
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
            if (rank.CumulativeExperience > experience)
                return rank.Previous ?? rank;
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

        string noDot = name.Replace(".", string.Empty).Replace(" ", string.Empty);
        for (WarfareRank? rank = _startingRank; rank != null; rank = rank.Next)
        {
            if (rank.Abbreviation.Replace(".", string.Empty).Equals(noDot, StringComparison.InvariantCultureIgnoreCase))
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
    /// Get an event meant for credit purchases.
    /// </summary>
    /// <param name="credits">The number of credits to remove. This should not be negative.</param>
    public ResolvedEventInfo GetPurchaseEvent(WarfarePlayer player, double credits)
    {
        EventInfo purchase = GetEvent("Purchase");
        return new ResolvedEventInfo(in purchase, null, -credits, null)
            .WithTranslation(_translations.XPToastPurchase, player);
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
    public Task ApplyEvent(WarfarePlayer player, ResolvedEventInfo @event, CancellationToken token = default)
    {
        return ApplyEvent(player.Steam64, player.Team.Faction.PrimaryKey, WarfareModule.Season, @event, token);
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

        if (factionId == 0)
        {
            xp = 0;
            credits = 0;
            if (rep == 0)
                return;
        }

        // todo XP boosts

        // only for display it's fine to use cached points
        PlayerPoints oldPoints = default, newPoints = default;

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(playerId);
        double oldRep = player?.CachedReputation ?? double.NaN;
        if (double.IsNaN(oldRep))
        {
            oldRep = await _pointsSql.GetReputationAsync(playerId, token).ConfigureAwait(false);
            if (player != null)
                player.CachedReputation = oldRep;
        }

        if (xp != 0 || credits != 0)
        {
            if (player is { CachedPoints.WasFound: true })
            {
                oldPoints = player.CachedPoints;
            }
            else
            {
                oldPoints = await _pointsSql.GetPointsAsync(playerId, factionId, season, token).ConfigureAwait(false);
            }

            newPoints = await _pointsSql.AddToPointsAsync(playerId, factionId, season, xp, credits, token).ConfigureAwait(false);

            xp = newPoints.XP - oldPoints.XP;
            credits = newPoints.Credits - oldPoints.Credits;
        }
        else
        {
            xp = 0;
            credits = 0;
        }

        double newRep;
        if (rep != 0)
        {
            newRep = await _pointsSql.AddToReputationAsync(playerId, rep, CancellationToken.None).ConfigureAwait(false);
            if (player != null)
                player.CachedReputation = newRep;
            oldRep = newRep - rep;
        }
        else
        {
            newRep = oldRep;
            rep = 0;
        }

        if (newPoints.WasFound)
        {
            _logger.LogInformation("Applied event {0}. XP: {1} -> {2} ({3}), Credits: {4} -> {5} ({6}). Reputation: {7} -> {8} ({9}). Faction: {10}, Season: {11}.",
                @event.EventName,
                oldPoints.XP,
                newPoints.XP,
                xp,
                oldPoints.Credits,
                newPoints.Credits,
                credits,
                oldRep,
                newRep,
                rep,
                factionId,
                season
            );

            if (newPoints.XP != oldPoints.XP)
                ActionLog.Add(ActionLogType.XPChanged, $"{oldPoints.XP} -> {newPoints.XP} | Event: '{@event.EventName}'", playerId);
            if (newPoints.Credits != oldPoints.Credits)
                ActionLog.Add(ActionLogType.CreditsChanged, $"{oldPoints.Credits} -> {newPoints.Credits} | Event: '{@event.EventName}'", playerId);
        }
        else if (!double.IsNaN(oldRep))
        {
            _logger.LogInformation("Applied event {0}. Reputation: {1} -> {2} ({3}). Season: {4}.",
                @event.EventName,
                oldRep,
                newRep,
                rep,
                season
            );
        }
        else
        {
            _logger.LogInformation("Applied event {0}. No changes. Season: {1}.",
                @event.EventName,
                season
            );
        }

        if (oldRep != rep)
            ActionLog.Add(ActionLogType.ReputationChanged, $"{oldRep} -> {rep} | Event: '{@event.EventName}'", playerId);

        await UniTask.SwitchToMainThread(CancellationToken.None);

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
                string numberTxt = (credits <= 0
                    ? @event.IsPurchase
                        ? _translations.XPToastPurchaseWithCredits
                        : _translations.XPToastLoseCredits
                    : _translations.XPToastGainCredits)
                    .Translate(Math.Abs(credits), player);

                string text = !string.IsNullOrWhiteSpace(@event.Message)
                    ? numberTxt + "\n" + TranslationFormattingUtility.Colorize(@event.Message, MessageColor)
                    : numberTxt;

                player.SendToast(new ToastMessage(ToastMessageStyle.Mini, text));
            }
        }

        // update UI
        if (player is { IsOnline: true })
        {
            if (newPoints.WasFound)
                player.CachedPoints = newPoints;
            if (!double.IsNaN(newRep))
                player.SetReputation((int)Math.Round(newRep));

            if (xp != 0 || credits != 0)
                _ui.UpdatePointsUI(player);

            if (!@event.ExcludeFromLeaderboard)
            {
                if (factionId == player.Team.Faction.PrimaryKey)
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
                else if (_module.IsLayoutActive())
                {
                    Layout layout = _module.GetActiveLayout();
                    uint fId2 = factionId;
                    Team? team = layout.TeamManager.AllTeams.FirstOrDefault(x => x.Faction.PrimaryKey == fId2);
                    LeaderboardPhase? phase = layout.Phases.OfType<LeaderboardPhase>().FirstOrDefault();
                    if (phase != null && team != null)
                    {
                        if (credits != 0)
                            phase.AddToOfflineStat(phase.GetStatIndex(KnownStatNames.Credits), credits, playerId, team);
                        if (rep != 0)
                            phase.AddToOfflineStat(phase.GetStatIndex(KnownStatNames.Reputation), rep, playerId, team);
                        if (xp != 0)
                            phase.AddToOfflineStat(phase.GetStatIndex(KnownStatNames.XP), xp, playerId, team);
                    }
                }
            }

            if (newPoints.WasFound)
            {
                WarfareRank oldRank = GetRankFromExperience(oldPoints.XP);
                WarfareRank newRank = GetRankFromExperience(newPoints.XP);
                if (oldRank != newRank)
                {
                    string msg = (newRank.Level < oldRank.Level ? _translations.ToastDemoted : _translations.ToastPromoted).Translate(newRank, player);
                    player.SendToast(new ToastMessage(ToastMessageStyle.Medium, msg));
                }
            }
        }
    }

    public void HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        _ui.UpdatePointsUI(e.Player);
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
    public bool IsPurchase { get; }
    public ResolvedEventInfo(in EventInfo @event) : this(@event, null, null, null) { }
    public ResolvedEventInfo(in EventInfo @event, double scaleFactor)
    : this(@event, @event.XP * scaleFactor, @event.Credits * scaleFactor, @event.Reputation * scaleFactor) { }
    public ResolvedEventInfo(in EventInfo @event, double? overrideXp, double? overrideCredits, double? overrideReputation)
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
        IsPurchase = c.GetValue("IsPurchase", false);
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
    public string? Name => Configuration?.Path;
    public double XP { get; }
    public double Credits { get; }
    public double Reputation { get; }

    public EventInfo(IConfigurationSection configuration)
    {
        Configuration = configuration;
        if (configuration == null)
            return;

        XP = configuration.GetValue("XP", 0d);
        Credits = ParsePercentageOrValueOfXP("Credits", 0.15);
        Reputation = ParsePercentageOrValueOfXP("Reputation", 0);
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


    public ResolvedEventInfo Resolve() => new ResolvedEventInfo(in this);
    public ResolvedEventInfo Resolve(double multiplier) => new ResolvedEventInfo(in this, multiplier);
    public ResolvedEventInfo Resolve(double? overrideXp = null, double? overrideCredits = null, double? overrideReputation = null)
    {
        return new ResolvedEventInfo(in this, overrideXp, overrideCredits, overrideReputation);
    }

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
    public readonly Translation<double> XPToastGainCredits = new Translation<double>("+<color=#b8ffc1>C</color> {0}", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastPurchaseWithCredits = new Translation<double>("-<color=#b8ffc1>C</color> {0}", TranslationOptions.TMProUI, "F0");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<double> XPToastLoseCredits = new Translation<double>("-{0} <color=#d69898>C</color>", TranslationOptions.TMProUI, "F0");

    [TranslationData("Sent to a player when they move up to the next level.")]
    public readonly Translation<WarfareRank> ToastPromoted = new Translation<WarfareRank>("YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO {0}", TranslationOptions.TMProUI, WarfareRank.FormatName);

    [TranslationData("Sent to a player when they move down to the previous level.")]
    public readonly Translation<WarfareRank> ToastDemoted = new Translation<WarfareRank>("YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO {0}", TranslationOptions.TMProUI, WarfareRank.FormatName);

    [TranslationData("Sent to a player on the points popup when XP or credits given from the console.")]
    public Translation XPToastFromOperator = new Translation("FROM OPERATOR", TranslationOptions.TMProUI);
    
    [TranslationData("Sent to a player on the points popup when a purchase is made.")]
    public Translation XPToastPurchase = new Translation("PURCHASE", TranslationOptions.TMProUI);
    
    [TranslationData("Sent to a player after they're given a quest reward.", "Quest name")]
    public Translation<string> XPToastQuestReward = new Translation<string>("{0} REWARD", TranslationOptions.TMProUI, UppercaseAddon.Instance);

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

    [TranslationData("Hint to tell a player to load supplies into the vehicle.")]
    public Translation FOBResourceToastLoadSupplies = new Translation("LOAD SUPPLIES");

    [TranslationData("Hint to tell a player to rearm a vehicle.")]
    public Translation FOBResourceToastRearmVehicle = new Translation("REARM VEHICLE");

    [TranslationData("Sent to a player on the points popup after they rearm a vehicle.")]
    public Translation FOBResourceToastRearmPlayer = new Translation("REARM");

    [TranslationData("Sent to a player on the points popup after they repair a vehicle.")]
    public Translation FOBResourceToastRepairVehicle = new Translation("REPAIR");

}