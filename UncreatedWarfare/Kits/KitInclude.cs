using System;

namespace Uncreated.Warfare.Kits;

[Flags]
public enum KitInclude
{
    Base = 0,

    Items              = 1 << 0,
    UnlockRequirements = 1 << 1,
    FactionFilter      = 1 << 2,
    MapFilter          = 1 << 3,
    Translations       = 1 << 4,
    Skillsets          = 1 << 5,
    Bundles            = 1 << 6,
    Access             = 1 << 7,
    Delays             = 1 << 8,
    Favorites          = 1 << 9,
                      // 1 << 10 reserved for KitModel.Faction (not needed for Kit class)

    /// <summary>
    /// Basic information and translations.
    /// </summary>
    Default = Base | Translations,

    /// <summary>
    /// Minimum information to cache a kit.
    /// </summary>
    Cached = Default | UnlockRequirements | Delays,

    /// <summary>
    /// Able to be given to a player.
    /// </summary>
    Giveable = Default | Items | Skillsets,
    
    Verifiable = Default | FactionFilter | MapFilter | UnlockRequirements | Delays,

    Buyable = Default | FactionFilter | MapFilter | UnlockRequirements | Delays,

    UI = Default | Items | FactionFilter | MapFilter | UnlockRequirements | Delays,

    All = Base | Items | UnlockRequirements | FactionFilter | MapFilter | Translations | Skillsets | Bundles | Access | Delays | Favorites,

    None = 1 << 11
}
