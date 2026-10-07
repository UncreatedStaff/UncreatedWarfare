using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.StrategyMaps;

public class StrategyMap : IDisposable, IEventListener<ClaimBedRequested>
{
    private static readonly List<RegionCoordinate> MapTackSearchBuffer = new List<RegionCoordinate>(4);

    private readonly MapTableInfo _tableInfo;
    private readonly BuildableAttributesDataStore _attributeStore;
    internal readonly List<MapTackInfo> ActiveMapTacks;

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
        ActiveMapTacks = new List<MapTackInfo>();
        Position = buildable.Position;
    }

    internal void DestroyOldMapTacks()
    {
        foreach (IBuildable buildable in EnumerateBuildablesOnSurface())
        {
            if (ActiveMapTacks.Exists(t => t.Tack.Marker.Equals(buildable)))
                continue;

            WarfareModule.Singleton.GlobalLogger.LogTrace($"Destroying unregistered map tack: {buildable.Asset.name}.");
            buildable.Destroy();
        }
    }

    /// <summary>
    /// Enumerates all buildables placed on this map's plane based on their position, irrespective to whether or not they're registered as map tacks.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public IEnumerable<IBuildable> EnumerateBuildablesOnSurface()
    {
        GameThread.AssertCurrent();

        Vector3 offset = new Vector3(0f, _tableInfo.VerticalSurfaceOffset, 0f);
        Vector3 scale;
        scale.x = _tableInfo.MapTableSquareWidth / 2f;
        scale.y = _tableInfo.MapTableSquareWidth / -2f;
        scale.z = 0f;

        Transform model = MapTable.Model;
        Vector3 planePosition = model.position + offset;
        Matrix4x4 normalizedToBarricade = Matrix4x4.TRS(planePosition, model.rotation, scale);

        Vector3 bl = normalizedToBarricade.MultiplyPoint3x4(new Vector3(-1f, -1f, -0f));
        Vector3 tl = normalizedToBarricade.MultiplyPoint3x4(new Vector3(-1f, 1f, 0f));
        Vector3 br = normalizedToBarricade.MultiplyPoint3x4(new Vector3(1f, -1f, 0f));
        Vector3 tr = normalizedToBarricade.MultiplyPoint3x4(new Vector3(1f, 1f, 0f));

        const float sqrt2 = 1.4142135623731f;

        // distance from corner of square containing circle with radius r
        //  = sqrt(2) * r (or sqrt(2r) via pythagorean theorem)
        float radius = sqrt2 * (_tableInfo.MapTableSquareWidth / 2f);

        IProximity polygon = new PolygonProximity([
            new Vector2(bl.x, bl.z),
            new Vector2(tl.x, tl.z),
            new Vector2(tr.x, tr.z),
            new Vector2(br.x, br.z)
        ], planePosition.y - 0.25f, planePosition.y + 0.25f);

        MapTackSearchBuffer.Clear();

        Regions.getRegionsInRadius(planePosition, radius, MapTackSearchBuffer);

        foreach (RegionCoordinate coordinate in MapTackSearchBuffer)
        {
            List<BarricadeDrop> barricades = BarricadeManager.regions[coordinate.x, coordinate.y].drops;
            List<StructureDrop> structures = StructureManager.regions[coordinate.x, coordinate.y].drops;

            // loop backwards in case they're being destroyed
            for (int i = barricades.Count - 1; i >= 0; --i)
            {
                BarricadeDrop drop = barricades[i];
                if (MapTable.Equals(drop) || !polygon.TestPoint(in drop.GetServersideData().point))
                    continue;
                
                yield return new BuildableBarricade(drop);
            }

            for (int i = structures.Count - 1; i >= 0; --i)
            {
                StructureDrop drop = structures[i];
                if (MapTable.Equals(drop) || !polygon.TestPoint(in drop.GetServersideData().point))
                    continue;

                yield return new BuildableStructure(drop);
            }
        }

        MapTackSearchBuffer.Clear();
    }

    public void ClearMapTacks()
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        WarfareModule.Singleton.GlobalLogger.LogConditional($"Clearing strategy map: {Position}.");

        GameThread.AssertCurrent();

        foreach (MapTackInfo tack in ActiveMapTacks)
        {
            tack.Tack.Dispose();
        }

        ActiveMapTacks.Clear();
    }

    public void AddMapTack(MapTack newMapTack, object owner)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        Vector3 pos = newMapTack.FeatureWorldPosition;
        
        Vector3 worldCoordsOnMapTable = TranslateWorldPointOntoMap(pos);

        newMapTack.DropMarker(worldCoordsOnMapTable, MapTable.Rotation);
        _attributeStore.UpdateAttributes(newMapTack.Marker).Add(MainBaseBuildables.TransientAttribute, null);

        ActiveMapTacks.Add(new MapTackInfo(newMapTack, owner));
    }

    public bool RemoveMapTack(MapTack newMapTack)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        for (int i = 0; i < ActiveMapTacks.Count; ++i)
        {
            if (ActiveMapTacks[i].Tack != newMapTack)
                continue;

            ActiveMapTacks.RemoveAt(i);
            newMapTack.Dispose();
            return true;
        }

        return false;
    }

    public int RemoveMapTacks(Func<MapTack, bool> filter)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        for (int i = ActiveMapTacks.Count - 1; i >= 0; --i)
        {
            MapTackInfo tack = ActiveMapTacks[i];
            if (!filter(tack.Tack))
                continue;

            ActiveMapTacks.RemoveAt(i);
            tack.Tack.Dispose();
            ++ct;
        }

        return ct;
    }
    public int RemoveMapTacks(Func<MapTack, object, bool> filter)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        GameThread.AssertCurrent();

        int ct = 0;
        for (int i = ActiveMapTacks.Count - 1; i >= 0; --i)
        {
            MapTackInfo tack = ActiveMapTacks[i];
            if (!filter(tack.Tack, tack.Owner))
                continue;

            ActiveMapTacks.RemoveAt(i);
            tack.Tack.Dispose();
            ++ct;
        }

        return ct;
    }

    public Vector3 TranslateWorldPointOntoMap(Vector3 featureWorldPosition)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        Matrix4x4 matrix = ProjectWorldCoordsToMapTable(MapTable.Model, new Vector3(0, _tableInfo.VerticalSurfaceOffset, 0), new Vector2(_tableInfo.MapTableSquareWidth, _tableInfo.MapTableSquareWidth));

        return matrix.MultiplyPoint3x4(featureWorldPosition);
    }

    /// <summary>
    /// Projects a world coodinate to the world coordinate of a point on a flat 'war table' type barricade, given the x and y size and 3D offset of the table.
    /// </summary>
    public static Matrix4x4 ProjectWorldCoordsToMapTable(Transform mapTableTransform, Vector3 platformOffset, Vector2 platformSize)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

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
        using IDisposable? profiler = ProfilerUtil.Profile();

        MapTackInfo mapTack = ActiveMapTacks.FirstOrDefault(t => t.Tack.Marker.Equals(e.Barricade));
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

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        DeploymentTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<DeploymentTranslations>>().Value;

        if (!d.Deployable.AllowUnarmedDeploy)
        {
            KitPlayerComponent kitComp = e.Player.Component<KitPlayerComponent>();
            if (!kitComp.IsArmed)
            {
                chatService.Send(e.Player, translations.DeployNoKit);
                e.Cancel();
                return;
            }
        }

        DeploymentService deploymentService = serviceProvider.GetRequiredService<DeploymentService>();

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
        return $"StrategyMap[MapTable: [{MapTable}] MapTacks: {string.Join(", ", ActiveMapTacks)}]";
    }

    internal record struct MapTackInfo(MapTack Tack, object Owner);
}