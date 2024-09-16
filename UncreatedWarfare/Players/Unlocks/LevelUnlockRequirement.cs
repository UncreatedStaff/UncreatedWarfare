using System;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

public class LevelUnlockRequirement : UnlockRequirement
{
    public int UnlockLevel = -1;

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        return player.Level.Level >= UnlockLevel;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer player)
    {
        if (UnlockLevel == 0)
            return string.Empty;

        int lvl = Points.GetLevel(player.CachedXP);
        return T.KitRequiredLevel.Translate(player, false, LevelData.GetRankAbbreviation(UnlockLevel), lvl >= UnlockLevel ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
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
    protected override bool ReadFromJson(ILogger? logger, ref Utf8JsonReader reader)
    {
        bool read = false;
        JsonUtility.ReadTopLevelProperties(ref reader, ref read, (ref Utf8JsonReader reader, string property, ref bool read) =>
        {
            if (!property.Equals("unlock_level", StringComparison.OrdinalIgnoreCase) || !reader.TryGetInt32(out int unlockRank))
                return;

            UnlockLevel = unlockRank;
            read = true;
        });

        return read;
    }

    /// <inheritdoc />
    protected override void WriteToJson(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_level", UnlockLevel);
    }

    /// <inheritdoc />
    public override object Clone()
    {
        return new LevelUnlockRequirement { UnlockLevel = UnlockLevel };
    }

    /// <inheritdoc />
    public override Exception RequestKitFailureToMeet(CommandContext ctx, Kit kit)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestKitLowLevel, data);
    }

    /// <inheritdoc />
    public override Exception RequestVehicleFailureToMeet(CommandContext ctx, WarfareVehicleInfo data)
    {
        LevelData data2 = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestVehicleMissingLevels, data2);
    }

    /// <inheritdoc />
    public override Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestTraitLowLevel, trait, data);
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