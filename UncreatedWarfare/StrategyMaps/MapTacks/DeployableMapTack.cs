using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Deployment;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class DeployableMapTack : MapTack
{
    public IDeployable Deployable { get; }
    public override Vector3 FeatureWorldPosition => Deployable.SpawnPosition;

    public DeployableMapTack(StrategyMapManager manager, StrategyMap map, IAssetLink<ItemBarricadeAsset> markerAsset, IDeployable deployable) : this(manager, map, markerAsset, deployable, deployable as IMapTackUIHandler, leaveUiHandlerOpen: true) { }
    public DeployableMapTack(StrategyMapManager manager, StrategyMap map, IAssetLink<ItemBarricadeAsset> markerAsset, IDeployable deployable, IMapTackUIHandler? uiHandler, bool leaveUiHandlerOpen = false)
        : base(manager, map, markerAsset, deployable.SpawnPosition, uiHandler, leaveUiHandlerOpen)
    {
        Deployable = deployable ?? throw new ArgumentNullException(nameof(deployable));
    }
}
