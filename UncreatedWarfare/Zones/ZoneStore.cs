using SDG.Framework.Landscapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Stores a full list of active zones.
/// </summary>
public class ZoneStore : IHostedService, IEarlyLevelHostedService
{
    private readonly List<IZoneProvider> _zoneProviders;
    private int _init;
    private readonly IPlayerService _playerService;
    private readonly ILogger<ZoneStore> _logger;
    private readonly WarfareModule _warfare;

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

    public ZoneStore(IEnumerable<IZoneProvider> zoneProviders, IPlayerService playerService, ILogger<ZoneStore> logger, bool isGlobal, WarfareModule warfare)
    {
        _zoneProviders = zoneProviders.ToList();
        _playerService = playerService;
        _logger = logger;
        _warfare = warfare;
        IsGlobal = isGlobal;

        Zones = Array.Empty<Zone>();
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (Level.isLoaded && _init == 0)
        {
            await Initialize(token);
        }
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public UniTask EarlyLoadLevelAsync(CancellationToken token)
    {
        if (_init == 0)
            return Initialize(token);

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
    }

    /// <summary>
    /// Creates a new <see cref="IProximity"/> depending on the type of zone defined.
    /// </summary>
    /// <returns>The proximity, which has collision events.</returns>
    /// <exception cref="ArgumentException">This zone doesn't have a valid shape or is missing the associated data object.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public ITrackingProximity<WarfarePlayer> CreateColliderForZone(Zone zone)
    {
        GameThread.AssertCurrent();

        // avoid making GameObject if it'll error
        if (zone.Shape is not ZoneShape.AABB and not ZoneShape.Cylinder and not ZoneShape.Sphere and not ZoneShape.Polygon
            || zone is { Shape: ZoneShape.AABB, AABBInfo: null }
            || zone is { Shape: ZoneShape.Cylinder or ZoneShape.Sphere, CircleInfo: null }
            || zone is { Shape: ZoneShape.Polygon, PolygonInfo: null })
        {
            throw new ArgumentException($"This zone ({zone.Name}) doesn't have a valid shape or is missing the associated data object.", nameof(zone));
        }

        GameObject obj = new GameObject(zone.Name)
        {
            layer = LayerMasks.CLIP
        };

        ColliderProximity prox = obj.AddComponent<ColliderProximity>();
        prox.Initialize(
            CreateProximityForZone(zone),
            _playerService,
            leaveGameObjectAlive: false
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
        IProximity prox;
        Vector3 center = zone.Center;
        switch (zone.Shape)
        {
            case ZoneShape.AABB when zone.AABBInfo is { } rect:
                prox = new AABBProximity(center, rect.Size);
                break;

            case ZoneShape.Cylinder when zone.CircleInfo is { } circle:
                float minHeight = circle.MinimumHeight ?? -Landscape.TILE_HEIGHT / 2f;
                float maxHeight = circle.MaximumHeight ?? Landscape.TILE_HEIGHT / 2f;
                prox = new AACylinderProximity(center with { y = minHeight + maxHeight / 2f }, circle.Radius, maxHeight - minHeight);
                break;

            case ZoneShape.Sphere when zone.CircleInfo is { } circle:
                prox = new SphereProximity(center, circle.Radius);
                break;

            case ZoneShape.Polygon when zone.PolygonInfo is { } polygon:
                Vector2[] relativePoints = polygon.Points;
                Vector2[] absolutePoints = new Vector2[relativePoints.Length];
                for (int i = 0; i < relativePoints.Length; ++i)
                {
                    ref Vector2 relativePt = ref relativePoints[i];
                    absolutePoints[i] = new Vector2(relativePt.x + center.x, relativePt.y + center.z);
                }
                prox = new PolygonProximity(absolutePoints, polygon.MinimumHeight, polygon.MaximumHeight);
                break;

            default:
                throw new ArgumentException($"This zone ({zone.Name}) doesn't have a valid shape or is missing the associated data object.", nameof(zone));
        }

        return prox;
    }

    /// <summary>
    /// Finds the closest zone to a point based on it's border.
    /// </summary>
    public Zone? FindClosestZone(in Vector3 point, ZoneType? type = null)
    {
        return FindClosestZone(in point, out _, type);
    }

    /// <summary>
    /// Finds the closest zone to a point based on it's border. Zones with the point inside their borders will return the square distance from the border but negative.
    /// </summary>
    public Zone? FindClosestZone(in Vector3 point, out float sqrDistance, ZoneType? type = null)
    {
        if (ProximityZones == null)
        {
            sqrDistance = float.NaN;
            return null;
        }

        float minSqrDistance = float.NaN;
        Zone? minZone = null;
        foreach (ZoneProximity proximity in ProximityZones)
        {
            if (type != null && type != proximity.Zone.Type)
                continue;

            Vector3 closestPoint = proximity.Proximity.GetNearestPointOnBorder(in point);

            float d = (point - closestPoint).sqrMagnitude * (proximity.Proximity.TestPoint(in point) ? -1 : 1);

            if (minZone != null && d >= minSqrDistance)
                continue;

            minZone = proximity.Zone;
            minSqrDistance = d;
        }

        sqrDistance = minSqrDistance;
        return minZone;
    }
    

    /// <summary>
    /// Finds the closest zone to a point based on it's border. Zones with the point inside their borders will return the square distance from the border but negative.
    /// </summary>
    public Zone? FindClosestZone(Vector2 point, out float sqrDistance, ZoneType? type = null)
    {
        if (ProximityZones == null)
        {
            sqrDistance = float.NaN;
            return null;
        }

        Vector3 p = new Vector3(point.x, 0f, point.y);
        float minSqrDistance = float.NaN;
        Zone? minZone = null;
        foreach (ZoneProximity proximity in ProximityZones)
        {
            Vector3 closestPoint = proximity.Proximity.GetNearestPointOnBorder(in p);

            float d = MathUtility.SquaredDistance(in p, in closestPoint, true) * (proximity.Proximity.TestPoint(in point) ? -1 : 1);

            if (minZone != null && d >= minSqrDistance)
                continue;

            minZone = proximity.Zone;
            minSqrDistance = d;
        }

        sqrDistance = minSqrDistance;
        return minZone;
    }

    /// <summary>
    /// Enumerate through all zones that <paramref name="point"/> is inside.
    /// </summary>
    public IEnumerable<Zone> EnumerateInsideZones(Vector3 point, ZoneType? type = null)
    {
        if (ProximityZones == null)
            yield break;

        foreach (ZoneProximity proximity in ProximityZones)
        {
            if (proximity.Proximity.TestPoint(point))
                yield return proximity.Zone;
        }
    }

    /// <summary>
    /// Enumerate through all zones that <paramref name="point"/> is inside.
    /// </summary>
    public IEnumerable<Zone> EnumerateInsideZones(Vector2 point, ZoneType? type = null)
    {
        if (ProximityZones == null)
            yield break;

        foreach (ZoneProximity proximity in ProximityZones)
        {
            if (proximity.Proximity.TestPoint(point))
                yield return proximity.Zone;
        }
    }

    /// <summary>
    /// Find the first zone matching the given zone type and faction.
    /// </summary>
    public Zone? SearchZone(ZoneType type, FactionInfo? faction = null)
    {
        return faction == null
            ? Zones.FirstOrDefault(zone => zone.Type == type)
            : Zones.FirstOrDefault(zone => zone.Type == type && string.Equals(zone.Faction, faction.FactionId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Find the first zone matching the given zone type and faction.
    /// </summary>
    public bool IsInsideZone(Vector3 point, ZoneType type, FactionInfo? faction)
    {
        return faction == null
            ? EnumerateInsideZones(point).Any(zone => zone.Type == type)
            : EnumerateInsideZones(point).Any(zone => zone.Type == type && string.Equals(zone.Faction, faction.FactionId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Find the first zone matching the given zone type and faction.
    /// </summary>
    public bool IsInsideZone(Vector2 point, ZoneType type, FactionInfo? faction)
    {
        return faction == null
            ? EnumerateInsideZones(point).Any(zone => zone.Type == type)
            : EnumerateInsideZones(point).Any(zone => zone.Type == type && string.Equals(zone.Faction, faction.FactionId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Find a zone matching the given name, or <see langword="null"/>.
    /// </summary>
    public Zone? SearchZone(string term, FactionInfo? relevantFaction = null)
    {
        int index = CollectionUtility.StringIndexOf(Zones, x => x.Name, term, false);
        if (index < 0)
        {
            index = CollectionUtility.StringIndexOf(Zones, x => x.ShortName, term, false);
        }

        if (index >= 0)
            return Zones[index];

        if (term.Equals("lobby", StringComparison.InvariantCultureIgnoreCase) || term.Equals("spawn", StringComparison.InvariantCultureIgnoreCase))
            return Zones.FirstOrDefault(x => x.Type == ZoneType.Lobby) ?? Zones.FirstOrDefault(x => x.Name.Equals("Lobby", StringComparison.OrdinalIgnoreCase));

        if (term.Equals("main", StringComparison.InvariantCultureIgnoreCase) && relevantFaction != null)
        {
            return Zones.FirstOrDefault(x => x.Type == ZoneType.MainBase && string.Equals(x.Faction, relevantFaction.FactionId, StringComparison.OrdinalIgnoreCase));
        }
        if (term.Equals("amc", StringComparison.InvariantCultureIgnoreCase) && relevantFaction != null)
        {
            return Zones.FirstOrDefault(x => x.Type == ZoneType.AntiMainCampArea && string.Equals(x.Faction, relevantFaction.FactionId, StringComparison.OrdinalIgnoreCase));
        }

        // tXmain
        if (term.Length > 5
            && term[0] is 't' or 'T'
            && char.IsDigit(term[1])
            && term.EndsWith("main", StringComparison.InvariantCultureIgnoreCase)
            && ulong.TryParse(term.AsSpan(1, term.Length - 5), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong teamGroupId)
            && _warfare.IsLayoutActive())
        {
            Team team = _warfare.GetActiveLayout().TeamManager.GetTeam(new CSteamID(teamGroupId));
            if (team.IsValid)
                return Zones.FirstOrDefault(x => x.Type == ZoneType.MainBase && string.Equals(x.Faction, team.Faction.FactionId, StringComparison.OrdinalIgnoreCase));
        }

        // tXamc
        if (term.Length > 4
            && term[0] is 't' or 'T'
            && char.IsDigit(term[1])
            && term.EndsWith("amc", StringComparison.InvariantCultureIgnoreCase)
            && ulong.TryParse(term.AsSpan(1, term.Length - 4), NumberStyles.Number, CultureInfo.InvariantCulture, out teamGroupId)
            && _warfare.IsLayoutActive())
        {
            Team team = _warfare.GetActiveLayout().TeamManager.GetTeam(new CSteamID(teamGroupId));
            if (team.IsValid)
                return Zones.FirstOrDefault(x => x.Type == ZoneType.AntiMainCampArea && string.Equals(x.Faction, team.Faction.FactionId, StringComparison.OrdinalIgnoreCase));
        }

        if (!_warfare.IsLayoutActive())
            return null;

        // todo lookup obj1, obj2, and obj for active objectives
        // ILayoutPhase? phase = _warfare.GetActiveLayout().ActivePhase;
        return null;
    }

    /// <summary>
    /// Get a zone where the given point is inside it. Smaller area zones will be returned before larger ones.
    /// </summary>
    /// <param name="pos">The position to search for.</param>
    /// <param name="noOverlap">If <see langword="null"/> should be returned if more than one zone match.</param>
    public Zone? FindInsideZone(Vector3 pos, bool noOverlap)
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
            {
                return null;
            }
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
    public Zone? FindInsideZone(Vector2 pos, bool noOverlap)
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
}