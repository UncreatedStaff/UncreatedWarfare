using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Deployment;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class DeployableMapTack : MapTack
{
    public IDeployable Deployable { get; }
    public override Vector3 FeatureWorldPosition => Deployable.SpawnPosition;

    public DeployableMapTack(IAssetLink<ItemBarricadeAsset> markerAsset, IDeployable deployable)
        : base(markerAsset, deployable.SpawnPosition)
    {
        Deployable = deployable;
    }
}
