using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using UnityEngine;

namespace Uncreated.Warfare.StrategyMaps;
internal class StrategyMap : IDisposable, IEventListener<ClaimBedRequested>
{
    private readonly MapTableInfo _tableInfo;

    public IBuildable MapTable { get; set; }
    public TrackingList<MapTack> _activeMapTacks { get; set; }

    public StrategyMap(IBuildable buildable, MapTableInfo tableInfo)
    {
        _tableInfo = tableInfo;
        MapTable = buildable;
        _activeMapTacks = new TrackingList<MapTack>();
    }
    public void RepopulateMapTacks(Dictionary<Vector3, ItemBarricadeAsset> newMapTacks)
    {
        _activeMapTacks.Clear();
        foreach (var pair in newMapTacks)
        {
            AddMapTack(pair.Value, pair.Key);
        }
    }
    public void ClearMapTacks()
    {
        foreach (var tack in _activeMapTacks)
        {
            tack.Dispose();
        }
    }
    public void Dispose() => ClearMapTacks();
    public void AddMapTack(ItemBarricadeAsset asset, Vector3 featureWorldPosition)
    {
        Vector3 worldCoordsOnMapTable = TranslateWorldPointOntoMap(featureWorldPosition);

        Transform mapTackTransform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(asset), worldCoordsOnMapTable, MapTable.Rotation, 0, 0
            );

        BarricadeDrop mapTackDrop = BarricadeManager.FindBarricadeByRootTransform(mapTackTransform);

        MapTack newMapTack = new MapTack(new BuildableBarricade(mapTackDrop), featureWorldPosition);
        _activeMapTacks.Add(newMapTack);
    }
    public void RemoveMapTack(MapTack newMapTack)
    {
        _activeMapTacks.Remove(newMapTack);
        newMapTack.Dispose();
    }
    public void ReplaceMapTack(ItemBarricadeAsset newMapTackAsset, MapTack oldMapTack)
    {
        RemoveMapTack(oldMapTack);
        AddMapTack(newMapTackAsset, oldMapTack.FeatureWorldPosition);
    }
    public Vector3 TranslateWorldPointOntoMap(Vector3 featureWorldPosition)
    {
        Matrix4x4 matrix = ProjectWorldCoordsToMapTable(MapTable.Model, new Vector3(0, _tableInfo.VerticalSurfaceOffset, 0), new Vector2(_tableInfo.MapTableSquareWidth, _tableInfo.MapTableSquareWidth));

        return matrix.MultiplyPoint3x4(featureWorldPosition);
    }
    /// Projects a world coodinate to the world co
    /// <summary>ordiate of a point on a flat 'war table' type barricade, given the x and y size and 3D offset of the table.
    /// </summary>
    public static Matrix4x4 ProjectWorldCoordsToMapTable(Transform mapTableTransform, Vector3 platformOffset, Vector2 platformSize)
    {
        Vector3 scale = default;
        scale.x = platformSize.x / 2f;
        scale.y = platformSize.y / -2f;

        // fit map into platform
        Vector2 captureSize = CartographyUtility.WorldCaptureAreaDimensions;

        float xRatio = platformSize.x / captureSize.x,
              yRatio = platformSize.y / captureSize.y;

        if (xRatio > yRatio)
            scale.x *= yRatio / xRatio;
        else
            scale.y *= xRatio / yRatio;

        Matrix4x4 normalizedToBarricade = Matrix4x4.TRS(
            mapTableTransform.position + platformOffset,
            mapTableTransform.rotation,
            scale
        );

        return normalizedToBarricade * CartographyUtility.WorldToMap;
    }
    
    public override string ToString() => $"StrategyMap[MapTable: [{MapTable}] MapTacks: {_activeMapTacks}]";

    public void HandleEvent(ClaimBedRequested e, IServiceProvider serviceProvider)
    {
        MapTack mapTack = _activeMapTacks.FirstOrDefault(t => t.Marker.InstanceId == e.Barricade.instanceID);

        e.Player.UnturnedPlayer.teleportToLocation(
            new Vector3(
                mapTack.FeatureWorldPosition.x,
                TerrainUtility.GetHighestPoint(mapTack.FeatureWorldPosition, 0),
                mapTack.FeatureWorldPosition.z),
            0
            );

        e.Cancel();
    }
}
