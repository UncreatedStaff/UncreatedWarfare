using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.Construction;
public interface IBuildableFobEntityInfo
{
    string? Icon { get; }
    Vector3 IconOffset { get; }

    IAssetLink<ItemPlaceableAsset>? IdentifyingAsset { get; }
}