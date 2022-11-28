using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public sealed class CircleZone : Zone
{
    private readonly float _radius;
    /// <summary>
    /// Radius of the circle zone.
    /// </summary>
    public float Radius => _radius;
    private const float SPACING = 18f; // every 5 degrees
    /// <inheritdoc/>
    internal CircleZone(ref ZoneModel data) : base(ref data)
    {
        if (data.UseMapCoordinates)
        {
            _radius = data.ZoneData.Radius * ImageMultiplier;
        }
        else
        {
            _radius = data.ZoneData.Radius;
        }
        GetParticleSpawnPoints(out _, out _);
        float r2 = _radius * 2;
        _boundArea = r2 * r2;
        _bounds = new Vector4(Center.x - _radius, Center.y - _radius, Center.x + _radius, Center.y + _radius);
        SucessfullyParsed = true;
    }
    public static void CalculateParticleSpawnPoints(out Vector2[] points, float radius, Vector2 center, float spacing = SPACING)
    {
        float pi2F = 2f * Mathf.PI;
        float circumference = pi2F * radius;
        float answer = circumference / spacing;
        int remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * spacing);
        int canfit = (int)Mathf.Floor(answer);
        if (remainder != 0)
        {
            if (remainder < SPACING / 2)            // extend all others
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
        if (_particleSpawnPoints != null) return _particleSpawnPoints;
        CalculateParticleSpawnPoints(out _particleSpawnPoints, _radius, Center);
        return _particleSpawnPoints;
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
    public override string ToString() => $"{base.ToString()} Radius: {_radius}";
    protected override DrawData GenerateDrawData()
    {
        DrawData d = new DrawData()
        {
            Center = ToMapCoordinates(Center),
            Radius = _radius / ImageMultiplier
        };
        d.Bounds = new Vector4(d.Center.x - d.Radius, d.Center.y - d.Radius, d.Center.x + d.Radius, d.Center.y + d.Radius);
        return d;
    }

    /// <inheritdoc/>
    internal override ZoneBuilder Builder
    {
        get
        {
            ZoneBuilder zb = new ZoneBuilder()
            {
                ZoneType = EZoneType.CIRCLE,
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
            zb.ZoneData.Radius = _radius;
            return zb;
        }
    }
}