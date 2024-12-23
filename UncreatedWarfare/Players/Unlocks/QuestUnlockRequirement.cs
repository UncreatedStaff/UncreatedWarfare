using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Players.Unlocks;

public class QuestUnlockRequirement : UnlockRequirement, IEquatable<QuestUnlockRequirement>
{
    private readonly RequestTranslations _reqTranslations;
    public Guid QuestId { get; set; }
    public Guid[] UnlockPresets { get; set; } = Array.Empty<Guid>();

    public QuestUnlockRequirement(TranslationInjection<RequestTranslations> reqTranslations)
    {
        _reqTranslations = reqTranslations.Value;
    }

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            // todo if (!player.QuestComplete(UnlockPresets[i]))
            //     return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer? player, LanguageInfo language, CultureInfo culture)
    {
        bool access = CanAccessFast(player);
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
                logger.LogError("Failed to convert {0} with value \"{1}\" to a GUID in quest unlock requirement.", property, (reader.GetString() ?? "null"));
            }
            else
            {
                QuestId = questID;
            }
        }
    }

    /// <inheritdoc />
    protected override bool ReadFromJson(ILogger? logger, ref Utf8JsonReader reader)
    {
        bool read = false;
        JsonUtility.ReadTopLevelProperties(ref reader, ref read, (ref Utf8JsonReader reader, string property, ref bool read) =>
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
                if (reader.TryGetGuid(out Guid questID))
                    QuestId = questID;
            }

            read = true;
        });

        return read;
    }

    /// <inheritdoc />
    protected override void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("unlock_presets");
        writer.WriteStartArray();
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            writer.WriteStringValue(UnlockPresets[i]);
        }
        writer.WriteEndArray();

        writer.WriteString("quest_id", QuestId);
    }

    /// <inheritdoc />
    public override object Clone()
    {
        QuestUnlockRequirement req = new QuestUnlockRequirement(new TranslationInjection<RequestTranslations>(_reqTranslations))
        {
            QuestId = QuestId,
            UnlockPresets = new Guid[UnlockPresets.Length]
        };
        Array.Copy(UnlockPresets, req.UnlockPresets, UnlockPresets.Length);
        return req;
    }

    /// <inheritdoc />
    public override Exception RequestKitFailureToMeet(CommandContext ctx, Kit kit)
    {
        if (Assets.find(QuestId) is not QuestAsset asset)
        {
            return ctx.Reply(_reqTranslations.RequestKitQuestIncomplete, null!);
        }

        // todo QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(_reqTranslations.RequestKitQuestIncomplete, asset);
    }

    /// <inheritdoc />
    public override Exception RequestVehicleFailureToMeet(CommandContext ctx, WarfareVehicleInfo data)
    {
        if (Assets.find(QuestId) is not QuestAsset asset)
        {
            return ctx.Reply(_reqTranslations.RequestVehicleQuestIncomplete, null!);
        }

        // todo QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(_reqTranslations.RequestVehicleQuestIncomplete, asset);
    }

#if false
    /// <inheritdoc />
    public override Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        if (Assets.find(QuestId) is not QuestAsset asset)
        {
            return ctx.Reply(T.RequestTraitQuestIncomplete, trait, null!);
        }

        // todo QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(T.RequestTraitQuestIncomplete, trait, asset);
    }
#endif

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