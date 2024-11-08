﻿using System;
using System.Globalization;
using System.Text.Json;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Players.Unlocks;

public class LevelUnlockRequirement : UnlockRequirement
{
    public int UnlockLevel = -1;

    /// <inheritdoc />
    public override bool CanAccessFast(WarfarePlayer player)
    {
        return false;// todo player.Level.Level >= UnlockLevel;
    }

    /// <inheritdoc />
    public override string GetSignText(WarfarePlayer? player, LanguageInfo language, CultureInfo culture)
    {
        if (UnlockLevel == 0)
            return "not implemented";

        // int lvl = Points.GetLevel(player.CachedXP);
        return "not implemented"; // todo T.KitRequiredLevel.Translate(player, false, LevelData.GetRankAbbreviation(UnlockLevel), lvl >= UnlockLevel ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
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
        // LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(ctx.CommonTranslations.NotImplemented/* T.RequestKitLowLevel, data */);
    }

    /// <inheritdoc />
    public override Exception RequestVehicleFailureToMeet(CommandContext ctx, WarfareVehicleInfo data)
    {
        // LevelData data2 = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(ctx.CommonTranslations.NotImplemented/* T.RequestVehicleMissingLevels, data2 */);
    }

#if false
    /// <inheritdoc />
    public override Exception RequestTraitFailureToMeet(CommandContext ctx, TraitData trait)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestTraitLowLevel, trait, data);
    }
#endif

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