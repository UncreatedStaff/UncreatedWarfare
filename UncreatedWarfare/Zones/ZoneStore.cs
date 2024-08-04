using SDG.Framework.Landscapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Stores a full list of active zones.
/// </summary>
public class ZoneStore : IHostedService
{
    private readonly List<IZoneProvider> _zoneProviders;
    private int _init;
    private readonly ILogger<ZoneStore> _logger;
    private bool _lvlEventSub;
    private UniTask _loadTask;

    /// <summary>
    /// All available zones (not just the ones in-game).
    /// </summary>
    public IReadOnlyList<Zone> Zones { get; private set; }

    /// <summary>
    /// All available zones (not just the ones in-game).
    /// </summary>
    /// <remarks>This is only available on the global <see cref="ZoneStore"/>.</remarks>
    public IReadOnlyList<ZoneProximity>? ProximityZones { get; private set; }

    /// <summary>
    /// If this instance of <see cref="ZoneStore"/> is the global instance, not the one created for flag layouts.
    /// </summary>
    public bool IsGlobal { get; }

    public ZoneStore(IEnumerable<IZoneProvider> zoneProviders, ILogger<ZoneStore> logger, bool isGlobal)
    {
        _zoneProviders = zoneProviders.ToList();
        _logger = logger;
        IsGlobal = isGlobal;
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (Level.isLoaded && _init == 0)
        {
            await Initialize(token);
        }
        else
        {
            Level.loadingSteps += OnLevelLoading;
            Level.onPrePreLevelLoaded += OnLevelLoaded;
            _lvlEventSub = true;
        }
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        if (_lvlEventSub)
            Level.loadingSteps -= OnLevelLoading;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Initialize <see cref="Zones"/> from all the registered <see cref="IZoneProvider"/>.
    /// </summary>
    public async UniTask Initialize(CancellationToken token = default)
    {
        if (Interlocked.Exchange(ref _init, 1) != 0)
            throw new InvalidOperationException("Already initialized.");

        List<Zone> zones = new List<Zone>(64);
        foreach (IZoneProvider zoneProvider in _zoneProviders)
        {
            foreach (Zone zone in await zoneProvider.GetZones(token))
            {
                zones.Add(zone);
            }
        }

        Zones = new ReadOnlyCollection<Zone>(zones);
        _logger.LogInformation("Discovered {0} zone(s) with {1} provider(s).", zones.Count, _zoneProviders.Count);
        _zoneProviders.Clear();

        if (!IsGlobal)
            return;

        List<ZoneProximity> proxZones = new List<ZoneProximity>(zones.Count);
        foreach (Zone zone in zones)
        {
            proxZones.Add(new ZoneProximity(CreateProximityForZone(zone), zone));
        }

        ProximityZones = proxZones.AsReadOnly();
        _logger.LogInformation("Initialized proximities for {0} zone(s).", proxZones.Count);
    }

    /// <summary>
    /// Creates a new <see cref="IProximity"/> depending on the type of zone defined.
    /// </summary>
    /// <returns>The proximity, which has collision events.</returns>
    /// <exception cref="ArgumentException">This zone doesn't have a valid shape or is missing the associated data object.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public ITrackingProximity<Collider> CreateColliderForZone(Zone zone)
    {
        ThreadUtil.assertIsGameThread();

        // avoid making GameObject if it'll error
        if (zone.Shape is not ZoneShape.AABB and not ZoneShape.Cylinder and not ZoneShape.Sphere and not ZoneShape.Polygon
            || zone is { Shape: ZoneShape.AABB, AABBInfo: null }
            || zone is { Shape: ZoneShape.Cylinder or ZoneShape.Sphere, CircleInfo: null }
            || zone is { Shape: ZoneShape.Polygon, PolygonInfo: null })
        {
            throw new ArgumentException("This zone doesn't have a valid shape or is missing the associated data object.", nameof(zone));
        }

        GameObject obj = new GameObject(zone.Name)
        {
            layer = LayerMasks.CLIP
        };

        ColliderProximity prox = obj.AddComponent<ColliderProximity>();
        prox.Initialize(
            CreateProximityForZone(zone),
            leaveGameObjectAlive: false,
            validationCheck: collider => collider.transform.CompareTag("Player")
        );

        return prox;
    }

    /// <summary>
    /// Creates a new <see cref="IProximity"/> depending on the type of zone defined.
    /// </summary>
    /// <returns>The proximity.</returns>
    /// <exception cref="ArgumentException">This zone doesn't have a valid shape or is missing the associated data object.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public IProximity CreateProximityForZone(Zone zone)
    {
        switch (zone.Shape)
        {
            case ZoneShape.AABB when zone.AABBInfo is { } rect:
                return new AABBProximity(zone.Center, rect.Size);

            case ZoneShape.Cylinder when zone.CircleInfo is { } circle:
                float minHeight = circle.MinimumHeight ?? -Landscape.TILE_HEIGHT / 2f;
                float maxHeight = circle.MaximumHeight ?? Landscape.TILE_HEIGHT / 2f;
                return new AACylinderProximity(zone.Center with { y = minHeight + maxHeight / 2f }, circle.Radius, maxHeight - minHeight);

            case ZoneShape.Sphere when zone.CircleInfo is { } circle:
                return new SphereProximity(zone.Center, circle.Radius);

            case ZoneShape.Polygon when zone.PolygonInfo is { } polygon:
                Vector2[] points = polygon.Points;
                Vector2[] newPoints = new Vector2[points.Length];
                Array.Copy(points, newPoints, points.Length);
                return new PolygonProximity(newPoints, polygon.MinimumHeight, polygon.MaximumHeight);

            default:
                throw new ArgumentException("This zone doesn't have a valid shape or is missing the associated data object.", nameof(zone));
        }
    }

    /// <summary>
    /// Find a zone matching the given name, or <see langword="null"/>.
    /// </summary>
    public Zone? SearchZone(string term)
    {
        int index = F.StringIndexOf(Zones, x => x.Name, term, false);
        if (index < 0)
        {
            index = F.StringIndexOf(Zones, x => x.ShortName, term, false);
        }

        if (index >= 0)
            return Zones[index];

        if (term.Equals("lobby", StringComparison.OrdinalIgnoreCase) || term.Equals("spawn", StringComparison.OrdinalIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.Lobby);
        if (term.Equals("t1main", StringComparison.OrdinalIgnoreCase) || term.Equals("t1", StringComparison.OrdinalIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.MainBase && string.Equals(x.Faction, TeamManager.Team1Faction));
        if (term.Equals("t2main", StringComparison.OrdinalIgnoreCase) || term.Equals("t2", StringComparison.OrdinalIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.MainBase && string.Equals(x.Faction, TeamManager.Team2Faction));
        if (term.Equals("t1amc", StringComparison.OrdinalIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.AntiMainCampArea && string.Equals(x.Faction, TeamManager.Team1Faction));
        if (term.Equals("t2amc", StringComparison.OrdinalIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.AntiMainCampArea && string.Equals(x.Faction, TeamManager.Team2Faction));

        // todo lookup obj1, obj2, and obj
        //Flag? fl = null;
        //if (term.Equals("obj1", StringComparison.OrdinalIgnoreCase))
        //{
        //    if (Data.Is(out IFlagTeamObjectiveGamemode gm))
        //    {
        //        fl = gm.ObjectiveTeam1;
        //    }
        //}
        //else if (term.Equals("obj2", StringComparison.OrdinalIgnoreCase))
        //{
        //    if (Data.Is(out IFlagTeamObjectiveGamemode gm))
        //    {
        //        fl = gm.ObjectiveTeam2;
        //    }
        //}
        //else if (term.Equals("obj", StringComparison.OrdinalIgnoreCase))
        //{
        //    if (Data.Is(out IFlagTeamObjectiveGamemode rot))
        //    {
        //        if (Data.Is(out IAttackDefense atdef))
        //        {
        //            ulong t = atdef.DefendingTeam;
        //            fl = t == 1 ? rot.ObjectiveTeam1 : rot.ObjectiveTeam2;
        //        }
        //    }
        //    else if (Data.Is(out IFlagObjectiveGamemode obj))
        //    {
        //        fl = obj.Objective;
        //    }
        //}
        //if (fl != null)
        //    return fl.ZoneData;
        return null;
    }

    /// <summary>
    /// Get a zone where the given point is inside it. Smaller area zones will be returned before larger ones.
    /// </summary>
    /// <param name="pos">The position to search for.</param>
    /// <param name="noOverlap">If <see langword="null"/> should be returned if more than one zone match.</param>
    public Zone? FindInsizeZone(Vector3 pos, bool noOverlap)
    {
        if (ProximityZones == null)
            return null;

        Zone? current = null;
        float area = 0;
        for (int i = 0; i < ProximityZones.Count; ++i)
        {
            ZoneProximity proximity = ProximityZones[i];
            if (!proximity.Proximity.TestPoint(pos))
                continue;
            
            float a = proximity.Proximity.Area;
            if (current is null)
            {
                current = proximity.Zone;
                area = a;
            }
            else if (noOverlap)
                return null;
            else if (a < area)
            {
                current = proximity.Zone;
                area = a;
            }
        }

        return current;
    }

    /// <summary>
    /// Get a zone where the given point is inside it. Smaller area zones will be returned before larger ones.
    /// </summary>
    /// <param name="pos">The position to search for.</param>
    /// <param name="noOverlap">If <see langword="null"/> should be returned if more than one zone match.</param>
    public Zone? FindInsizeZone(Vector2 pos, bool noOverlap)
    {
        if (ProximityZones == null)
            return null;

        Zone? current = null;
        float area = 0;
        for (int i = 0; i < ProximityZones.Count; ++i)
        {
            ZoneProximity proximity = ProximityZones[i];
            if (!proximity.Proximity.TestPoint(pos))
                continue;
            
            float a = proximity.Proximity.Area;
            if (current is null)
            {
                current = proximity.Zone;
                area = a;
            }
            else if (noOverlap)
                return null;
            else if (a < area)
            {
                current = proximity.Zone;
                area = a;
            }
        }

        return current;
    }

    // start loading zones mid-way through level load and force it to finish at the end.

    /// <summary>
    /// Invoked just before the level actually loads.
    /// </summary>
    private void OnLevelLoaded(int level)
    {
        if (_loadTask.Status == UniTaskStatus.Pending)
        {
            _logger.LogWarning("Still waiting on zone reading to complete.");
            _loadTask.AsTask().Wait();
            _logger.LogInformation("Zone reading completed.");
        }
        else
        {
            _loadTask = default;
        }
    }

    /// <summary>
    /// Invoked during level load.
    /// </summary>
    private void OnLevelLoading()
    {
        _loadTask = UniTask.Create(async () =>
        {
            if (_init == 0)
                await Initialize();
        });

        if (_loadTask.Status != UniTaskStatus.Pending)
            _loadTask = default;
    }
}