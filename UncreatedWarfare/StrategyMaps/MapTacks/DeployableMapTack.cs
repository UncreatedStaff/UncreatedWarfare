using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;
internal class DeployableMapTack : MapTack
{
    public IDeployable Deployable { get; }
    public override Vector3 FeatureWorldPosition => Deployable.SpawnPosition;

    public DeployableMapTack(IAssetLink<ItemBarricadeAsset> markerAsset, IDeployable deployable)
        : base(markerAsset, deployable.SpawnPosition)
    {
        Deployable = deployable;
    }
}
