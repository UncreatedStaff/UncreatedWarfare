using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Util;
public static class RaycastUtility
{
    private static readonly List<int> TriangleBuffer = new List<int>();
    private static readonly List<Color32> ColorBuffer = new List<Color32>();
    public static bool TryGetHitVertexColor(ref RaycastHit hit, out Color color)
    {
        ThreadUtil.assertIsGameThread();
        color = default;
        
        if (hit.collider is not MeshCollider meshCollider || meshCollider.sharedMesh == null)
            return false;

        Mesh mesh = meshCollider.sharedMesh;

        // lists are cleared by Unity
        mesh.GetTriangles(TriangleBuffer, 0);

        int triIndex = hit.triangleIndex;

        if (triIndex < 0 || triIndex * 3 + 2 >= TriangleBuffer.Count)
            return false;
        
        mesh.GetColors(ColorBuffer);

        int v1 = TriangleBuffer[triIndex * 3],
            v2 = TriangleBuffer[triIndex * 3 + 1],
            v3 = TriangleBuffer[triIndex * 3 + 2];

        if (v1 >= ColorBuffer.Count || v2 >= ColorBuffer.Count || v3 >= ColorBuffer.Count)
            return false;

        Color c1 = ColorBuffer[v1],
              c2 = ColorBuffer[v2],
              c3 = ColorBuffer[v3];

        Vector3 triangleCoords = hit.barycentricCoordinate;

        color = c1 * triangleCoords.x + c2 * triangleCoords.y + c3 * triangleCoords.z;
        return true;
    }
}
