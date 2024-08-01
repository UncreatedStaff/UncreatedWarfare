using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for barricades.
/// </summary>
public static class BarricadeUtility
{
    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around the center of the level, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades()
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(Vector3 center)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(Vector3 center, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(byte x, byte y)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(x, y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(RegionCoord region)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(region.x, region.y, true, true);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(byte x, byte y, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(x, y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>, then planted barricades (barricades on a vehicle).
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateBarricades(RegionCoord region, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(region.x, region.y, true, true, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades()
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(Vector3 center)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="center"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(Vector3 center, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeIterator(x, y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(byte x, byte y)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(x, y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(RegionCoord region)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(region.x, region.y, true, false);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(byte x, byte y, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(x, y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through non-planted barricades (barricades not on a vehicle) around <paramref name="region"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumerateNonPlantedBarricades(RegionCoord region, byte maxRegionDistance)
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator(region.x, region.y, true, false, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through planted barricades (barricades on a vehicle).
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeIterator EnumeratePlantedBarricades()
    {
        ThreadUtil.assertIsGameThread();

        return new BarricadeIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), false, true);
    }

    /// <summary>
    /// Find a barricade by it's instance ID.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId)
    {
        return FindBarricade(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, Vector3 expectedPosition)
    {
        return Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y)
            ? FindBarricade(instanceId, x, y)
            : FindBarricade(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        ThreadUtil.assertIsGameThread();

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedRegionX, expectedRegionY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, coord);
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, (ushort)r);
            }
        }
        
        return new BarricadeInfo(null, -1, new RegionCoord(expectedRegionX, expectedRegionY));
    }

    /// <summary>
    /// Find a barricade by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region. Only instance ID is checked on planted barricades.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo FindBarricade(uint instanceId, IAssetLink<ItemBarricadeAsset> expectedAsset, Vector3 expectedPosition)
    {
        ThreadUtil.assertIsGameThread();

        BarricadeInfo foundByPosition = default;

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedPosition);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                if (drop.instanceID == instanceId && expectedAsset.MatchAsset(drop.asset))
                    return new BarricadeInfo(drop, i, coord);

                Vector3 pos = drop.GetServersideData().point;
                if (!pos.IsNearlyEqual(expectedPosition, 0.1f) || !expectedAsset.MatchAsset(drop.asset))
                    continue;

                // if not found or the one found is farther from the expected point than this one
                if (foundByPosition.Drop == null
                    || (foundByPosition.Drop.GetServersideData().point - expectedPosition).sqrMagnitude > (pos - expectedPosition).sqrMagnitude)
                {
                    foundByPosition = new BarricadeInfo(drop, i, coord);
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new BarricadeInfo(drops[i], i, (ushort)r);
            }
        }

        if (foundByPosition.Drop != null)
        {
            return foundByPosition;
        }

        if (!Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new BarricadeInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Check for a nearby barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, group, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeWhere(position, radius, group, barricadeSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby barricade to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsBarricadeInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        return GetClosestBarricadeInRange(position, radius, group, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();
                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade with the given <paramref name="asset"/> to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, ulong group, IAssetLink<ItemBarricadeAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();
                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricade(Vector3 position, ulong group, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, float radius, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        BarricadeInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest barricade matching a predicate to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static BarricadeInfo GetClosestBarricadeWhere(Vector3 position, ulong group, Predicate<BarricadeDrop> barricadeSelector, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        BarricadeInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                BarricadeData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !barricadeSelector(drop))
                    continue;

                closest = new BarricadeInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Count the number of barricades in the given <paramref name="radius"/> matching a predicate.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesWhere(Vector3 position, float radius, Predicate<BarricadeDrop> barricadeSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesWhere(Predicate<BarricadeDrop> barricadeSelector, int max = -1)
    {
        if (barricadeSelector == null)
            throw new ArgumentNullException(nameof(barricadeSelector));

        ThreadUtil.assertIsGameThread();

        int totalBarricadesFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!barricadeSelector(drop))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <remarks>Planted barricades are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesInRange(Vector3 position, float radius, IAssetLink<ItemBarricadeAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricades(IAssetLink<ItemBarricadeAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        int totalBarricadesFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        IReadOnlyList<VehicleBarricadeRegion> vRegions = BarricadeManager.vehicleRegions;
        int ct = Math.Min(ushort.MaxValue - 1, vRegions.Count);
        for (int r = 0; r < ct; ++r)
        {
            List<BarricadeDrop> drops = vRegions[r].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];
                if (!asset.MatchAsset(drop.asset))
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }

    /// <summary>
    /// Count the number of barricades in the given radius.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountBarricadesInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalBarricadesFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<BarricadeDrop> drops = BarricadeManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                BarricadeDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalBarricadesFound;
                if (max >= 0 && totalBarricadesFound >= max)
                {
                    return totalBarricadesFound;
                }
            }
        }

        return totalBarricadesFound;
    }
}

/// <summary>
/// Stores return information about a barricade including it's region information.
/// </summary>
/// <remarks>Only valid for one frame, shouldn't be stored for longer than that.</remarks>
public readonly struct BarricadeInfo
{
#nullable disable
    public BarricadeDrop Drop { get; }
#nullable restore
    public bool HasValue => Drop != null;

    /// <summary>
    /// Coordinates of the region the barricade is in, if it's not on a vehicle.
    /// </summary>
    public RegionCoord Coord { get; }

    /// <summary>
    /// Index of the barricade in it's region's drop list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>.
    /// </summary>
    public ushort Plant { get; }
    public bool IsOnVehicle => Plant != ushort.MaxValue;

    public BarricadeInfo(BarricadeDrop? drop, int index, RegionCoord coord)
    {
        Drop = drop;
        Coord = coord;
        Index = index;
        Plant = ushort.MaxValue;
    }
    public BarricadeInfo(BarricadeDrop? drop, int index, ushort plant)
    {
        Drop = drop;
        Index = index;
        Plant = plant;
    }

    [Pure]
    public BarricadeRegion GetRegion()
    {
        if (Drop == null)
            throw new NullReferenceException("This info doesn't store a valid BarricadeDrop instance.");

        if (Plant != ushort.MaxValue)
            return BarricadeManager.vehicleRegions[Plant];

        RegionCoord regionCoord = Coord;
        return BarricadeManager.regions[regionCoord.x, regionCoord.y];
    }
}