using Microsoft.Extensions.Logging;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

public class QuestUnlockRequirement : UnlockRequirement, IEquatable<QuestUnlockRequirement>
{
    public Guid QuestId { get; set; }
    public Guid[] UnlockPresets { get; set; } = Array.Empty<Guid>();

    /// <inheritdoc />
    public override bool CanAccessFast(UCPlayer player)
    {
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            if (!player.QuestComplete(UnlockPresets[i]))
                return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override string GetSignText(UCPlayer player)
    {
        bool access = CanAccessFast(player);
        if (access)
            return T.KitRequiredQuestsComplete.Translate(player);
        if (Assets.find(QuestId) is QuestAsset quest)
            return T.KitRequiredQuest.Translate(player, false, quest, UCWarfare.GetColor("kit_level_unavailable"));

        return T.KitRequiredQuestsMultiple.Translate(player, false, UnlockPresets.Length, UCWarfare.GetColor("kit_level_unavailable"), UnlockPresets.Length == 1 ? string.Empty : "S");
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
        QuestUnlockRequirement req = new QuestUnlockRequirement
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
            return ctx.Reply(T.RequestKitQuestIncomplete, null!);
        }

        QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(T.RequestKitQuestIncomplete, asset);
    }

    /// <inheritdoc />
    public override Exception RequestVehicleFailureToMeet(CommandContext ctx, VehicleData data)
    {
        if (Assets.find(QuestId) is not QuestAsset asset)
        {
            return ctx.Reply(T.RequestVehicleQuestIncomplete, null!);
        }

        QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(T.RequestVehicleQuestIncomplete, asset);
    }

    /// <inheritdoc />
    public override Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        if (Assets.find(QuestId) is not QuestAsset asset)
        {
            return ctx.Reply(T.RequestTraitQuestIncomplete, trait, null!);
        }

        QuestManager.TryAddQuest(ctx.Player, asset);
        return ctx.Reply(T.RequestTraitQuestIncomplete, trait, asset);
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