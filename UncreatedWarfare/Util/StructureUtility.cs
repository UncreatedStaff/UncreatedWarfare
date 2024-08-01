using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for structures.
/// </summary>
public static class StructureUtility
{
    /// <summary>
    /// Enumerate through all structures around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureIterator EnumerateStructures()
    {
        ThreadUtil.assertIsGameThread();

        return new StructureIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Enumerate through all structures around <paramref name="center"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureIterator EnumerateStructures(Vector3 center)
    {
        ThreadUtil.assertIsGameThread();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new StructureIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all structures around region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureIterator EnumerateStructures(byte x, byte y)
    {
        ThreadUtil.assertIsGameThread();

        return new StructureIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all structures around <paramref name="region"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureIterator EnumerateStructures(RegionCoord region)
    {
        ThreadUtil.assertIsGameThread();

        return new StructureIterator(region.x, region.y);
    }

    /// <summary>
    /// Find a structure by it's instance ID.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo FindStructure(uint instanceId)
    {
        return FindStructure(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a structure by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo FindStructure(uint instanceId, Vector3 expectedPosition)
    {
        return Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y)
            ? FindStructure(instanceId, x, y)
            : FindStructure(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a structure by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo FindStructure(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        ThreadUtil.assertIsGameThread();

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedRegionX, expectedRegionY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                if (drops[i].instanceID == instanceId)
                    return new StructureInfo(drops[i], i, coord);
            }
        }

        return new StructureInfo(null, -1, new RegionCoord(expectedRegionX, expectedRegionY));
    }

    /// <summary>
    /// Find a structure by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region. Only instance ID is checked on planted structures.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo FindStructure(uint instanceId, IAssetLink<ItemStructureAsset> expectedAsset, Vector3 expectedPosition)
    {
        ThreadUtil.assertIsGameThread();

        StructureInfo foundByPosition = default;

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedPosition);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                if (drop.instanceID == instanceId && expectedAsset.MatchAsset(drop.asset))
                    return new StructureInfo(drop, i, coord);

                Vector3 pos = drop.GetServersideData().point;
                if (!pos.IsNearlyEqual(expectedPosition, 0.1f) || !expectedAsset.MatchAsset(drop.asset))
                    continue;

                // if not found or the one found is farther from the expected point than this one
                if (foundByPosition.Drop == null
                    || (foundByPosition.Drop.GetServersideData().point - expectedPosition).sqrMagnitude > (pos - expectedPosition).sqrMagnitude)
                {
                    foundByPosition = new StructureInfo(drop, i, coord);
                }
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

        return new StructureInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Check for a nearby structure with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureInRange(position, radius, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby structure matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureWhere(position, radius, structureSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby structure to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureInRange(position, radius, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby structure with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureInRange(position, radius, group, asset, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby structure matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, ulong group, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureWhere(position, radius, group, structureSelector, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Check for a nearby structure to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsStructureInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        return GetClosestStructureInRange(position, radius, group, horizontalDistanceOnly).Drop != null;
    }

    /// <summary>
    /// Find the closest structure with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureInRange(Vector3 position, float radius, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure with the given <paramref name="asset"/> to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructure(Vector3 position, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureInRange(Vector3 position, float radius, ulong group, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure with the given <paramref name="asset"/> to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructure(Vector3 position, ulong group, IAssetLink<ItemStructureAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;
                
                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(drop.asset))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructure(Vector3 position, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure to <paramref name="position"/> within the given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureInRange(Vector3 position, float radius, ulong group, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructure(Vector3 position, ulong group, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();
     
        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureWhere(Vector3 position, float radius, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !structureSelector(drop))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure matching a predicate to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureWhere(Vector3 position, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !structureSelector(drop))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/> and <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureWhere(Vector3 position, float radius, ulong group, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        StructureInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !structureSelector(drop))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest structure matching a predicate to <paramref name="position"/> with the given <paramref name="group"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static StructureInfo GetClosestStructureWhere(Vector3 position, ulong group, Predicate<StructureDrop> structureSelector, bool horizontalDistanceOnly = false)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        float closestSqrDist = 0f;
        StructureInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                StructureData data = drop.GetServersideData();

                if (data.group != group)
                    continue;

                float sqrDist = MathUtility.SquaredDistance(in position, in data.point, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !structureSelector(drop))
                    continue;

                closest = new StructureInfo(drop, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Count the number of structures in the given <paramref name="radius"/> matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountStructuresWhere(Vector3 position, float radius, Predicate<StructureDrop> structureSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalStructuresFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !structureSelector(drop))
                    continue;

                ++totalStructuresFound;
                if (max >= 0 && totalStructuresFound >= max)
                {
                    return totalStructuresFound;
                }
            }
        }

        return totalStructuresFound;
    }

    /// <summary>
    /// Count the number of structures in the given radius matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountStructuresWhere(Predicate<StructureDrop> structureSelector, int max = -1)
    {
        if (structureSelector == null)
            throw new ArgumentNullException(nameof(structureSelector));

        ThreadUtil.assertIsGameThread();

        int totalStructuresFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                if (!structureSelector(drop))
                    continue;

                ++totalStructuresFound;
                if (max >= 0 && totalStructuresFound >= max)
                {
                    return totalStructuresFound;
                }
            }
        }

        return totalStructuresFound;
    }

    /// <summary>
    /// Count the number of structures in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountStructuresInRange(Vector3 position, float radius, IAssetLink<ItemStructureAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalStructuresFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchAsset(drop.asset))
                    continue;

                ++totalStructuresFound;
                if (max >= 0 && totalStructuresFound >= max)
                {
                    return totalStructuresFound;
                }
            }
        }

        return totalStructuresFound;
    }

    /// <summary>
    /// Count the number of structures in the given radius matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountStructures(IAssetLink<ItemStructureAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        ThreadUtil.assertIsGameThread();

        int totalStructuresFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];
                if (!asset.MatchAsset(drop.asset))
                    continue;

                ++totalStructuresFound;
                if (max >= 0 && totalStructuresFound >= max)
                {
                    return totalStructuresFound;
                }
            }
        }

        return totalStructuresFound;
    }

    /// <summary>
    /// Count the number of structures in the given radius.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountStructuresInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        ThreadUtil.assertIsGameThread();

        float sqrRadius = radius * radius;
        int totalStructuresFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<StructureDrop> drops = StructureManager.regions[coord.x, coord.y].drops;
            for (int i = 0; i < drops.Count; ++i)
            {
                StructureDrop drop = drops[i];

                float sqrDist = MathUtility.SquaredDistance(in position, in drop.GetServersideData().point, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalStructuresFound;
                if (max >= 0 && totalStructuresFound >= max)
                {
                    return totalStructuresFound;
                }
            }
        }

        return totalStructuresFound;
    }
}

/// <summary>
/// Stores return information about a structure including it's region information.
/// </summary>
/// <remarks>Only valid for one frame, shouldn't be stored for longer than that.</remarks>
public readonly struct StructureInfo
{
#nullable disable
    public StructureDrop Drop { get; }
#nullable restore
    public bool HasValue => Drop != null;

    /// <summary>
    /// Coordinates of the region the structure is in, if it's not on a vehicle.
    /// </summary>
    public RegionCoord Coord { get; }

    /// <summary>
    /// Index of the structure in it's region's drop list.
    /// </summary>
    public int Index { get; }

    public StructureInfo(StructureDrop? drop, int index, RegionCoord coord)
    {
        Drop = drop;
        Coord = coord;
        Index = index;
    }

    [Pure]
    public StructureRegion GetRegion()
    {
        if (Drop == null)
            throw new NullReferenceException("This info doesn't store a valid StructureDrop instance.");

        RegionCoord regionCoord = Coord;
        return StructureManager.regions[regionCoord.x, regionCoord.y];
    }
}