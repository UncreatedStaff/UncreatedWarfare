using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Teams;

/// <summary>Max field character limit: <see cref="KitEx.RedirectTypeCharLimit"/>.</summary>
[ExcludedEnum(None)]
[ExcludedEnum(StandardAmmoIcon)]
[ExcludedEnum(StandardGrenadeIcon)]
[ExcludedEnum(StandardMeleeIcon)]
[ExcludedEnum(StandardSmokeGrenadeIcon)]
[ExcludedEnum(VehicleBay)]
[Translatable(Description = "Common items represented by a redirect Id.")]
public enum RedirectType : byte
{
    None = 255,
    [Translatable(Languages.ChineseSimplified, "上衣")]
    Shirt = 0,
    [Translatable(Languages.ChineseSimplified, "裤子")]
    Pants,
    [Translatable(Languages.ChineseSimplified, "背心")]
    Vest,
    [Translatable(Languages.ChineseSimplified, "帽子")]
    Hat,
    [Translatable(Languages.ChineseSimplified, "面具")]
    Mask,
    [Translatable(Languages.ChineseSimplified, "背包")]
    Backpack,
    [Translatable(Languages.ChineseSimplified, "眼镜")]
    Glasses,
    [Translatable(Languages.ChineseSimplified, "弹药补给")]
    [Translatable("Ammo Supplies")]
    AmmoSupply,
    [Translatable(Languages.ChineseSimplified, "建筑材料")]
    [Translatable("Building Supplies")]
    BuildSupply,
    [Translatable(Languages.ChineseSimplified, "集合点")]
    [Translatable("Rally Point")]
    RallyPoint,
    [Translatable(Languages.ChineseSimplified, "FOB 电台")]
    [Translatable("FOB Radio")]
    Radio,
    [Translatable(Languages.ChineseSimplified, "弹药包")]
    [Translatable("Ammo Bag")]
    AmmoBag,
    [Translatable(Languages.ChineseSimplified, "弹药箱")]
    [Translatable("Ammo Crate")]
    AmmoCrate,
    [Translatable(Languages.ChineseSimplified, "维修站")]
    [Translatable("Repair Station")]
    RepairStation,
    [Translatable(Languages.ChineseSimplified, "FOB 地堡")]
    [Translatable("FOB Bunker")]
    Bunker,
    [Translatable(Languages.ChineseSimplified, "载具停泊处")]
    [Translatable("Vehicle Bay")]
    VehicleBay,
    [Translatable(Languages.ChineseSimplified, "工兵铲")]
    [Translatable("Entrenching Tool")]
    EntrenchingTool,
    [Translatable(Languages.ChineseSimplified, "无人机")]
    [Translatable("UAV", Description = "Unmanned Aerial Vehicle")]
    UAV,
    [Translatable(Languages.ChineseSimplified, "建造维修站")]
    [Translatable("Built Repair Station")]
    RepairStationBuilt,
    [Translatable(Languages.ChineseSimplified, "建造弹药箱")]
    [Translatable("Built Ammo Crate")]
    AmmoCrateBuilt,
    [Translatable(Languages.ChineseSimplified, "建造FOB地堡")]
    [Translatable("Built FOB Bunker")]
    BunkerBuilt,
    [Translatable(Languages.ChineseSimplified, "隐藏地")]
    [Translatable("Insurgency Cache")]
    Cache,
    [Translatable(Languages.ChineseSimplified, "电台受损")]
    [Translatable("Damaged Radio")]
    RadioDamaged,
    [Translatable(Languages.ChineseSimplified, "激光指示器")]
    [Translatable("Laser Designator")]
    LaserDesignator,
    [Translatable("Generic Ammo", IsPrioritizedTranslation = false)]
    StandardAmmoIcon,
    [Translatable("Generic Knife", IsPrioritizedTranslation = false)]
    StandardMeleeIcon,
    [Translatable("Generic Grenade", IsPrioritizedTranslation = false)]
    StandardGrenadeIcon,
    [Translatable("Generic Smoke Grenade", IsPrioritizedTranslation = false)]
    StandardSmokeGrenadeIcon,
}