using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrateInfo
{
    public required IAssetLink<ItemPlaceableAsset> SupplyItemAsset { get; set; }
    public required IAssetLink<EffectAsset> PlacementEffect { get; set; }
    public SupplyType Type { get; set; }
    public int StartingSupplies { get; set; } = 30;
    public int SupplyRadius { get; set; } = 40;
}