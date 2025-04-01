using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Utilities for working with regions.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
public static class RegionUtility
{
    /// <summary>
    /// Gets the region of a position, or throws an error if it's out of range. Pass an argument name when the name is not 'position'.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void AssertGetRegion(Vector3 position, out byte x, out byte y, string argumentName)
    {
        if (!Regions.tryGetCoordinate(position, out x, out y))
            throw new ArgumentOutOfRangeException(argumentName, "Position is out of range of the region system.");
    }
    /// <summary>
    /// Gets the region of a position, or throws an error if it's out of range.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void AssertGetRegion(Vector3 position, out byte x, out byte y)
    {
        if (!Regions.tryGetCoordinate(position, out x, out y))
            throw new ArgumentOutOfRangeException(nameof(position), "Position is out of range of the region system.");
    }

    /// <summary>
    /// Enumerate through all regions around a the center of the level.
    /// </summary>
    public static SurroundingRegionsIterator EnumerateRegions() => new SurroundingRegionsIterator((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), 255);

    /// <summary>
    /// Enumerate through all regions around a given region, with a maximum distance in regions.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static SurroundingRegionsIterator EnumerateRegions(byte centerX, byte centerY, byte maxRegionDistance) => new SurroundingRegionsIterator(centerX, centerY, maxRegionDistance);

    /// <summary>
    /// Enumerate through all regions around a given region.
    /// </summary>
    public static SurroundingRegionsIterator EnumerateRegions(byte centerX, byte centerY) => new SurroundingRegionsIterator(centerX, centerY);

    /// <summary>
    /// Enumerate through all regions around a given region, with a maximum distance in regions.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static SurroundingRegionsIterator EnumerateRegions(RegionCoord center, byte maxRegionDistance) => new SurroundingRegionsIterator(center.x, center.y, maxRegionDistance);

    /// <summary>
    /// Enumerate through all regions around a given region.
    /// </summary>
    public static SurroundingRegionsIterator EnumerateRegions(RegionCoord center) => new SurroundingRegionsIterator(center.x, center.y);

    /// <summary>
    /// Enumerate through all regions around a given position in a region.
    /// </summary>
    public static SurroundingRegionsIterator EnumerateRegions(Vector3 center)
    {
        if (!Regions.tryGetCoordinate(center, out byte centerX, out byte centerY))
            centerX = centerY = (byte)(Regions.WORLD_SIZE / 2);

        return new SurroundingRegionsIterator(centerX, centerY);
    }

    /// <summary>
    /// Enumerate through all regions around a given position which contain a circle with a <paramref name="radius"/>.
    /// </summary>
    public static RadiusRegionsEnumerator EnumerateRegions(Vector3 center, float radius) => new RadiusRegionsEnumerator(new Vector2(center.x, center.z), radius);

    /// <summary>
    /// Enumerate through all regions around a given 2D position which contain a circle with a <paramref name="radius"/>.
    /// </summary>
    public static RadiusRegionsEnumerator EnumerateRegions(Vector2 center, float radius) => new RadiusRegionsEnumerator(center, radius);

    /// <summary>
    /// Linearly enumerate through all regions, but use the Y-axis as the primary axis.
    /// </summary>
    public static RegionsIterator LinearEnumerateRegions(bool yPrimary) => new RegionsIterator(yPrimary);

    /// <summary>
    /// Linearly enumerate through all regions.
    /// </summary>
    public static RegionsIterator LinearEnumerateRegions() => new RegionsIterator();

    /// <summary>
    /// Enumerate through a list of regions around a given region.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static ListRegionsEnumerator<T> CastFrom<T>(this List<T>[,] regions, RegionCoord center, byte maxRegionDistance = 255)
        => new ListRegionsEnumerator<T>(regions, center.x, center.y, maxRegionDistance);

    /// <summary>
    /// Enumerate through a list of regions around a given region.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static ListRegionsEnumerator<T> CastFrom<T>(this List<T>[,] regions, byte centerX, byte centerY, byte maxRegionDistance = 255)
        => new ListRegionsEnumerator<T>(regions, centerX, centerY, maxRegionDistance);

    /// <summary>
    /// Enumerate through a list of regions around a given position in a region.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static ListRegionsEnumerator<T> CastFrom<T>(this List<T>[,] regions, Vector3 position, byte maxRegionDistance = 255)
    {
        if (Regions.tryGetCoordinate(position, out byte x, out byte y))
            return new ListRegionsEnumerator<T>(regions, x, y, maxRegionDistance);
        
        return new ListRegionsEnumerator<T>(regions, (byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2), maxRegionDistance);
    }

    /// <summary>
    /// Enumerate through a list of regions around the center of the level.
    /// </summary>
    public static ListRegionsEnumerator<T> CastFrom<T>(this List<T>[,] regions) => new ListRegionsEnumerator<T>(regions);

    /// <summary>
    /// Invoke an action for each region around the center of the level.
    /// </summary>
    public static void ForEachRegion([InstantHandle] RegionAction action)
    {
        int worldSize = Regions.WORLD_SIZE;
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator((byte)(worldSize / 2), (byte)(worldSize / 2));
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            action(coord);
        }
    }

    /// <summary>
    /// Invoke an action for each region starting around a given region.
    /// </summary>
    public static void ForEachRegion(RegionCoord center, [InstantHandle] RegionAction action)
        => ForEachRegion(center.x, center.y, action);

    /// <summary>
    /// Invoke an action for each region starting around a given region.
    /// </summary>
    public static void ForEachRegion(byte centerX, byte centerY, [InstantHandle] RegionAction action)
    {
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(centerX, centerY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            action(coord);
        }
    }

    /// <summary>
    /// Invoke an action for each region around a given region with a radius of <paramref name="maxRegionDistance"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static void ForEachRegion(RegionCoord center, byte maxRegionDistance, [InstantHandle] RegionAction action)
        => ForEachRegion(center.x, center.y, maxRegionDistance, action);

    /// <summary>
    /// Invoke an action for each region around a given region with a radius of <paramref name="maxRegionDistance"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static void ForEachRegion(byte centerX, byte centerY, byte maxRegionDistance, [InstantHandle] RegionAction action)
    {
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(centerX, centerY, maxRegionDistance);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            action(coord);
        }
    }

    /// <summary>
    /// Invoke an action for each region starting around a given position in a region.
    /// </summary>
    public static void ForEachRegion(Vector3 center, [InstantHandle] RegionAction action)
    {
        if (!Regions.tryGetCoordinate(center, out byte centerX, out byte centerY))
            centerX = centerY = (byte)(Regions.WORLD_SIZE / 2);

        ForEachRegion(centerX, centerY, action);
    }

    /// <summary>
    /// Invoke an action for each region around a given position in a region that contain a circle with a given <paramref name="radius"/>.
    /// </summary>
    public static void ForEachRegion(Vector3 center, float radius, [InstantHandle] RegionAction action)
    {
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(center, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            action(coord);
        }
    }

    /// <summary>
    /// Invoke an action for each region around the center of the level. Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    public static void ForEachRegion([InstantHandle] RegionActionWhile action)
    {
        int worldSize = Regions.WORLD_SIZE;
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator((byte)(worldSize / 2), (byte)(worldSize / 2));
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            if (!action(coord))
                break;
        }
    }

    /// <summary>
    /// Invoke an action for each region around a given region. Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    public static void ForEachRegion(RegionCoord center, [InstantHandle] RegionActionWhile action)
        => ForEachRegion(center.x, center.y, action);

    /// <summary>
    /// Invoke an action for each region around a given region. Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    public static void ForEachRegion(byte centerX, byte centerY, [InstantHandle] RegionActionWhile action)
    {
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(centerX, centerY);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            if (!action(coord))
                break;
        }
    }

    /// <summary>
    /// Invoke an action for each region around a given region with a radius of <paramref name="maxRegionDistance"/>.
    /// Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static void ForEachRegion(RegionCoord center, byte maxRegionDistance, [InstantHandle] RegionActionWhile action)
        => ForEachRegion(center.x, center.y, maxRegionDistance, action);

    /// <summary>
    /// Invoke an action for each region around a given region with a radius of <paramref name="maxRegionDistance"/>.
    /// Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    /// <remarks>The square enumerated will have a size of <c><paramref name="maxRegionDistance"/> * 2 + 1</c> regions.</remarks>
    public static void ForEachRegion(byte centerX, byte centerY, byte maxRegionDistance, [InstantHandle] RegionActionWhile action)
    {
        SurroundingRegionsIterator iterator = new SurroundingRegionsIterator(centerX, centerY, maxRegionDistance);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            if (!action(coord))
                break;
        }
    }

    /// <summary>
    /// Invoke an action for each region around a given position in a region. Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    public static void ForEachRegion(Vector3 center, [InstantHandle] RegionActionWhile action)
    {
        if (!Regions.tryGetCoordinate(center, out byte centerX, out byte centerY))
            centerX = centerY = (byte)(Regions.WORLD_SIZE / 2);

        ForEachRegion(centerX, centerY, action);
    }

    /// <summary>
    /// Invoke an action for each region around a given position in a region that contain a circle with a given <paramref name="radius"/>.
    /// Can break within <paramref name="action"/> by returning <see langword="false"/>.
    /// </summary>
    public static void ForEachRegion(Vector3 center, float radius, [InstantHandle] RegionActionWhile action)
    {
        RadiusRegionsEnumerator iterator = new RadiusRegionsEnumerator(center, radius);
        while (iterator.MoveNext())
        {
            RegionCoord coord = iterator.Current;
            if (!action(coord))
                break;
        }
    }
}

public delegate void RegionAction(RegionCoord coord);
public delegate bool RegionActionWhile(RegionCoord coord);