using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrateInfo : IBuildableFobEntityInfo
{
    public required IAssetLink<ItemPlaceableAsset> SupplyItemAsset { get; set; }
    public required IAssetLink<EffectAsset> PlacementEffect { get; set; }
    public SupplyType Type { get; set; }
    public int StartingSupplies { get; set; } = 30;
    public int SupplyRadius { get; set; } = 40;

    /// <summary>
    /// The axis that is perpendicular with the stack's front/back.
    /// </summary>
    public SnapAxis StackAxis { get; set; } = SnapAxis.X;
    public int MaxStackHeight { get; set; } = 3;
    public int MaxStackWidth { get; set; } = 7;

    IAssetLink<ItemPlaceableAsset> IBuildableFobEntityInfo.IdentifyingAsset => SupplyItemAsset;

    public string? Icon { get; set; }
    public Vector3 IconOffset { get; set; }
}