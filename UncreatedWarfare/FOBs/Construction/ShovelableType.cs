using System;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.FOBs.Construction;

/// <summary>
/// Represents various types of buildables.
/// </summary>
[Translatable("Buildable Type", Description = "Buildable types for FOB buildings.")]
public enum ShovelableType
{
    /// <summary>
    /// A FOB bunker that players can deploy to.
    /// </summary>
    [TranslatableValue("FOB")]
    Fob,

    /// <summary>
    /// An ammo vending buildable where players can rearm/change their kits using FOB ammo.
    /// </summary>
    [TranslatableValue("Fob Ammo Vendor")]
    FobAmmoVendor,

    /// <summary>
    /// A buildable that repairs nearby vehicles using Building Supplies.
    /// </summary>
    [TranslatableValue("Repair Station")]
    RepairStation,

    /// <summary>
    /// A buildable that acts purely as fortification and has no other function.
    /// </summary>
    [TranslatableValue(Description = "Barricade or Structure buildables that do not fall into another category.")]
    Fortification,

    /// <summary>
    /// A buildable that spawns a vehicle to be operated by players.
    /// </summary>
    [TranslatableValue(Description = "Vehicle buildables.")]
    Emplacement
}