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
}