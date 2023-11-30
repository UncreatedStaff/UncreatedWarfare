using System;
using System.Collections.Generic;
using Uncreated.Warfare.Locations;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public sealed class RectZone : Zone
{
    private const float Spacing = 10f;
    public Vector2 Size => _size;

    private readonly Vector2 _size;
    private readonly Line[] _lines;
    private readonly Vector2[] _corners;

    /// <inheritdoc/>
    internal RectZone(in ZoneModel data) : base(in data)
    {
        if (data.UseMapCoordinates)
        {
            _size = new Vector2(GridLocation.MapDistanceToWorldDistanceX(data.ZoneData.SizeX), GridLocation.MapDistanceToWorldDistanceY(data.ZoneData.SizeZ));
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
    public override Vector2 GetClosestPointOnBorder(Vector2 location)
    {
        int gx, gy;

        float top = Center.y + _size.y / 2f;
        float bot = Center.y - _size.y / 2f;
        float right = Center.x + _size.x / 2f;
        float left = Center.x - _size.x / 2f;

        if (location.y > top)
            gy = 1;
        else if (location.y < bot)
            gy = -1;
        else
            gy = 0;

        if (location.x > right)
            gx = 1;
        else if (location.x < left)
            gx = -1;
        else
            gx = 0;

        // inside rect
        if (gx == 0 && gy == 0)
        {
            float distTop = top - location.y;
            float distBot = location.y - bot;
            float distRight = right - location.x;
            float distLeft = location.x - left;

            if (distTop <= distBot && distTop <= distRight && distTop <= distLeft)
                return new Vector2(location.x, top);
            
            if (distBot <= distTop && distBot <= distRight && distBot <= distLeft)
                return new Vector2(location.x, bot);
            
            if (distRight <= distBot && distRight <= distTop && distRight <= distLeft)
                return new Vector2(right, location.y);

            // distLeft is lowest
            return new Vector2(left, location.y);
        }

        if (gx == 1)
        {
            if (gy == 1)
                return new Vector2(right, top);
            if (gy == -1)
                return new Vector2(right, bot);

            return new Vector2(right, location.y);
        }
        if (gx == -1)
        {
            if (gy == 1)
                return new Vector2(left, top);
            if (gy == -1)
                return new Vector2(left, bot);

            return new Vector2(left, location.y);
        }

        if (gy == 1)
            return new Vector2(location.x, top);
        
        // gy == -1
        return new Vector2(location.x, bot);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()} Size: {_size.x}x{_size.y}";
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
