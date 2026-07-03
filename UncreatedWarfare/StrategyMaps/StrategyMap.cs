using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.StrategyMaps;

public class StrategyMap : IDisposable, IEventListener<ClaimBedRequested>
{
    private readonly MapTableInfo _tableInfo;
    private readonly BuildableAttributesDataStore _attributeStore;
    private readonly List<MapTackInfo> _activeMapTacks;

    // list of war room zones this map is in maintained by StrategyMapManager
    internal readonly HashSet<Zone> Zones = new HashSet<Zone>();

    public IBuildable MapTable { get; }

    public Vector3 Position { get; }

    public float Size => _tableInfo.MapTableSquareWidth;

    public StrategyMap(IBuildable buildable, MapTableInfo tableInfo, BuildableAttributesDataStore attributeStore)
    {
        _tableInfo = tableInfo;
        _attributeStore = attributeStore;
        MapTable = buildable;
        _activeMapTacks = new List<MapTackInfo>();
        Position = buildable.Position;
    }

    internal MapTack? TickMapTackLook(WarfarePlayer player, in Vector3 position)
    {
        MapTack? closestLookTack = null;
        float closestLookDot = 0;
        Transform pos = player.UnturnedPlayer.look.aim;
        Vector3 playerPos = pos.position;
        Vector3 playerLookVector = pos.forward;

        foreach (MapTackInfo tack in _activeMapTacks)
        {
            if (tack.Tack.UIHandler == null)
                continue;

            Vector3 lookVector = tack.Tack.Position - playerPos;
            lookVector.Normalize();

            float dot = Vector3.Dot(lookVector, playerLookVector);
            if (closestLookTack != null && closestLookDot >= dot)
                continue;

            closestLookDot = dot;
            closestLookTack = tack.Tack;
        }

        // within 35 degrees of looking at tack
        float angle = Mathf.Acos(closestLookDot);
        if (angle > 35f * Mathf.Deg2Rad)
            closestLookTack = null;

        return closestLookTack;
    }

    public void ClearMapTacks()
    {
        GameThread.AssertCurrent();

        foreach (MapTackInfo tack in _activeMapTacks)
        {
            tack.Tack.Dispose();
        }

        _activeMapTacks.Clear();
    }

    public void AddMapTack(MapTack newMapTack, object owner)
    {
        GameThread.AssertCurrent();

        Vector3 pos = newMapTack.FeatureWorldPosition;
        
        Vector3 worldCoordsOnMapTable = TranslateWorldPointOntoMap(pos);

        newMapTack.DropMarker(worldCoordsOnMapTable, MapTable.Rotation);
        _attributeStore.UpdateAttributes(newMapTack.Marker).Add(MainBaseBuildables.TransientAttribute, null);

        _activeMapTacks.Add(new MapTackInfo(newMapTack, owner));
    }

    public bool RemoveMapTack(MapTack newMapTack)
    {
        GameThread.AssertCurrent();

        for (int i = 0; i < _activeMapTacks.Count; ++i)
        {
            if (_activeMapTacks[i].Tack != newMapTack)
                continue;

            _activeMapTacks.RemoveAt(i);
            newMapTack.Dispose();
            return true;
        }

        return false;
    }

    public int RemoveMapTacks(Func<MapTack, bool> filter)
    {
        GameThread.AssertCurrent();

        int ct = 0;
        for (int i = _activeMapTacks.Count - 1; i >= 0; --i)
        {
            MapTackInfo tack = _activeMapTacks[i];
            if (!filter(tack.Tack))
                continue;

            _activeMapTacks.RemoveAt(i);
            tack.Tack.Dispose();
            ++ct;
        }

        return ct;
    }
    public int RemoveMapTacks(Func<MapTack, object, bool> filter)
    {
        GameThread.AssertCurrent();

        int ct = 0;
        for (int i = _activeMapTacks.Count - 1; i >= 0; --i)
        {
            MapTackInfo tack = _activeMapTacks[i];
            if (!filter(tack.Tack, tack.Owner))
                continue;

            _activeMapTacks.RemoveAt(i);
            tack.Tack.Dispose();
            ++ct;
        }

        return ct;
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
        MapTackInfo mapTack = _activeMapTacks.FirstOrDefault(t => t.Tack.Marker.Equals(e.Barricade));
        if (mapTack.Tack == null)
        {
            // we can't cancel action here because this will run for both strategy maps
            return;
        }

        if (mapTack.Tack is not DeployableMapTack d)
        {
            e.Cancel();
            return;
        }

        DeploymentService deploymentService = serviceProvider.GetRequiredService<DeploymentService>();
        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        DeploymentTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;

        if (e.Player.Component<DeploymentComponent>().CurrentDeployment != null)
        {
            e.Cancel();
            return;
        }
        
        if (d.Deployable is RallyPoint rallyPoint && !rallyPoint.Squad.ContainsPlayer(e.Player))
        {
            chatService.Send(e.Player, translations.DeployRallyPointWrongSquad, rallyPoint.Squad.TeamIdentificationNumber, rallyPoint.Squad);
            e.Cancel();
            return;
        }

        // override the deploy yaw to the player's rotation relative to the map
        // originally it was a bit disorienting when deploying
        Quaternion rotation = e.Player.UnturnedPlayer.look.aim.rotation;
        Quaternion buildableRotation = MapTable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

        rotation = Quaternion.Inverse(buildableRotation) * rotation;

        deploymentService.TryStartDeployment(e.Player, d.Deployable,
            new DeploySettings
            {
                // AllowNearbyEnemies is for the location the player is teleporting from, NOT to.
                YawOverride = rotation.eulerAngles.y
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

    private record struct MapTackInfo(MapTack Tack, object Owner);
}