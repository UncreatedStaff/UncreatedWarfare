using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.FOBs.Construction;

/// <summary>
/// Represents various types of buildables.
/// </summary>
[Translatable("Buildable Type", Description = "Buildable types for FOB buildings.")]
public enum ShovelableType
{
    // todo: rename

    /// <summary>
    /// A FOB bunker that players can deploy to.
    /// </summary>
    [Translatable(Languages.ChineseSimplified, "地堡")]
    Fob,

    /// <summary>
    /// A crate where players can refill their ammo and use Ammo Supplies.
    /// </summary>
    [Translatable(Languages.ChineseSimplified, "弹药箱")]
    AmmoCrate,

    /// <summary>
    /// A buildable that repairs nearby vehicles using Building Supplies.
    /// </summary>
    [Translatable(Languages.ChineseSimplified, "维修站")]
    RepairStation,

    /// <summary>
    /// A buildable that acts purely as fortification and has no other function.
    /// </summary>
    [Translatable(Languages.ChineseSimplified, "防御")]
    [Translatable("Fortification", Description = "Barricade or Structure buildables that do not fall into another category.")]
    Fortification,

    /// <summary>
    /// A buildable that spawns a vehicle to be operated by players.
    /// </summary>
    [Translatable(Languages.ChineseSimplified, "架设")]
    [Translatable("Emplacement", Description = "Vehicle buildables.")]
    Emplacement,

    // todo: remove
    ///// <summary>
    ///// A radio that acts as the 'center' of the FOB.
    ///// </summary>
    //[Translatable(Languages.ChineseSimplified, "电台")]
    //Radio
}