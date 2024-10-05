using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps;
internal class MapTack : IDisposable
{
    public IBuildable Marker { get; set; }
    public Vector3 FeatureWorldPosition { get; }

    public MapTack(IBuildable mapTackMarker, Vector3 featureWorldPosition)
    {
        Marker = mapTackMarker;
        FeatureWorldPosition = featureWorldPosition;
    }

    public void Dispose()
    {
        Marker.Destroy();
    }
}
