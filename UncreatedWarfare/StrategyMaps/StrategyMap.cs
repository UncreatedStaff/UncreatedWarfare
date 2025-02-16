using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.StrategyMaps;

public class StrategyMap : IDisposable, IEventListener<ClaimBedRequested>
{
    private readonly MapTableInfo _tableInfo;
    private readonly TrackingList<MapTack> _activeMapTacks;
    
    public IBuildable MapTable { get; set; }

    public StrategyMap(IBuildable buildable, MapTableInfo tableInfo)
    {
        _tableInfo = tableInfo;
        MapTable = buildable;
        _activeMapTacks = new TrackingList<MapTack>();
    }

    public void RepopulateMapTacks(IEnumerable<MapTack> newMapTacks)
    {
        GameThread.AssertCurrent();

        ClearMapTacks();
        foreach (MapTack mapTack in newMapTacks)
        {
            AddMapTack(mapTack);
        }
    }

    public void ClearMapTacks()
    {
        GameThread.AssertCurrent();

        foreach (MapTack tack in _activeMapTacks)
        {
            tack.Dispose();
        }
        _activeMapTacks.Clear();
    }

    public void AddMapTack(MapTack newMapTack)
    {
        GameThread.AssertCurrent();

        Vector3 worldCoordsOnMapTable = TranslateWorldPointOntoMap(newMapTack.FeatureWorldPosition);

        newMapTack.DropMarker(worldCoordsOnMapTable, MapTable.Rotation);

        _activeMapTacks.Add(newMapTack);
    }

    public bool RemoveMapTack(MapTack newMapTack)
    {
        GameThread.AssertCurrent();

        if (!_activeMapTacks.Remove(newMapTack))
            return false;
        
        newMapTack.Dispose();
        return true;
    }

    public void RemoveMapTacks(Func<MapTack, bool> filter)
    {
        GameThread.AssertCurrent();

        for (int i = _activeMapTacks.Count - 1; i >= 0; --i)
        {
            MapTack tack = _activeMapTacks[i];
            if (!filter(tack))
                continue;

            _activeMapTacks.RemoveAt(i);
            tack.Dispose();
        }
    }

    public Vector3 TranslateWorldPointOntoMap(Vector3 featureWorldPosition)
    {
        Matrix4x4 matrix = ProjectWorldCoordsToMapTable(MapTable.Model, new Vector3(0, _tableInfo.VerticalSurfaceOffset, 0), new Vector2(_tableInfo.MapTableSquareWidth, _tableInfo.MapTableSquareWidth));

        return matrix.MultiplyPoint3x4(featureWorldPosition);
    }

    /// <summary>
    /// Projects a world coodinate to the world coordinate of a point on a flat 'war table' type barricade, given the x and y size and 3D offset of the table.
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

    void IEventListener<ClaimBedRequested>.HandleEvent(ClaimBedRequested e, IServiceProvider serviceProvider)
    {
        MapTack? mapTack = _activeMapTacks.FirstOrDefault(t => t.Marker.Equals(e.Barricade));

        if (mapTack is not DeployableMapTack d)
        {
            e.Cancel();
            return;
        }

        DeploymentService deploymentService = serviceProvider.GetRequiredService<DeploymentService>();

        if (e.Player.Component<DeploymentComponent>().CurrentDeployment != null)
        {
            // todo: send chat message
            e.Cancel();
            return;
        }

        deploymentService.TryStartDeployment(e.Player, d.Deployable,
            new DeploySettings
            {
                AllowNearbyEnemies = false,
            }
        );

        e.Cancel();
    }

    public void Dispose()
    {
        ClearMapTacks();
    }

    public override string ToString()
    {
        return $"StrategyMap[MapTable: [{MapTable}] MapTacks: {string.Join(", ", _activeMapTacks)}]";
    }
}
