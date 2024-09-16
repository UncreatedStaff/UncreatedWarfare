using System;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

public class RankUnlockRequirement : UnlockRequirement
{
    public int UnlockRank { get; set; } = -1;

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        return success && data.Order >= UnlockRank;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        ref Ranks.RankData reqData = ref Ranks.RankManager.GetRank(UnlockRank, out _);
        return T.KitRequiredRank.Translate(player, false, reqData, success && data.Order >= reqData.Order ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
    }

    /// <inheritdoc />
    protected override void ReadLegacyProperty(ILogger? logger, ref Utf8JsonReader reader, string property)
    {
        if (!property.Equals("unlock_rank", StringComparison.OrdinalIgnoreCase))
            return;
        
        if (reader.TryGetInt32(out int unlockRank))
            UnlockRank = unlockRank;
    }

    /// <inheritdoc />
    protected override bool ReadFromJson(ILogger? logger, ref Utf8JsonReader reader)
    {
        bool read = false;
        JsonUtility.ReadTopLevelProperties(ref reader, ref read, (ref Utf8JsonReader reader, string property, ref bool read) =>
        {
            if (!property.Equals("unlock_rank", StringComparison.OrdinalIgnoreCase) || !reader.TryGetInt32(out int unlockRank))
                return;

            UnlockRank = unlockRank;
            read = true;
        });

        return read;
    }

    /// <inheritdoc />
    protected override void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_rank", UnlockRank);
    }

    /// <inheritdoc />
    public override object Clone()
    {
        return new RankUnlockRequirement { UnlockRank = UnlockRank };
    }

    /// <inheritdoc />
    public override Exception RequestKitFailureToMeet(CommandContext ctx, Kit kit)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in kit requirement: " + (kit?.InternalName ?? string.Empty) + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestKitLowRank, data);
    }

    /// <inheritdoc />
    public override Exception RequestVehicleFailureToMeet(CommandContext ctx, WarfareVehicleInfo data)
    {
        ref Ranks.RankData rankData = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in vehicle requirement: " + data.VehicleID + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestVehicleRankIncomplete, rankData);
    }

    /// <inheritdoc />
    public override Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in trait requirement: " + trait.TypeName + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestTraitLowRank, trait, data);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RankUnlockRequirement r && r.UnlockRank == UnlockRank;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return UnlockRank;
    }
}