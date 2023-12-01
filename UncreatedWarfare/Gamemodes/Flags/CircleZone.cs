using System;
using System.Collections.Generic;
using Uncreated.Warfare.Locations;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public sealed class CircleZone : Zone
{
    private const float Spacing = 18f; // every 5 degrees
    private readonly float _radius;
    /// <summary>
    /// Radius of the circle zone.
    /// </summary>
    public float Radius => _radius;
    /// <inheritdoc/>
    internal CircleZone(in ZoneModel data) : base(in data)
    {
        if (data.UseMapCoordinates)
        {
            _radius = GridLocation.MapDistanceToWorldDistanceX(data.ZoneData.Radius);
        }
        else
        {
            _radius = data.ZoneData.Radius;
        }
        GetParticleSpawnPoints(out _, out _);
        float r2 = _radius * 2;
        BoundArea = r2 * r2;
        Bound = new Vector4(Center.x - _radius, Center.y - _radius, Center.x + _radius, Center.y + _radius);
        SucessfullyParsed = true;
    }
    public static void CalculateParticleSpawnPoints(out Vector2[] points, float radius, Vector2 center, float spacing = Spacing)
    {
        float pi2F = 2f * Mathf.PI;
        float circumference = pi2F * radius;
        float answer = circumference / spacing;
        int remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * spacing);
        int canfit = (int)Mathf.Floor(answer);
        if (remainder != 0)
        {
            if (remainder < Spacing / 2)            // extend all others
                spacing = circumference / canfit;
            else                                    // add one more and subtend all others
                spacing = circumference / ++canfit;
        }
        List<Vector2> pts = new List<Vector2>(canfit + 1);
        float angleRad = spacing / radius;
        for (float i = 0; i < pi2F; i += angleRad)
        {
            pts.Add(new Vector2(center.x + Mathf.Cos(i) * radius, center.y + Mathf.Sin(i) * radius));
        }
        points = pts.ToArray();
    }
    /// <inheritdoc/>
    public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center)
    {
        corners = Array.Empty<Vector2>();
        center = Center;
        if (ParticleSpawnPoints != null) return ParticleSpawnPoints;
        CalculateParticleSpawnPoints(out ParticleSpawnPoints, _radius, Center);
        return ParticleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        if (!IsInsideBounds(location)) return false;
        float difX = location.x - Center.x;
        float difY = location.y - Center.y;
        float sqrDistance = (difX * difX) + (difY * difY);
        return sqrDistance <= _radius * _radius;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        if (!IsInsideBounds(location)) return false;
        float difX = location.x - Center.x;
        float difY = location.z - Center.y;
        float sqrDistance = (difX * difX) + (difY * difY);
        return sqrDistance <= _radius * _radius;
    }

    /// <inheritdoc/>
    public override Vector2 GetClosestPointOnBorder(Vector2 location)
    {
        Vector2 relative = location - Center;

        return relative * (Radius / relative.magnitude) + Center;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()} Radius: {_radius}";

    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = base.Builder;
            zb.ZoneData.X = Center.x;
            zb.ZoneData.Z = Center.y;
            zb.ZoneData.Radius = _radius;
            return zb;
        }
    }
}