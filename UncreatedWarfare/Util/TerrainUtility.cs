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
}
