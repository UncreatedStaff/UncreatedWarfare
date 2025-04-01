using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;

public class ThrownAmmoBagInfo
{
    public required IAssetLink<ItemAsset> ThrowableItemAsset { get; set; }
    public required IAssetLink<ItemBarricadeAsset> AmmoBagBarricadeAsset { get; set; }

    public required float StartingAmmo { get; set; } = 5;
    // public required IAssetLink<EffectAsset> ResupplyEffect { get; set; }
}