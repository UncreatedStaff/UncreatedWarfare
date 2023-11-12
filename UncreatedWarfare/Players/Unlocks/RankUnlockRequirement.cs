using System;
using System.Text.Json;
using Uncreated.Encoding;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

[UnlockRequirement(2, "unlock_rank")]
public class RankUnlockRequirement : UnlockRequirement
{
    public int UnlockRank = -1;
    public override bool CanAccess(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        return success && data.Order >= UnlockRank;
    }
    public override string GetSignText(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        ref Ranks.RankData reqData = ref Ranks.RankManager.GetRank(UnlockRank, out _);
        return T.KitRequiredRank.Translate(player, false, reqData, success && data.Order >= reqData.Order ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_rank", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockRank);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_rank", UnlockRank);
    }
    public override object Clone() => new RankUnlockRequirement { UnlockRank = UnlockRank };
    protected override void Read(ByteReader reader)
    {
        UnlockRank = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockRank);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in kit requirement: " + (kit?.Id ?? string.Empty) + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestKitLowRank, data);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        ref Ranks.RankData rankData = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in vehicle requirement: " + data.VehicleID + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestVehicleRankIncomplete, rankData);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in trait requirement: " + trait.TypeName + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestTraitLowRank, trait, data);
    }
    public override bool Equals(object obj) => obj is RankUnlockRequirement r && r.UnlockRank == UnlockRank;
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => UnlockRank;
}