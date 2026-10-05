using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.Unlocks;

public class QuestUnlockRequirement : UnlockRequirement, IEquatable<QuestUnlockRequirement>
{
    private RequestTranslations? _reqTranslations;
    // private QuestService? _questService;
    public Guid QuestId { get; set; }
    public Guid[] UnlockPresets { get; set; } = Array.Empty<Guid>();

    /// <inheritdoc />
    public override void Initialize(IServiceProvider serviceProvider)
    {
        base.Initialize(serviceProvider);
        _reqTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestTranslations>>().Value;
    }

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            // todo if (!_questService.HasCompletedQuest(UnlockPresets[i]))
            //     return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer? player, LanguageInfo language, CultureInfo culture)
    {
        bool access = player != null && CanAccessFast(player);
        //if (access)
        //    return T.KitRequiredQuestsComplete.Translate(player);
        // if (Assets.find(QuestId) is QuestAsset quest)
        //     return T.KitRequiredQuest.Translate(quest, UCWarfare.GetColor("kit_level_unavailable"), player);

        return "not implemented";/*KitRequiredQuestsMultiple.Translate(UnlockPresets.Length , UCWarfare.GetColor("kit_level_unavailable"), UnlockPresets.Length == 1 ? string.Empty : "S", player );*/
    }

    /// <inheritdoc />
    protected override void ReadLegacyProperty(ILogger? logger, ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_presets", StringComparison.OrdinalIgnoreCase))
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                return;

            List<Guid> ids = new List<Guid>(4);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TryGetGuid(out Guid guid) && !ids.Contains(guid))
                    ids.Add(guid);
            }

            UnlockPresets = ids.ToArray();
        }
        else if (property.Equals("quest_id", StringComparison.OrdinalIgnoreCase))
        {
            if (!reader.TryGetGuid(out Guid questID))
            {
                logger?.LogError("Failed to convert {0} with value \"{1}\" to a GUID in quest unlock requirement.", property, (reader.GetString() ?? "null"));
            }
            else
            {
                QuestId = questID;
            }
        }
    }

    /// <inheritdoc />
    public override object Clone()
    {
        QuestUnlockRequirement req = new QuestUnlockRequirement
        {
            _reqTranslations = _reqTranslations,
            QuestId = QuestId,
            UnlockPresets = new Guid[UnlockPresets.Length]
        };
        Array.Copy(UnlockPresets, req.UnlockPresets, UnlockPresets.Length);
        return req;
    }

    /// <inheritdoc />
    public override Exception RequestFailureToMeet(CommandContext ctx, IRequestable<object> requestable)
    {
        if (_reqTranslations == null)
            throw new InvalidOperationException("Not initialized.");

        QuestAsset? asset = Assets.find<QuestAsset>(QuestId);

        if (asset != null && ctx.Player != null)
        {
            QuestService.ServerTrackQuest(ctx.Player, asset);
        }

        return ctx.Reply(_reqTranslations.RequestKitQuestIncomplete, asset!);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is QuestUnlockRequirement r && Equals(r);
    }

    /// <inheritdoc />
    public bool Equals(QuestUnlockRequirement other)
    {
        if (other.QuestId != QuestId)
            return false;

        if (other.UnlockPresets is not { Length: > 0 } && UnlockPresets is not { Length: > 0 })
            return true;

        if (other.UnlockPresets.Length != UnlockPresets.Length)
            return false;

        for (int i = 0; i < UnlockPresets.Length; ++i)
        {
            if (other.UnlockPresets[i] != UnlockPresets[i])
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(QuestId, UnlockPresets.Length);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}