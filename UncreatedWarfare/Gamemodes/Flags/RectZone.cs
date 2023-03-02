using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public sealed class RectZone : Zone
{
    private const float Spacing = 10f;

    private readonly Vector2 _size;
    private readonly Line[] _lines;
    private readonly Vector2[] _corners;

    /// <inheritdoc/>
    internal RectZone(in ZoneModel data) : base(in data)
    {
        if (data.UseMapCoordinates)
        {
            _size = new Vector2(FromMapCoordinates(data.ZoneData.SizeX), FromMapCoordinates(data.ZoneData.SizeZ));
        }
        else
        {
            _size = new Vector2(data.ZoneData.SizeX, data.ZoneData.SizeZ);
        }
        _corners = new Vector2[]
        {
            new Vector2(Center.x - _size.x / 2, Center.y - _size.y / 2), // tl
            new Vector2(Center.x + _size.x / 2, Center.y - _size.y / 2), // tr
            new Vector2(Center.x + _size.x / 2, Center.y + _size.y / 2), // br
            new Vector2(Center.x - _size.x / 2, Center.y + _size.y / 2)  // bl
        };
        Bound = new Vector4(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
        BoundArea = _size.x * _size.y;
        _lines = new Line[]
        {
            new Line(_corners[0], _corners[1]), // tl -> tr
            new Line(_corners[1], _corners[2]), // tr -> br
            new Line(_corners[2], _corners[3]), // br -> bl
            new Line(_corners[3], _corners[0]), // bl -> tl
        };
        GetParticleSpawnPoints(out _, out _);
        SucessfullyParsed = true;
    }
    /// <param name="corners">Only populated if <see cref="_lines"/> <see langword="is null"/></param>
    public static void CalculateParticleSpawnPoints(out Vector2[] points, out Vector2[] corners, Vector2 size, Vector2 center, float spacing = Spacing, Line[]? lines = null)
    {
        List<Vector2> rtnSpawnPoints = new List<Vector2>(64);
        if (lines != null)
            corners = Array.Empty<Vector2>();
        else
            corners = new Vector2[]
            {
                new Vector2(center.x - size.x / 2, center.y - size.y / 2), // tl
                new Vector2(center.x + size.x / 2, center.y - size.y / 2), // tr
                new Vector2(center.x + size.x / 2, center.y + size.y / 2), // br
                new Vector2(center.x - size.x / 2, center.y + size.y / 2)  // bl
            };
        lines ??= new Line[]
        {
            new Line(corners[0], corners[1]), // tl -> tr
            new Line(corners[1], corners[2]), // tr -> br
            new Line(corners[2], corners[3]), // br -> bl
            new Line(corners[3], corners[0]), // bl -> tl
        };
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
        corners = _corners;
        center = Center;
        if (ParticleSpawnPoints != null) return ParticleSpawnPoints;
        CalculateParticleSpawnPoints(out ParticleSpawnPoints, out _, _size, Center, Spacing, _lines);
        return ParticleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        return location.x > Center.x - _size.x / 2 && location.x < Center.x + _size.x / 2 && location.y > Center.y - _size.y / 2 && location.y < Center.y + _size.y / 2;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        return (float.IsNaN(MinHeight) || location.y >= MinHeight) && (float.IsNaN(MaxHeight) || location.y <= MaxHeight) &&
               location.x > Center.x - _size.x / 2 && location.x < Center.x + _size.x / 2 && location.z > Center.y - _size.y / 2 && location.z < Center.y + _size.y / 2;
    }
    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()} Size: {_size.x}x{_size.y}";
    protected override DrawData GenerateDrawData()
    {
        DrawData d = new DrawData
        {
            Center = ToMapCoordinates(Center),
            Size = new Vector2(ToMapCoordinates(_size.x), ToMapCoordinates(_size.y)),
            Lines = new Line[this._lines.Length],
            Bounds = new Vector4(ToMapCoordinates(Bound.x), ToMapCoordinates(Bound.y), ToMapCoordinates(Bound.z), ToMapCoordinates(Bound.w))
        };
        for (int i = 0; i < _lines.Length; ++i)
        {
            d.Lines[i] = new Line(ToMapCoordinates(_lines[i].Point1), ToMapCoordinates(_lines[i].Point2));
        }
        d.Bounds = new Vector4(d.Center.x - d.Size.x / 2, d.Center.y - d.Size.y / 2, d.Center.x + d.Size.x / 2, d.Center.y + d.Size.y / 2);
        return d;
    }
    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = base.Builder;
            zb.ZoneData.X = Center.x;
            zb.ZoneData.Z = Center.y;
            zb.ZoneData.SizeX = _size.x;
            zb.ZoneData.SizeZ = _size.y;
            return zb;
        }
    }
}
