using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.Unlocks;

public class LevelUnlockRequirement : UnlockRequirement
{
    [JsonIgnore] private PointsService? _pointsService;
    [JsonIgnore] private KitSignTranslations? _kitSignTranslations;
    [JsonIgnore] private RequestKitsTranslations? _requestKitsTranslations;
    [JsonIgnore] private RequestVehicleTranslations? _requestVehicleTranslations;

    [JsonPropertyName("unlock_level")]
    public int UnlockLevel { get; set; } = -1;

    /// <inheritdoc />
    public override void Initialize(IServiceProvider serviceProvider)
    {
        _pointsService = serviceProvider.GetRequiredService<PointsService>();
        _kitSignTranslations = serviceProvider.GetRequiredService<TranslationInjection<KitSignTranslations>>().Value;
        _requestKitsTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestKitsTranslations>>().Value;
        _requestVehicleTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestVehicleTranslations>>().Value;
        base.Initialize(serviceProvider);
    }

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        if (_pointsService == null)
            throw new InvalidOperationException("Not initialized.");

        WarfareRank rank = _pointsService.GetRankFromExperience(player.CachedPoints.XP);
        return rank.Level >= UnlockLevel;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer? player, LanguageInfo language, CultureInfo culture)
    {
        if (_kitSignTranslations == null || _pointsService == null)
            throw new InvalidOperationException("Not initialized.");

        WarfareRank? rank = _pointsService.GetRankFromLevel(UnlockLevel);
        if (rank == null)
            return $"ERROR UNKNOWN RANK {UnlockLevel}";

        return player == null
            ? _kitSignTranslations.KitLevelCost.Translate(rank, language, culture, TimeZoneInfo.Utc)
            : _kitSignTranslations.KitLevelCost.Translate(rank, player);
    }

    /// <inheritdoc />
    protected override void ReadLegacyProperty(ILogger? logger, ref Utf8JsonReader reader, string property)
    {
        if (!property.Equals("unlock_level", StringComparison.OrdinalIgnoreCase))
            return;

        if (reader.TryGetInt32(out int unlockRank))
            UnlockLevel = unlockRank;
    }

    /// <inheritdoc />
    public override object Clone()
    {
        return new LevelUnlockRequirement
        {
            _pointsService = _pointsService,
            _kitSignTranslations = _kitSignTranslations,
            _requestKitsTranslations = _requestKitsTranslations,
            _requestVehicleTranslations = _requestVehicleTranslations,
            UnlockLevel = UnlockLevel
        };
    }

    /// <inheritdoc />
    public override Exception RequestFailureToMeet(CommandContext ctx, IRequestable<object> requestable)
    {
        if (_pointsService == null)
            throw new InvalidOperationException("Not initialized.");

        WarfareRank? rank = _pointsService.GetRankFromLevel(UnlockLevel);
        if (rank == null)
            return ctx.ReplyString($"Not level {UnlockLevel} (unknown rank tell devs).");

        return requestable switch
        {
            Kit => _requestKitsTranslations == null
                ? throw new InvalidOperationException("Not initialized.")
                : ctx.Reply(_requestKitsTranslations.RequestNotLevel, rank),

            VehicleSpawner => _requestVehicleTranslations == null
                ? throw new InvalidOperationException("Not initialized.")
                : ctx.Reply(_requestVehicleTranslations.RequestNotLevel, rank),

            _ => ctx.ReplyString($"Unhandled unlock level, tell devs. You need level {rank.Name} to use this object.")
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is LevelUnlockRequirement r && r.UnlockLevel == UnlockLevel;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return UnlockLevel;
    }
}