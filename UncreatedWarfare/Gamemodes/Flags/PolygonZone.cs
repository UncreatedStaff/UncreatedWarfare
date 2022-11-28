using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
public sealed class PolygonZone : Zone
{
    private readonly Vector2[] Points;
    private readonly Line[] Lines;
    private const float SPACING = 10f;
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
    internal PolygonZone(ref ZoneModel data) : base(ref data)
    {
        Points = new Vector2[data.ZoneData.Points.Length];
        Array.Copy(data.ZoneData.Points, 0, Points, 0, Points.Length);
        if (UseMapCoordinates)
        {
            for (int i = 0; i < Points.Length; ++i)
            {
                ref Vector2 point = ref Points[i];
                point = FromMapCoordinates(point);
            }
        }
        Lines = new Line[Points.Length];
        for (int i = 0; i < Points.Length; i++)
            Lines[i] = new Line(Points[i], Points[i == Points.Length - 1 ? 0 : i + 1]);
        GetParticleSpawnPoints(out _, out _);
        _bounds = GetBounds(Points);
        _boundArea = (Bounds.z - Bounds.x) * (Bounds.w - Bounds.y);
        SucessfullyParsed = true;
    }
    public static void CalculateParticleSpawnPoints(out Vector2[] points, Vector2[] corners, float spacing = SPACING, Line[]? lines = null)
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
    public static void CalculateParticleSpawnPoints(out Vector2[] points, List<Vector2> corners, float spacing = SPACING, Line[]? lines = null)
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
        corners = Points;
        center = Center;
        if (_particleSpawnPoints != null) return _particleSpawnPoints;
        CalculateParticleSpawnPoints(out _particleSpawnPoints, Points, SPACING, Lines);
        return _particleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        if (!IsInsideBounds(location)) return false;
        int intersects = 0;
        for (int i = 0; i < Lines.Length; i++)
        {
            if (Lines[i].IsIntersecting(location.x, location.y)) intersects++;
        }
        if (intersects % 2 == 1) return true; // is odd
        else return false;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        if (!IsInsideBounds(location)) return false;
        int intersects = 0;
        for (int i = 0; i < Lines.Length; i++)
        {
            if (Lines[i].IsIntersecting(location.x, location.z)) intersects++;
        }
        if (intersects % 2 == 1) return true; // is odd
        else return false;
    }
    /// <inheritdoc/>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder($"{base.ToString()}\n");
        for (int i = 0; i < Lines.Length; i++)
        {
            sb.Append($"Line {i + 1}: ({Lines[i].Point1.x}, {Lines[i].Point1.y}) to ({Lines[i].Point2.x}, {Lines[i].Point2.y}).\n");
        }
        return sb.ToString();
    }
    protected override DrawData GenerateDrawData()
    {
        DrawData d = new DrawData()
        {
            Center = ToMapCoordinates(Center),
            Lines = new Line[this.Points.Length]
        };
        for (int i = 0; i < Points.Length; i++)
            d.Lines[i] = new Line(ToMapCoordinates(Points[i]), ToMapCoordinates(Points[i == Points.Length - 1 ? 0 : i + 1]));
        Vector2 b1 = ToMapCoordinates((Vector2)_bounds);
        Vector2 b2 = ToMapCoordinates(new Vector2(_bounds.z, _bounds.w));
        d.Bounds = new Vector4(b1.x, b1.y, b2.x, b2.y);
        return d;
    }
    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = new ZoneBuilder()
            {
                ZoneType = EZoneType.POLYGON,
                MinHeight = MinHeight,
                MaxHeight = MaxHeight,
                Name = Name,
                ShortName = ShortName,
                Adjacencies = Data.Adjacencies,
                Id = Id,
                UseMapCoordinates = false,
                X = Center.x,
                Z = Center.y,
                UseCase = Data.UseCase
            };
            zb.ZoneData.Points = Points;
            return zb;
        }
    }
}
