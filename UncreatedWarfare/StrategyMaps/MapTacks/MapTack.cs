using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;
internal class MapTack : IDisposable
{
    public IAssetLink<ItemBarricadeAsset> MarkerAsset { get; }
    public IBuildable Marker { get; private set; }
    public virtual Vector3 FeatureWorldPosition { get; }

    public MapTack(IAssetLink<ItemBarricadeAsset> markerAsset, Vector3 featureWorldPosition)
    {
        MarkerAsset = markerAsset;
        FeatureWorldPosition = featureWorldPosition;
    }

    public void DropMarker(Vector3 worldCoordinatesOnTable, Quaternion rotation)
    {
        if (Marker != null)
            throw new InvalidOperationException("Map tack's marker has already been dropped. Map tack markers should not be dropped more than once.");       

        Transform mapTackTransform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(MarkerAsset.GetAssetOrFail()), worldCoordinatesOnTable, rotation, 0, 0
            );

        BarricadeDrop marker = BarricadeManager.FindBarricadeByRootTransform(mapTackTransform);
        Marker = new BuildableBarricade(marker);
    }

    public void Dispose()
    {
        Marker.Destroy();
    }
}
