using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

[UnlockRequirement(3, "unlock_presets", "quest_id")]
public class QuestUnlockRequirement : UnlockRequirement
{
    public Guid QuestID;
    public Guid[] UnlockPresets = Array.Empty<Guid>();
    public override bool CanAccess(UCPlayer player)
    {
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            if (!player.QuestComplete(UnlockPresets[i]))
                return false;
        }
        return true;
    }
    public override string GetSignText(UCPlayer player)
    {
        bool access = CanAccess(player);
        if (access)
            return T.KitRequiredQuestsComplete.Translate(player);
        if (Assets.find(QuestID) is QuestAsset quest)
            return T.KitRequiredQuest.Translate(player, false, quest, UCWarfare.GetColor("kit_level_unavailable"));

        return T.KitRequiredQuestsMultiple.Translate(player, false, UnlockPresets.Length, UCWarfare.GetColor("kit_level_unavailable"), UnlockPresets.Length.S());
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_presets", StringComparison.OrdinalIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<Guid> ids = new List<Guid>(4);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TryGetGuid(out Guid guid) && !ids.Contains(guid))
                        ids.Add(guid);
                }
                UnlockPresets = ids.ToArray();
            }
        }
        else if (property.Equals("quest_id", StringComparison.OrdinalIgnoreCase))
        {
            if (!reader.TryGetGuid(out QuestID))
                L.LogWarning("Failed to convert " + property + " with value \"" + (reader.GetString() ?? "null") + "\" to a GUID.");
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("unlock_presets");
        writer.WriteStartArray();
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            writer.WriteStringValue(UnlockPresets[i]);
        }
        writer.WriteEndArray();

        writer.WriteString("quest_id", QuestID);
    }
    public override object Clone()
    {
        QuestUnlockRequirement req = new QuestUnlockRequirement
        {
            QuestID = QuestID,
            UnlockPresets = new Guid[UnlockPresets.Length]
        };
        Array.Copy(UnlockPresets, req.UnlockPresets, UnlockPresets.Length);
        return req;
    }
    protected override void Read(ByteReader reader)
    {
        QuestID = reader.ReadGuid();
        UnlockPresets = reader.ReadGuidArray();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(QuestID);
        writer.Write(UnlockPresets);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            QuestManager.TryAddQuest(ctx.Caller, asset);
            return ctx.Reply(T.RequestKitQuestIncomplete, asset);
        }
        return ctx.Reply(T.RequestKitQuestIncomplete, null!);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            QuestManager.TryAddQuest(ctx.Caller, asset);
            return ctx.Reply(T.RequestVehicleQuestIncomplete, asset);
        }
        return ctx.Reply(T.RequestVehicleQuestIncomplete, null!);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            QuestManager.TryAddQuest(ctx.Caller, asset);
            return ctx.Reply(T.RequestTraitQuestIncomplete, trait, asset);
        }
        return ctx.Reply(T.RequestTraitQuestIncomplete, trait, null!);
    }
    public override bool Equals(object obj)
    {
        if (!(obj is QuestUnlockRequirement r && r.QuestID == QuestID))
            return false;
        if (r.UnlockPresets is not { Length: > 0 } && UnlockPresets is not { Length: > 0 })
            return true;
        if (r.UnlockPresets.Length != UnlockPresets.Length)
            return false;
        for (int i = 0; i < UnlockPresets.Length; ++i)
        {
            if (r.UnlockPresets[i] != UnlockPresets[i])
                return false;
        }

        return true;
    }

    protected bool Equals(QuestUnlockRequirement other)
    {
        return QuestID.Equals(other.QuestID) && UnlockPresets.Equals(other.UnlockPresets);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(QuestID, UnlockPresets.Length);
    // ReSharper restore NonReadonlyMemberInGetHashCode
}