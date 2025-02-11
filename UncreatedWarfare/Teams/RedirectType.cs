using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Teams;

[ExcludedEnum(None)]
[ExcludedEnum(StandardAmmoIcon)]
[ExcludedEnum(StandardGrenadeIcon)]
[ExcludedEnum(StandardMeleeIcon)]
[ExcludedEnum(StandardSmokeGrenadeIcon)]
[Translatable(Description = "Common items represented by a redirect Id.")]
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
    [Translatable("Ammo Supplies")]
    AmmoSupply,
    [Translatable("Building Supplies")]
    BuildSupply,
    [Translatable("Rally Point")]
    RallyPoint,
    [Translatable("FOB Radio")]
    Radio,
    [Translatable("Ammo Bag")]
    AmmoBag,
    [Translatable("Ammo Crate")]
    AmmoCrate,
    [Translatable("Repair Station")]
    RepairStation,
    [Translatable("FOB Bunker")]
    Bunker,
    [Translatable("Vehicle Bay")]
    VehicleBay,
    [Translatable("Entrenching Tool")]
    EntrenchingTool,
    [Translatable("UAV", Description = "Unmanned Aerial Vehicle")]
    UAV,
    [Translatable("Built Repair Station")]
    RepairStationBuilt,
    [Translatable("Built Ammo Crate")]
    AmmoCrateBuilt,
    [Translatable("Built FOB Bunker")]
    BunkerBuilt,
    [Translatable("Insurgency Cache")]
    Cache,
    [Translatable("Damaged Radio")]
    RadioDamaged,
    [Translatable("Laser Designator")]
    LaserDesignator,
    [Translatable("Map Tack Flag")]
    MapTackFlag,
    [Translatable("Generic Ammo", IsPrioritizedTranslation = false)]
    StandardAmmoIcon,
    [Translatable("Generic Knife", IsPrioritizedTranslation = false)]
    StandardMeleeIcon,
    [Translatable("Generic Grenade", IsPrioritizedTranslation = false)]
    StandardGrenadeIcon,
    [Translatable("Generic Smoke Grenade", IsPrioritizedTranslation = false)]
    StandardSmokeGrenadeIcon,
}