using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Proximity;
public static class ProximityExtensions
{
    /// <summary>
    /// Attach a proximity to an <paramref name="attachmentRoot"/> so it follows all transformations made to it.
    /// </summary>
    public static IAttachedProximity AttachTo(this IProximity proximity, Transform attachmentRoot)
    {
        if (proximity is not IAttachableProximity<IAttachedProximity> proxNearestPointImpl)
            throw new NotSupportedException($"Can not calculate the nearest point for a {Accessor.ExceptionFormatter.Format(proximity.GetType())}, it must implement {Accessor.ExceptionFormatter.Format(typeof(INearestPointProximity))}.");

        return proxNearestPointImpl.CreateAttachedProximity(attachmentRoot);
    }

    /// <summary>
    /// Get the nearest point on a proximity's border. Some proximities may throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Can not caluclate the nearest point for <paramref name="proximity"/>.</exception>
    public static Vector3 GetNearestPointOnBorder(this IProximity proximity, Vector3 fromLocation)
    {
        if (proximity is not INearestPointProximity proxNearestPointImpl)
            throw new NotSupportedException($"Can not calculate the nearest point for a {Accessor.ExceptionFormatter.Format(proximity.GetType())}, it must implement {Accessor.ExceptionFormatter.Format(typeof(INearestPointProximity))}.");

        return proxNearestPointImpl.GetNearestPointOnBorder(fromLocation);
    }

    /// <summary>
    /// Get the nearest point on the surface of a sphere to <paramref name="fromLocation"/>.
    /// </summary>
    public static Vector3 GetNearestPointOnBorder(ISphereProximity proximity, Vector3 fromLocation)
    {
        BoundingSphere sphere = proximity.Sphere;
        Vector3 diff = sphere.position - fromLocation;
        float distance = Math.Max(MathF.Sqrt(diff.x * diff.x + diff.y * diff.y + diff.z * diff.z), float.Epsilon);
        float radius = sphere.radius;
        return new Vector3(diff.x / distance * radius, diff.y / distance * radius, diff.z / distance * radius);
    }

    /// <summary>
    /// Get the nearest point on the surface of an axis-aligned bounding box to <paramref name="fromLocation"/>.
    /// </summary>
    public static Vector3 GetNearestPointOnBorder(IAABBProximity proximity, Vector3 fromLocation)
    {
        Bounds bounds = proximity.Dimensions;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        int gx = GappedSign(fromLocation.x, min.x, max.x);
        int gy = GappedSign(fromLocation.y, min.y, max.y);
        int gz = GappedSign(fromLocation.z, min.z, max.z);

        if (gx == 0 && gy == 0 && gz == 0)
        {
            // point is inside the rectangle, find closest side
            float distMaxX = max.x - fromLocation.x;
            float distMinX = fromLocation.x - min.x;
            float distMaxY = max.y - fromLocation.y;
            float distMinY = fromLocation.y - min.y;
            float distMaxZ = max.z - fromLocation.z;
            float distMinZ = fromLocation.z - min.z;

            if (distMaxX <= distMinX && distMaxX <= distMaxY && distMaxX <= distMinY && distMaxX <= distMaxZ && distMaxX < distMinZ)
                return new Vector3(max.x, fromLocation.y, fromLocation.z);
            
            if (distMinX <= distMaxX && distMinX <= distMaxY && distMinX <= distMinY && distMinX <= distMaxZ && distMinX < distMinZ)
                return new Vector3(min.x, fromLocation.y, fromLocation.z);
            
            if (distMaxY <= distMinX && distMaxY <= distMaxX && distMaxY <= distMinY && distMaxY <= distMaxZ && distMaxY < distMinZ)
                return new Vector3(fromLocation.x, max.y, fromLocation.z);
            
            if (distMinY <= distMaxX && distMinY <= distMaxY && distMinY <= distMinX && distMinY <= distMaxZ && distMinY < distMinZ)
                return new Vector3(fromLocation.x, min.y, fromLocation.z);
            
            if (distMaxZ <= distMinX && distMaxZ <= distMaxX && distMaxZ <= distMinY && distMaxZ <= distMaxY && distMaxZ < distMinZ)
                return new Vector3(fromLocation.x, fromLocation.y, max.z);

            // distMinZ is lowest
            return new Vector3(fromLocation.x, fromLocation.y, min.z);
        }

        Vector3 rtn = default;

        rtn.x = gx switch
        {
            -1 => min.x,
            1 => max.x,
            _ => fromLocation.x
        };
        rtn.y = gy switch
        {
            -1 => min.y,
            1 => max.y,
            _ => fromLocation.y
        };
        rtn.z = gz switch
        {
            -1 => min.z,
            1 => max.z,
            _ => fromLocation.z
        };
        
        return rtn;
    }

    // like Math.Sign but the area that returns zero is the values between min and max.
    private static int GappedSign(float value, float min, float max)
    {
        if (value > max)
            return 1;

        if (value < min)
            return -1;

        return 0;
    }

    /// <summary>
    /// Get the nearest point on the surface of a 2D polygon to <paramref name="fromLocation"/>.
    /// </summary>
    public static Vector3 GetNearestPointOnBorder(IPolygonProximity proximity, Vector3 fromLocation)
    {
        float? minHeight = proximity.MinHeight, maxHeight = proximity.MaxHeight;
        if (minHeight.HasValue && fromLocation.y < minHeight.Value)
            fromLocation.y = minHeight.Value;
        else if (maxHeight.HasValue && fromLocation.y > maxHeight.Value)
            fromLocation.y = maxHeight.Value;

        // Closest Point on a Polygon by Javed Ali: https://javedali-iitkgp.medium.com/get-closest-point-on-a-polygon-23b68e26a33
        IReadOnlyList<Vector2> pts = proximity.Points;
        int len = pts.Count;
        if (len == 0)
            return fromLocation;

        if (len == 1)
            return pts[0];

        Vector2 from2d = new Vector2(fromLocation.x, fromLocation.z);
        float minSqrDist = float.NaN;
        Vector2 closestPoint = default;
        for (int i = 0; i < len; ++i)
        {
            Vector2 pt1 = pts[i];
            Vector2 pt2 = pts[i != len - 1 ? i + 1 : 0];

            Vector2 a = from2d - pt1;
            Vector2 b = pt2 - pt1;

            float dot = Vector2.Dot(a, b);
            float sqrMagnitude = b.sqrMagnitude;

            float alpha = dot / sqrMagnitude;
            Vector2 point;
            if (alpha < 0)
                point = pt1;
            else if (alpha > 1)
                point = pt2;
            else
                point = new Vector2(pt1.x + alpha * b.x, pt1.y + alpha * b.y);

            float sqrDist = (from2d - point).sqrMagnitude;
            if (!float.IsNaN(minSqrDist) && sqrDist >= minSqrDist)
                continue;

            minSqrDist = sqrDist;
            closestPoint = point;
        }

        return new Vector3(closestPoint.x, fromLocation.y, closestPoint.y);
    }

    /// <summary>
    /// Get the nearest point on the surface of an axis-aligned cylinder to <paramref name="fromLocation"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Axis is not X, Y, or Z in <paramref name="proximity"/>.</exception>
    public static Vector3 GetNearestPointOnBorder(IAACylinderProximity proximity, Vector3 fromLocation)
    {
        Vector3 center = proximity.Center;
        SnapAxis axis = proximity.Axis;

        float radius = proximity.Radius;
        float sqrRadius = radius * radius;

        float height = proximity.Height;
        bool infinite = !float.IsFinite(height);
        height /= 2;

        float relX = fromLocation.x - center.x,
              relY = fromLocation.y - center.y,
              relZ = fromLocation.z - center.z;

        bool inHeightRange = infinite || axis switch
        {
            SnapAxis.X => relX >= -height && relX <= height,
            SnapAxis.Y => relY >= -height && relY <= height,
            SnapAxis.Z => relZ >= -height && relZ <= height,
            _ => throw new ArgumentException("Proximity axis is not X, Y, or Z.", nameof(proximity))
        };

        float sqrDistance = axis switch
        {
            SnapAxis.X => relY * relY + relZ * relZ,
            SnapAxis.Y => relX * relX + relZ * relZ,
            _  /* Z */ => relX * relX + relY * relY
        };

        bool inCircle = sqrDistance <= sqrRadius;

        if (!inHeightRange)
        {
            switch (axis)
            {
                case SnapAxis.X:
                    fromLocation.x = Mathf.Clamp(fromLocation.x, center.x - height, center.x + height);
                    break;

                case SnapAxis.Y:
                    fromLocation.y = Mathf.Clamp(fromLocation.y, center.y - height, center.y + height);
                    break;

                case SnapAxis.Z:
                    fromLocation.z = Mathf.Clamp(fromLocation.z, center.z - height, center.z + height);
                    break;
            }
        }

        if (!inCircle && !inHeightRange)
            return fromLocation;

        float distance = Math.Max(MathF.Sqrt(sqrDistance), float.Epsilon);
        
        // scale by unit vector
        return axis switch
        {
            SnapAxis.X => new Vector3(fromLocation.x, relY / distance * radius, relZ / distance * radius),
            SnapAxis.Y => new Vector3(relX / distance * radius, fromLocation.y, relZ / distance * radius),
            _  /* Z */ => new Vector3(relX / distance * radius, relY / distance * radius, fromLocation.z)
        };
    }

    public static Mesh CreateMesh(this IPolygonProximity proximity, int triCount, Vector3? originOverride, out Vector3 origin)
    {
        IReadOnlyList<Vector2> pointList = proximity.Points;

        int ptCt = pointList.Count;
        if (ptCt < 3)
            throw new ArgumentException("Polygons must have at least 3 points to create a mesh.", nameof(proximity));

        bool isReversed = IsCounterclockwise(pointList);

        Vector2[] points;
        if (isReversed)
        {
            points = new Vector2[ptCt];
            for (int i = 0; i < ptCt; ++i)
            {
                points[i] = pointList[ptCt - i - 1];
            }
        }
        else if (pointList is Vector2[] ptArr)
        {
            Vector2[] pts = new Vector2[ptArr.Length];
            Array.Copy(ptArr, pts, pts.Length);
            points = pts;
        }
        else
        {
            points = new Vector2[ptCt];
            for (int i = 0; i < ptCt; ++i)
            {
                points[i] = pointList[i];
            }
        }

        const float minHeight = -2, maxHeight = 2;
        if (originOverride.HasValue)
        {
            origin = originOverride.Value;
        }
        else
        {
            origin = default;
            for (int i = 0; i < ptCt; ++i)
            {
                ref Vector2 pt = ref points[i];
                origin.x += pt.x;
                origin.z += pt.y;
            }

            origin /= ptCt;
        }

        Debug.Log($"Start generate: rev: {isReversed}.");
        Debug.Log("Origin: " + origin);

        int capTriCount = ptCt - 2;
        if (triCount < 0)
            triCount = capTriCount;
        else if (triCount > capTriCount)
            triCount = capTriCount;

        Vector3[] vertices = new Vector3[ptCt * 6];
        int[] tris = new int[ptCt * 6 + capTriCount * 6];
        Vector3[] normals = new Vector3[ptCt * 6];
        //Vector2[] uv = new Vector2[ptCt * 6]; todo fixup UVs later

        Vector2 origin2d = new Vector2(origin.x, origin.z);

        for (int i = 0; i < ptCt; ++i)
        {
            int nextIndex = (i + 1) % ptCt;

            Vector2 pt = points[i] - origin2d;
            Vector2 nextPoint = points[nextIndex] - origin2d;

            int vertStartIndex = i * 4;

            vertices[vertStartIndex] = new Vector3(pt.x, minHeight, pt.y);
            vertices[vertStartIndex + 1] = new Vector3(pt.x, maxHeight, pt.y);
            vertices[vertStartIndex + 2] = new Vector3(nextPoint.x, minHeight, nextPoint.y);
            vertices[vertStartIndex + 3] = new Vector3(nextPoint.x, maxHeight, nextPoint.y);

            Vector2 dir = (nextPoint - pt).normalized;
            Vector3 faceNormal = Vector3.Cross(new Vector3(dir.x, 0, dir.y), Vector3.up);

            normals[vertStartIndex] = faceNormal;
            normals[vertStartIndex + 1] = faceNormal;
            normals[vertStartIndex + 2] = faceNormal;
            normals[vertStartIndex + 3] = faceNormal;

            // top
            vertices[ptCt * 4 + i] = new Vector3(pt.x, maxHeight, pt.y);
            normals[ptCt * 4 + i] = Vector3.up;

            // bottom
            vertices[ptCt * 5 + i] = new Vector3(pt.x, minHeight, pt.y);
            normals[ptCt * 5 + i] = Vector3.down;

            int triStartIndex = i * 6;

            tris[triStartIndex] = vertStartIndex + 1;
            tris[triStartIndex + 1] = vertStartIndex;
            tris[triStartIndex + 2] = vertStartIndex + 2;
            tris[triStartIndex + 3] = vertStartIndex + 1;
            tris[triStartIndex + 4] = vertStartIndex + 2;
            tris[triStartIndex + 5] = vertStartIndex + 3;

            Debug.Log(faceNormal);
        }

        Array.Reverse(points);

        int triOffset = ptCt * 6;
        int triCountWritten = new PolygonTriangulationProcessor(points, ptCt * 4)
                                .WriteTriangles(new ArraySegment<int>(tris, triOffset, (ptCt - 2) * 3), triCount);

        // flip triangles for bottom
        for (int i = 0; i < triCountWritten; ++i)
        {
            int toIndex = triOffset + triCountWritten * 3 + i * 3;
            int fromIndex = triOffset + i * 3;

            tris[fromIndex] = ptCt - (tris[fromIndex] - ptCt * 4) + ptCt * 4 - 1;
            tris[fromIndex + 1] = ptCt - (tris[fromIndex + 1] - ptCt * 4) + ptCt * 4 - 1;
            tris[fromIndex + 2] = ptCt - (tris[fromIndex + 2] - ptCt * 4) + ptCt * 4 - 1;

            tris[toIndex] = tris[fromIndex + 2] + ptCt;
            tris[toIndex + 1] = tris[fromIndex + 1] + ptCt;
            tris[toIndex + 2] = tris[fromIndex] + ptCt;
        }

        Mesh mesh = new Mesh
        {
            name = "Polygon[" + ptCt + "]",
            vertices = vertices,
            triangles = new ArraySegment<int>(tris, 0, triOffset + triCountWritten * 6).ToArray(),
            normals = normals
        };

        return mesh;
    }

    // http://www.faqs.org/faqs/graphics/algorithms-faq/ Subject 2.07
    private static bool IsCounterclockwise(IReadOnlyList<Vector2> points)
    {
        int ptCt = points.Count;
        if (ptCt < 3)
            throw new ArgumentException("Polygons must have at least 3 points to create a mesh.", "proximity");

        // find most bottom left point, guaranteed to be on the convex hull.
        Vector2 minPt = points[0];
        int minPtIndex = 0;
        for (int i = 1; i < ptCt; ++i)
        {
            Vector2 pt = points[i];
            if (pt.y < minPt.y || pt.y == minPt.y && pt.x < minPt.x)
            {
                minPt = pt;
                minPtIndex = i;
            }
        }

        Vector2 pt1 = points[(minPtIndex == 0 ? ptCt : minPtIndex) - 1],
            pt3 = points[(minPtIndex + 1) % ptCt];

        Vector3 crx = Vector3.Cross(pt1 - minPt, pt3 - minPt);

        return crx.z < 0;
    }
}