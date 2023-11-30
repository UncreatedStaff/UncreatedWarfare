using System;
using System.Text.Json;
using Uncreated.Encoding;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

[UnlockRequirement(1, "unlock_level")]
public class LevelUnlockRequirement : UnlockRequirement
{
    public int UnlockLevel = -1;
    public override bool CanAccess(UCPlayer player)
    {
        return player.Level.Level >= UnlockLevel;
    }
    public override string GetSignText(UCPlayer player)
    {
        if (UnlockLevel == 0)
            return string.Empty;

        int lvl = Points.GetLevel(player.CachedXP);
        return T.KitRequiredLevel.Translate(player, false, LevelData.GetRankAbbreviation(UnlockLevel), lvl >= UnlockLevel ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_level", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockLevel);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_level", UnlockLevel);
    }
    public override object Clone() => new LevelUnlockRequirement { UnlockLevel = UnlockLevel };
    protected override void Read(ByteReader reader)
    {
        UnlockLevel = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockLevel);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestKitLowLevel, data);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        LevelData data2 = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestVehicleMissingLevels, data2);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestTraitLowLevel, trait, data);
    }

    public override bool Equals(object obj) => obj is LevelUnlockRequirement r && r.UnlockLevel == UnlockLevel;
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => UnlockLevel;
}