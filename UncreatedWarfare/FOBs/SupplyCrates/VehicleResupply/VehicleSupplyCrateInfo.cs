using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates.VehicleResupply;

public class VehicleSupplyCrateInfo
{
    public required IAssetLink<ItemBarricadeAsset> SupplyItemAsset { get; set; }
    public required IAssetLink<EffectAsset> ResupplyEffect { get; set; }
}