using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Teams;

[ExcludedEnum(None)]
[ExcludedEnum(StandardAmmoIcon)]
[ExcludedEnum(StandardGrenadeIcon)]
[ExcludedEnum(StandardMeleeIcon)]
[ExcludedEnum(StandardSmokeGrenadeIcon)]
[Translatable("Item Redirect Type", Description = "Common special items that can change depending on your current faction.")]
public enum RedirectType : byte
{
    None = 255,
    Shirt = 0,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses,
    [TranslatableValue("Ammo Supplies")]
    AmmoSupply,
    [TranslatableValue("Building Supplies")]
    BuildSupply,
    [TranslatableValue("Rally Point")]
    RallyPoint,
    [TranslatableValue("FOB Radio")]
    Radio,
    [TranslatableValue("Ammo Bag")]
    AmmoBag,
    [TranslatableValue("Ammo Crate")]
    AmmoCrate,
    [TranslatableValue("Repair Station")]
    RepairStation,
    [TranslatableValue("FOB Bunker")]
    Bunker,
    [TranslatableValue("Vehicle Bay")]
    VehicleBay,
    [TranslatableValue("Entrenching Tool")]
    EntrenchingTool,
    [TranslatableValue("UAV", Description = "Unmanned Aerial Vehicle")]
    UAV,
    [TranslatableValue("Built Repair Station")]
    RepairStationBuilt,
    [TranslatableValue("Built Ammo Crate")]
    AmmoCrateBuilt,
    [TranslatableValue("Built FOB Bunker")]
    BunkerBuilt,
    [TranslatableValue("Insurgency Cache")]
    Cache,
    [TranslatableValue("Damaged Radio")]
    RadioDamaged,
    [TranslatableValue("Laser Designator")]
    LaserDesignator,
    [TranslatableValue("Map Tack Flag")]
    MapTackFlag,
    [TranslatableValue("Generic Ammo", IsPrioritizedTranslation = false)]
    StandardAmmoIcon,
    [TranslatableValue("Generic Knife", IsPrioritizedTranslation = false)]
    StandardMeleeIcon,
    [TranslatableValue("Generic Grenade", IsPrioritizedTranslation = false)]
    StandardGrenadeIcon,
    [TranslatableValue("Generic Smoke Grenade", IsPrioritizedTranslation = false)]
    StandardSmokeGrenadeIcon,
    [TranslatableValue("Generic Buildable", IsPrioritizedTranslation = false)]
    StandardBuildable
}