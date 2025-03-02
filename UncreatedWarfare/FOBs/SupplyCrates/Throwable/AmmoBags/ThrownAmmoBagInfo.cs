using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrownAmmoBagInfo
{
    public required IAssetLink<ItemAsset> ThrowableItemAsset { get; set; }
    public required IAssetLink<ItemBarricadeAsset> AmmoBagBarricadeAsset { get; set; }
    // public required IAssetLink<EffectAsset> ResupplyEffect { get; set; }
}