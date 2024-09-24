using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util.Region;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for level objects.
/// </summary>
public static class LevelObjectUtility
{
    /// <summary>
    /// Enumerate through all level objects around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects()
    {
        GameThread.AssertCurrent();

        byte pos = (byte)(Regions.WORLD_SIZE / 2);
        return new ObjectIterator(pos, pos);
    }

    /// <summary>
    /// Enumerate through all level objects around the center of the level.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(Vector3 center)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new ObjectIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all level objects around the center of the level.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(Vector3 center, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        if (!Regions.tryGetCoordinate(center, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new ObjectIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all level objects around the given <paramref name="region"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(RegionCoord region)
    {
        GameThread.AssertCurrent();

        return new ObjectIterator(region.x, region.y);
    }

    /// <summary>
    /// Enumerate through all level objects around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(byte x, byte y)
    {
        GameThread.AssertCurrent();

        return new ObjectIterator(x, y);
    }

    /// <summary>
    /// Enumerate through all level objects around the given <paramref name="region"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(RegionCoord region, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new ObjectIterator(region.x, region.y, maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through all level objects around the region <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectIterator EnumerateObjects(byte x, byte y, byte maxRegionDistance)
    {
        GameThread.AssertCurrent();

        return new ObjectIterator(x, y, maxRegionDistance);
    }

    /// <summary>
    /// Safely get the position of the object in case the main model didn't load.
    /// </summary>
    public static Vector3 GetPosition(LevelObject @object)
    {
        if (@object.transform is not null)
        {
            return @object.transform.position;
        }

        if (@object.placeholderTransform is not null)
        {
            return @object.placeholderTransform.position;
        }

        if (@object.skybox is not null)
        {
            return @object.skybox.position;
        }

        if (@object.interactable is not null)
        {
            return @object.interactable.transform.position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Find a object by it's instance ID.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo FindObject(uint instanceId)
    {
        return FindObject(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a object by it's Unity <see cref="Transform"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo FindObject(Transform transform)
    {
        GameThread.AssertCurrent();

        if (transform == null)
            return default;

        if (!Regions.tryGetCoordinate(transform.position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord region = iterator.Current;
            List<LevelObject> objRegion = LevelObjects.objects[region.x, region.y];
            int ct = objRegion.Count;
            for (int i = 0; i < ct; ++i)
            {
                if (ReferenceEquals(objRegion[i].transform, transform))
                {
                    return new ObjectInfo(objRegion[i], i, region);
                }
            }
        }

        iterator.Reset();
        while (iterator.MoveNext())
        {
            RegionCoord region = iterator.Current;
            List<LevelObject> objRegion = LevelObjects.objects[region.x, region.y];
            int ct = objRegion.Count;
            for (int i = 0; i < ct; ++i)
            {
                if (ReferenceEquals(objRegion[i].skybox, transform))
                {
                    return new ObjectInfo(objRegion[i], i, region);
                }
                if (ReferenceEquals(objRegion[i].placeholderTransform, transform))
                {
                    return new ObjectInfo(objRegion[i], i, region);
                }
            }
        }

        return new ObjectInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Find a object by it's instance ID, with help from a position to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found near the expected position.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo FindObject(uint instanceId, Vector3 expectedPosition)
    {
        return Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y)
            ? FindObject(instanceId, x, y)
            : FindObject(instanceId, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
    }

    /// <summary>
    /// Find a object by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo FindObject(uint instanceId, byte expectedRegionX, byte expectedRegionY)
    {
        GameThread.AssertCurrent();

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedRegionX, expectedRegionY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                if (objects[i].instanceID == instanceId)
                    return new ObjectInfo(objects[i], i, coord);
            }
        }

        return new ObjectInfo(null, -1, new RegionCoord(expectedRegionX, expectedRegionY));
    }

    /// <summary>
    /// Find a object by it's instance ID, with help from an expected region to prevent having to search every region.
    /// </summary>
    /// <remarks>All regions will be searched if it's not found in the expected region.</remarks>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo FindObject(uint instanceId, IAssetLink<ObjectAsset> expectedAsset, Vector3 expectedPosition)
    {
        GameThread.AssertCurrent();

        ObjectInfo foundByPosition = default;

        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions(expectedPosition);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];

                if (@object.instanceID == instanceId && expectedAsset.MatchAsset(@object.asset))
                    return new ObjectInfo(@object, i, coord);

                Vector3 pos = GetPosition(@object);
                if (!pos.IsNearlyEqual(expectedPosition, 0.1f) || !expectedAsset.MatchAsset(@object.asset))
                    continue;

                // if not found or the one found is farther from the expected point than this one
                if (foundByPosition.Object == null
                    || (GetPosition(foundByPosition.Object) - expectedPosition).sqrMagnitude > (pos - expectedPosition).sqrMagnitude)
                {
                    foundByPosition = new ObjectInfo(@object, i, coord);
                }
            }
        }

        if (foundByPosition.Object != null)
        {
            return foundByPosition;
        }

        if (!Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        return new ObjectInfo(null, -1, new RegionCoord(x, y));
    }

    /// <summary>
    /// Check for a nearby object with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsObjectInRange(Vector3 position, float radius, IAssetLink<ObjectAsset> asset, bool horizontalDistanceOnly = false)
    {
        return GetClosestObjectInRange(position, radius, asset, horizontalDistanceOnly).Object != null;
    }

    /// <summary>
    /// Check for a nearby object matching a predicate to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsObjectInRange(Vector3 position, float radius, Predicate<LevelObject> objectSelector, bool horizontalDistanceOnly = false)
    {
        return GetClosestObjectWhere(position, radius, objectSelector, horizontalDistanceOnly).Object != null;
    }

    /// <summary>
    /// Check for a nearby object to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static bool IsObjectInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        return GetClosestObjectInRange(position, radius, horizontalDistanceOnly).Object != null;
    }

    /// <summary>
    /// Find the closest object with the given <paramref name="asset"/> to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObjectInRange(Vector3 position, float radius, IAssetLink<ObjectAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ObjectInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !asset.MatchAsset(@object.asset))
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest object with the given <paramref name="asset"/> to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObject(Vector3 position, IAssetLink<ObjectAsset> asset, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        ObjectInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !asset.MatchAsset(@object.asset))
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest object to <paramref name="position"/> within the given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObjectInRange(Vector3 position, float radius, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ObjectInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius)
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest object to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObject(Vector3 position, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        ObjectInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist)
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest object matching a predicate to <paramref name="position"/> within a given <paramref name="radius"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObjectWhere(Vector3 position, float radius, Predicate<LevelObject> objectSelector, bool horizontalDistanceOnly = false)
    {
        if (objectSelector == null)
            throw new ArgumentNullException(nameof(objectSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        float sqrRadius = radius * radius;
        ObjectInfo closest = default;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || sqrDist > sqrRadius || !objectSelector(@object))
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Find the closest object matching a predicate to <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static ObjectInfo GetClosestObjectWhere(Vector3 position, Predicate<LevelObject> objectSelector, bool horizontalDistanceOnly = false)
    {
        if (objectSelector == null)
            throw new ArgumentNullException(nameof(objectSelector));

        GameThread.AssertCurrent();

        float closestSqrDist = 0f;
        ObjectInfo closest = default;
        if (!Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            x = y = (byte)(Regions.WORLD_SIZE / 2);
        }

        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(x, y);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > closestSqrDist || !objectSelector(@object))
                    continue;

                closest = new ObjectInfo(@object, i, coord);
                closestSqrDist = sqrDist;
            }
        }

        return closest;
    }

    /// <summary>
    /// Count the number of objects in the given <paramref name="radius"/> matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountObjectsWhere(Vector3 position, float radius, Predicate<LevelObject> objectSelector, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (objectSelector == null)
            throw new ArgumentNullException(nameof(objectSelector));

        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalObjectsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !objectSelector(@object))
                    continue;

                ++totalObjectsFound;
                if (max >= 0 && totalObjectsFound >= max)
                {
                    return totalObjectsFound;
                }
            }
        }

        return totalObjectsFound;
    }

    /// <summary>
    /// Count the number of objects in the given radius matching a predicate.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountObjectsWhere(Predicate<LevelObject> objectSelector, int max = -1)
    {
        if (objectSelector == null)
            throw new ArgumentNullException(nameof(objectSelector));

        GameThread.AssertCurrent();

        int totalObjectsFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                if (!objectSelector(@object))
                    continue;

                ++totalObjectsFound;
                if (max >= 0 && totalObjectsFound >= max)
                {
                    return totalObjectsFound;
                }
            }
        }

        return totalObjectsFound;
    }

    /// <summary>
    /// Count the number of objects in the given <paramref name="radius"/> matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountObjectsInRange(Vector3 position, float radius, IAssetLink<ObjectAsset> asset, int max = -1, bool horizontalDistanceOnly = false)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalObjectsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius || !asset.MatchAsset(@object.asset))
                    continue;

                ++totalObjectsFound;
                if (max >= 0 && totalObjectsFound >= max)
                {
                    return totalObjectsFound;
                }
            }
        }

        return totalObjectsFound;
    }

    /// <summary>
    /// Count the number of objects in the given radius matching an <paramref name="asset"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountObjects(IAssetLink<ObjectAsset> asset, int max = -1)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset));

        GameThread.AssertCurrent();

        int totalObjectsFound = 0;
        SurroundingRegionsIterator iterator = RegionUtility.EnumerateRegions();
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                if (!asset.MatchAsset(@object.asset))
                    continue;

                ++totalObjectsFound;
                if (max >= 0 && totalObjectsFound >= max)
                {
                    return totalObjectsFound;
                }
            }
        }

        return totalObjectsFound;
    }

    /// <summary>
    /// Count the number of objects in the given radius.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    [Pure]
    public static int CountObjectsInRange(Vector3 position, float radius, int max = -1, bool horizontalDistanceOnly = false)
    {
        GameThread.AssertCurrent();

        float sqrRadius = radius * radius;
        int totalObjectsFound = 0;
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(position, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            List<LevelObject> objects = LevelObjects.objects[coord.x, coord.y];
            for (int i = 0; i < objects.Count; ++i)
            {
                LevelObject @object = objects[i];
                Vector3 pos = GetPosition(@object);

                float sqrDist = MathUtility.SquaredDistance(in position, in pos, horizontalDistanceOnly);

                if (sqrDist > sqrRadius)
                    continue;

                ++totalObjectsFound;
                if (max >= 0 && totalObjectsFound >= max)
                {
                    return totalObjectsFound;
                }
            }
        }

        return totalObjectsFound;
    }
}

/// <summary>
/// Stores return information about a object including it's region information.
/// </summary>
/// <remarks>Only valid for one frame, shouldn't be stored for longer than that.</remarks>
public readonly struct ObjectInfo
{
#nullable disable
    public LevelObject Object { get; }
#nullable restore
    public bool HasValue => Object != null;

    /// <summary>
    /// Coordinates of the region the object is in, if it's not on a vehicle.
    /// </summary>
    public RegionCoord Coord { get; }

    /// <summary>
    /// Index of the object in it's region's object list.
    /// </summary>
    public int Index { get; }

    public ObjectInfo(LevelObject? @object, int index, RegionCoord coord)
    {
        Object = @object;
        Coord = coord;
        Index = index;
    }

    [Pure]
    public List<LevelObject> GetRegion()
    {
        if (Object == null)
            throw new NullReferenceException("This info doesn't store a valid LevelObject instance.");

        RegionCoord regionCoord = Coord;
        return LevelObjects.objects[regionCoord.x, regionCoord.y];
    }
}