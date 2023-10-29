using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Locations;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
public sealed class PolygonZone : Zone
{
    private const float Spacing = 10f;

    private readonly Vector2[] _points;
    private readonly Line[] _lines;
    /// <returns>Top left corner of bounds rectangle</returns>
    public static Vector4 GetBounds(Vector2[] points)
    {
        float? maxX = null, maxY = null, minX = null, minY = null;
        if (points.Length == 1)
        {
            return new Vector4(points[0].x, points[0].y, points[0].x, points[0].y);
        }
        for (int i = 0; i < points.Length; i++)
        {
            ref Vector2 point = ref points[i];
            if (!maxX.HasValue || maxX.Value < point.x) maxX = point.x;
            if (!maxY.HasValue || maxY.Value < point.y) maxY = point.y;
            if (!minX.HasValue || minX.Value > point.x) minX = point.x;
            if (!minY.HasValue || minY.Value > point.y) minY = point.y;
        }
        if (maxX.HasValue && maxY.HasValue && minX.HasValue && minY.HasValue)
        {
            return new Vector4(minX.Value, minY.Value, maxX.Value, maxY.Value);
        }
        return Vector4.zero;
    }
    internal PolygonZone(in ZoneModel data) : base(in data)
    {
        _points = new Vector2[data.ZoneData.Points.Length];
        Array.Copy(data.ZoneData.Points, 0, _points, 0, _points.Length);
        if (UseMapCoordinates)
        {
            for (int i = 0; i < _points.Length; ++i)
            {
                ref Vector2 point = ref _points[i];
                if (GridLocation.LegacyMapping)
                    point = LegacyMappingFromMapPos(point.x, point.y);
                else
                {
                    Vector3 v = GridLocation.MapCoordsToWorldCoords(point);
                    point = new Vector2(v.x, v.z);
                }
            }
        }
        _lines = new Line[_points.Length];
        for (int i = 0; i < _points.Length; i++)
            _lines[i] = new Line(_points[i], _points[i == _points.Length - 1 ? 0 : i + 1]);
        GetParticleSpawnPoints(out _, out _);
        Bound = GetBounds(_points);
        BoundArea = (Bounds.z - Bounds.x) * (Bounds.w - Bounds.y);
        SucessfullyParsed = true;
    }
    public static void CalculateParticleSpawnPoints(out Vector2[] points, Vector2[] corners, float spacing = Spacing, Line[]? lines = null)
    {
        List<Vector2> rtnSpawnPoints = new List<Vector2>(64);
        if (lines == null)
        {
            lines = new Line[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                lines[i] = new Line(corners[i], corners[i == corners.Length - 1 ? 0 : i + 1]);
        }
        for (int i1 = 0; i1 < lines.Length; i1++)
        {
            ref Line line = ref lines[i1];
            if (line.Length == 0) continue;
            float distance = line.NormalizeSpacing(spacing);
            if (distance != 0) // prevent infinite loops
            {
                for (float i = distance; i < line.Length; i += distance)
                {
                    rtnSpawnPoints.Add(line.GetPointFromP1(i));
                }
            }
        }
        points = rtnSpawnPoints.ToArray();
    }
    public static void CalculateParticleSpawnPoints(out Vector2[] points, List<Vector2> corners, float spacing = Spacing, Line[]? lines = null)
    {
        List<Vector2> rtnSpawnPoints = new List<Vector2>(64);
        if (lines == null)
        {
            lines = new Line[corners.Count];
            for (int i = 0; i < corners.Count; i++)
                lines[i] = new Line(corners[i], corners[i == corners.Count - 1 ? 0 : i + 1]);
        }
        for (int i1 = 0; i1 < lines.Length; i1++)
        {
            ref Line line = ref lines[i1];
            if (line.Length == 0) continue;
            float distance = line.NormalizeSpacing(spacing);
            if (distance != 0) // prevent infinite loops
            {
                for (float i = distance; i < line.Length; i += distance)
                {
                    rtnSpawnPoints.Add(line.GetPointFromP1(i));
                }
            }
        }
        points = rtnSpawnPoints.ToArray();
    }

    /// <inheritdoc/>
    public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center)
    {
        corners = _points;
        center = Center;
        if (ParticleSpawnPoints != null) return ParticleSpawnPoints;
        CalculateParticleSpawnPoints(out ParticleSpawnPoints, _points, Spacing, _lines);
        return ParticleSpawnPoints;
    }


    /// <inheritdoc/>
    
    // Stolen from
    // https://javedali-iitkgp.medium.com/get-closest-point-on-a-polygon-23b68e26a33
    public override Vector2 GetClosestPointOnBorder(Vector2 location)
    {
        if (_points.Length <= 1)
            return Center;

        float minSqrDist = Single.NaN;
        Vector2 closestPoint = default;
        for (int i = 0; i < _points.Length; ++i)
        {
            Vector2 pt1 = _points[i];
            Vector2 pt2 = _points[i != _points.Length - 1 ? i + 1 : 0];

            Vector2 a = location - pt1;
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

            float sqrDist = (location - point).sqrMagnitude;
            if (float.IsNaN(minSqrDist) || sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                closestPoint = point;
            }
        }

        return closestPoint;
    }

    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        if (!IsInsideBounds(location)) return false;
        int intersects = 0;
        for (int i = 0; i < _lines.Length; i++)
        {
            ref Line line = ref _lines[i];
            if (line.IsIntersecting(location.x, location.y))
                intersects++;
        }

        return intersects % 2 == 1;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        if (!IsInsideBounds(location)) return false;
        int intersects = 0;
        for (int i = 0; i < _lines.Length; i++)
        {
            ref Line line = ref _lines[i];
            if (line.IsIntersecting(location.x, location.z))
                ++intersects;
        }

        return intersects % 2 == 1; // is odd
    }
    /// <inheritdoc/>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder($"{base.ToString()}\n");
        for (int i = 0; i < _lines.Length; i++)
        {
            sb.Append($"Line {i + 1}: ({_lines[i].Point1.x}, {_lines[i].Point1.y}) to ({_lines[i].Point2.x}, {_lines[i].Point2.y}).\n");
        }
        return sb.ToString();
    }
    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = base.Builder;
            zb.ZoneData.X = Center.x;
            zb.ZoneData.Z = Center.y;
            zb.ZoneData.Points = _points;
            return zb;
        }
    }
}
