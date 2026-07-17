using DanielWillett.ReflectionTools;
using SDG.Framework.Landscapes;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util;

public static class TerrainUtility
{
    private static float? _highestMapPoint;

    /// <summary>
    /// Gets the highest point on the map.
    /// </summary>
    public static float GetMapHighestPeak()
    {
        GameThread.AssertCurrent();

        if (_highestMapPoint.HasValue)
            return _highestMapPoint.Value;

        Dictionary<LandscapeCoord, LandscapeTile>? tiles = Variables.FindStatic<Landscape, Dictionary<LandscapeCoord, LandscapeTile>>("tiles")?.GetValue();

        if (tiles == null)
        {
            Accessor.Logger?.LogError(nameof(TerrainUtility), null, "Unable to find Landscape.tiles variable.");
            return 0;
        }

        if (tiles.Count == 0)
            throw new InvalidOperationException("Level not yet loaded.");

        float max = 0;
        foreach (LandscapeTile tile in tiles.Values)
        {
            int res = Landscape.HEIGHTMAP_RESOLUTION;
            for (int x = 0; x < res; ++x)
            {
                for (int y = 0; y < res; ++y)
                {
                    max = Math.Max(max, tile.heightmap[x, y]);
                }
            }
        }

        max = max * Landscape.TILE_HEIGHT - Landscape.TILE_HEIGHT / 2f;
        _highestMapPoint = max;
        return max;
    }

    /// <summary>
    /// Perform a raycast from the sky to find the 'ground'.
    /// </summary>
    /// <remarks>This could hit a building, etc.</remarks>
    public static float GetHighestPoint(in Vector2 point, float minHeight)
    {
        float height;
        if (Physics.SphereCast(new Vector3(point.x, Level.HEIGHT, point.y), PlayerStance.RADIUS + 0.01f, Vector3.down, out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION & ~(RayMasks.CLIP | RayMasks.VEHICLE), QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
        }

        height = LevelGround.getHeight(point);
        return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
    }

    /// <summary>
    /// Perform a raycast from the sky to find the 'ground'.
    /// </summary>
    /// <remarks>This could hit a building, etc.</remarks>
    public static float GetHighestPoint(in Vector3 point, float minHeight)
    {
        float height;
        if (Physics.SphereCast(new Vector3(point.x, Level.HEIGHT, point.z), PlayerStance.RADIUS + 0.01f, Vector3.down, out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION & ~(RayMasks.CLIP | RayMasks.VEHICLE), QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
        }

        height = LevelGround.getHeight(point);
        return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
    }

    /// <summary>
    /// Perform a raycast from the sky to find the 'ground'.
    /// </summary>
    /// <remarks>This could hit a building, etc.</remarks>
    public static float GetHighestPoint(in Vector3 point, float minHeight, out Vector3 normal)
    {
        float height;
        if (Physics.SphereCast(new Vector3(point.x, Level.HEIGHT, point.z), PlayerStance.RADIUS + 0.01f, Vector3.down, out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION & ~(RayMasks.CLIP | RayMasks.VEHICLE), QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            normal = hit.normal;
            return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
        }

        height = LevelGround.getHeight(point);
        normal = Vector3.up;
        return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
    }

    /// <summary>
    /// Returns the distance from a <paramref name="point"/> to the 'ground' by performing a raycast.
    /// </summary>
    /// <remarks>The raycast could hit a building, etc. If the raycast doesn't hit anything, <see cref="Level.HEIGHT"/> will be returned.</remarks>
    public static float GetDistanceToGround(in Vector3 point)
    {
        if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION & ~(RayMasks.CLIP | RayMasks.VEHICLE), QueryTriggerInteraction.Ignore))
        {
            return hit.distance;
        }

        return Level.HEIGHT;
    }

    /// <summary>
    /// Finds an open section of Y values with the given radius starting at <paramref name="point"/>'s Y value and moving around until it finds one.
    /// Will not go below the terrain. Falls back to the original value of <paramref name="point"/>'s Y value.
    /// </summary>
    public static float GetNextOpenPoint(Vector3 point, float radius, int? rayMask = null)
    {
        int mask = rayMask ?? RayMasks.BLOCK_COLLISION & ~(RayMasks.CLIP | RayMasks.VEHICLE | RayMasks.AGENT | RayMasks.PLAYER | RayMasks.ENEMY);

        float minY = LevelGround.getHeight(point) + radius;
        point.y = Math.Max(minY, point.y);
        
        bool below = false;
        bool tailEnded = false;
        float head = point.y + radius, tail = point.y - radius;
        int maxIterations = Math.Min(64, (int)Math.Round(1024 / radius));
        for (int i = 0; i < maxIterations; ++i)
        {
            float origin = below ? tail : head;
            if (!Physics.SphereCast(new Vector3(point.x, origin, point.z), radius + 0.01f, Vector3.down, out RaycastHit hit, radius * 16f, mask, QueryTriggerInteraction.Ignore)
                || hit.distance >= radius * 2f - 0.01f)
            {
                if (hit.point.y >= minY)
                {
                    return hit.point.y;
                }
                
                if (below)
                    tailEnded = true;
            }

            if (below)
                tail -= radius * 2;
            else
                head += radius * 2;

            if (tailEnded)
            {
                below = false;
                continue;
            }

            below = !below;
        }

        return point.y;
    }
}
