using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class MapTack : IDisposable, ITransformObject
{
    public IAssetLink<ItemPlaceableAsset> MarkerAsset { get; }
    public IBuildable Marker { get; private set; }
    public virtual Vector3 FeatureWorldPosition { get; }

    public MapTack(IAssetLink<ItemPlaceableAsset> markerAsset, Vector3 featureWorldPosition)
    {
        Marker = null!;
        MarkerAsset = markerAsset;
        FeatureWorldPosition = featureWorldPosition;
    }

    public virtual void DropMarker(Vector3 worldCoordinatesOnTable, Quaternion rotation)
    {
        if (Marker != null)
            throw new InvalidOperationException("Map tack's marker has already been dropped. Map tack markers should not be dropped more than once.");

        Marker = BuildableExtensions.DropBuildable(MarkerAsset.GetAssetOrFail(), worldCoordinatesOnTable, rotation);
    }

    public void Dispose()
    {
        Marker.Destroy();
    }

    public Vector3 Position
    {
        get => Marker.Position;
        set => Marker.Position = value;
    }

    public Quaternion Rotation
    {
        get => Marker.Rotation;
        set => Marker.Rotation = value;
    }

    public Vector3 Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        Marker.SetPositionAndRotation(position, rotation);
    }

    public bool Alive => !Marker.IsDead;
}
