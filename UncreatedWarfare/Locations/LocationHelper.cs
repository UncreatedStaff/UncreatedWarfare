using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Locations;

public static class LocationHelper
{
    /// <summary>
    /// Finds the closest <see cref="LocationDevkitNode"/> to the given <paramref name="point"/>.
    /// </summary>
    public static string GetClosestLocationName(Vector3 point)
    {
        LocationDevkitNode? node = null;
        float smallest = 0f;
        foreach (LocationDevkitNode existingNode in LocationDevkitNodeSystem.Get().GetAllNodes())
        {
            Vector3 nodePos = existingNode.transform.position;
            float dist = MathUtility.SquaredDistance(in point, in nodePos, true);
            if (dist >= smallest && node is not null)
                continue;

            node = existingNode;
            smallest = dist;
        }

        return node == null ? new GridLocation(in point).ToString() : node.locationName;
    }
}