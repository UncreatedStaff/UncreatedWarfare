using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public class RectZone : Zone
{
    private readonly Vector2 Size;
    private readonly Line[] lines;
    private readonly Vector2[] Corners;
    private const float SPACING = 10f;


    /// <inheritdoc/>
    internal RectZone(ref ZoneModel data) : base(ref data)
    {
        if (data.UseMapCoordinates)
        {
            Size = new Vector2(data.ZoneData.SizeX * ImageMultiplier, data.ZoneData.SizeZ * ImageMultiplier);
        }
        else
        {
            Size = new Vector2(data.ZoneData.SizeX, data.ZoneData.SizeZ);
        }
        Corners = new Vector2[4]
        {
            new Vector2(Center.x - Size.x / 2, Center.y - Size.y / 2), //tl
            new Vector2(Center.x + Size.x / 2, Center.y - Size.y / 2), //tr
            new Vector2(Center.x + Size.x / 2, Center.y + Size.y / 2), //br
            new Vector2(Center.x - Size.x / 2, Center.y + Size.y / 2)  //bl
        };
        _bounds = new Vector4(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
        _boundArea = Size.x * Size.y;
        lines = new Line[4]
        {
            new Line(Corners[0], Corners[1]), // tl -> tr
            new Line(Corners[1], Corners[2]), // tr -> br
            new Line(Corners[2], Corners[3]), // br -> bl
            new Line(Corners[3], Corners[0]), // bl -> tl
        };
        GetParticleSpawnPoints(out _, out _);
        SucessfullyParsed = true;
    }
    /// <param name="corners">Only populated if <see cref="lines"/> <see langword="is null"/></param>
    public static void CalculateParticleSpawnPoints(out Vector2[] points, out Vector2[] corners, Vector2 size, Vector2 center, float spacing = SPACING, Line[]? lines = null)
    {
        List<Vector2> rtnSpawnPoints = new List<Vector2>(64);
        if (lines != null)
            corners = Array.Empty<Vector2>();
        else
            corners = new Vector2[4]
            {
                new Vector2(center.x - size.x / 2, center.y - size.y / 2), //tl
                new Vector2(center.x + size.x / 2, center.y - size.y / 2), //tr
                new Vector2(center.x + size.x / 2, center.y + size.y / 2), //br
                new Vector2(center.x - size.x / 2, center.y + size.y / 2)  //bl
            };
        if (lines == null)
            lines = new Line[4]
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
        corners = Corners;
        center = Center;
        if (_particleSpawnPoints != null) return _particleSpawnPoints;
        CalculateParticleSpawnPoints(out _particleSpawnPoints, out _, Size, Center, SPACING, lines);
        return _particleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        return location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.y > Center.y - Size.y / 2 && location.y < Center.y + Size.y / 2;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        return (float.IsNaN(MinHeight) || location.y >= MinHeight) && (float.IsNaN(MaxHeight) || location.y <= MaxHeight) &&
               location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.z > Center.y - Size.y / 2 && location.z < Center.y + Size.y / 2;
    }
    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()} Size: {Size.x}x{Size.y}";
    protected override DrawData GenerateDrawData()
    {
        DrawData d = new DrawData()
        {
            Center = ToMapCoordinates(Center),
            Size = new Vector2(Size.x / ImageMultiplier, Size.y / ImageMultiplier),
            Lines = new Line[this.lines.Length],
            Bounds = _bounds / ImageMultiplier
        };
        for (int i = 0; i < lines.Length; ++i)
        {
            d.Lines[i] = new Line(ToMapCoordinates(lines[i].Point1), ToMapCoordinates(lines[i].Point2));
        }
        d.Bounds = new Vector4(d.Center.x - d.Size.x / 2, d.Center.y - d.Size.y / 2, d.Center.x + d.Size.x / 2, d.Center.y + d.Size.y / 2);
        return d;
    }
    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = new ZoneBuilder()
            {
                ZoneType = EZoneType.RECTANGLE,
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
            zb.ZoneData.SizeX = Size.x;
            zb.ZoneData.SizeZ = Size.y;
            return zb;
        }
    }
}
