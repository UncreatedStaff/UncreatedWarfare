using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.Entities;

public class FortificationEntity : IBuildableFobEntity
{
    public IBuildable Buildable { get; }
    public IAssetLink<Asset> IdentifyingAsset { get; }
    public Vector3 Position => Buildable.Position;
    public Quaternion Rotation => Buildable.Rotation;
    public bool PreventItemDrops => false;

    public FortificationEntity(IBuildable buildable)
    {
        Buildable = buildable;
        IdentifyingAsset = AssetLink.Create(buildable.Asset);
    }
}
