namespace Uncreated.Warfare.Util;
public static class TerrainUtility
{
    /// <summary>
    /// Perform a raycast from the sky to find the 'ground'.
    /// </summary>
    /// <remarks>This could hit a building, etc.</remarks>
    public static float GetHighestPoint(in Vector2 point, float minHeight)
    {
        float height;
        if (Physics.Raycast(new Ray(new Vector3(point.x, Level.HEIGHT, point.y), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
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
        if (Physics.Raycast(new Ray(new Vector3(point.x, Level.HEIGHT, point.z), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            height = hit.point.y;
            return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
        }

        height = LevelGround.getHeight(point);
        return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
    }

    /// <summary>
    /// Returns the distance from a <paramref name="point"/> to the 'ground' by performing a raycast.
    /// </summary>
    /// <remarks>The raycast could hit a building, etc. If the raycast doesn't hit anything, <see cref="Level.HEIGHT"/> will be returned.</remarks>
    public static float GetDistanceToGround(in Vector3 point)
    {
        if (Physics.Raycast(new Ray(point, Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            return hit.distance;
        }

        return Level.HEIGHT;
    }
}
