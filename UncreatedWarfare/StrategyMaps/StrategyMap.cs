using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Provider.Services.Translation;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using UnityEngine;

namespace Uncreated.Warfare.StrategyMaps;
internal class StrategyMap : IDisposable, IEventListener<ClaimBedRequested>
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
        ClearMapTacks();
        foreach (MapTack mapTack in newMapTacks)
        {
            AddMapTack(mapTack);
        }
    }
    public void ClearMapTacks()
    {
        foreach (MapTack tack in _activeMapTacks)
        {
            tack.Dispose();
        }
        _activeMapTacks.Clear();
    }
    public void Dispose() => ClearMapTacks();

    public void AddMapTack(MapTack newMapTack)
    {
        Vector3 worldCoordsOnMapTable = TranslateWorldPointOntoMap(newMapTack.FeatureWorldPosition);

        newMapTack.DropMarker(worldCoordsOnMapTable, MapTable.Rotation);

        _activeMapTacks.Add(newMapTack);
    }
    public void RemoveMapTack(MapTack newMapTack)
    {
        _activeMapTacks.Remove(newMapTack);
        newMapTack.Dispose();
    }
    public void RemoveMapTacks(Func<MapTack, bool> filter)
    {
        foreach (MapTack mapTack in _activeMapTacks.Where(filter))
        {
            mapTack.Dispose();
        }

        _activeMapTacks.RemoveAll(new Predicate<MapTack>(filter));
    }
    public void ReplaceMapTack(MapTack oldMapTack, MapTack newMapTack)
    {
        RemoveMapTack(oldMapTack);
        AddMapTack(newMapTack);
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

        if (mapTack is not DeployableMapTack d)
            return;

        FobConfiguration fobConfig = serviceProvider.GetRequiredService<FobConfiguration>();
        DeploymentService deploymentService = serviceProvider.GetRequiredService<DeploymentService>();
        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        DeploymentTranslations translations = new DeploymentTranslations();

        //Context.LogAction(ActionLogType.Teleport, deployable.Translate(_translationService));

        if (e.Player.Component<DeploymentComponent>().CurrentDeployment != null)
        {
            return;
        }

        int delay = fobConfig.GetValue("FobDeployDelay", 5);

        chatService.Send(e.Player, new DeploymentTranslations().DeployStandby, d.Deployable, delay);
        deploymentService.TryStartDeployment(e.Player, d.Deployable,
            new DeploySettings
            {
                Delay = TimeSpan.FromSeconds(delay),
                AllowNearbyEnemies = false
            }
        );
        chatService.Send(e.Player, new DeploymentTranslations().DeploySuccess, d.Deployable);

        e.Cancel();
    }
}
