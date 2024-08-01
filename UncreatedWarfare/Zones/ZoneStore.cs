using SDG.Framework.Landscapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Proximity;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Stores a full list of active zones.
/// </summary>
public class ZoneStore
{
    private readonly List<IZoneProvider> _zoneProviders;
    private int _init;

    /// <summary>
    /// All available zones (not just the ones in-game).
    /// </summary>
    public IReadOnlyList<Zone> Zones { get; private set; }

    public ZoneStore(IEnumerable<IZoneProvider> zoneProviders)
    {
        _zoneProviders = zoneProviders.ToListFast();
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

        _zoneProviders.Clear();
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
}