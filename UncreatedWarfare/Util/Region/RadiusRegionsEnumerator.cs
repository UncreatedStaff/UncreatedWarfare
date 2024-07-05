using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Enumerates all regions containing a circle of a given radius.
/// </summary>
public struct RadiusRegionsEnumerator : IEnumerator<RegionCoord>, IEnumerable<RegionCoord>
{
    private RegionCoord _current;
    private SurroundingRegionsIterator _xyIterator;
    private readonly Vector2 _center;
    private readonly byte _cx, _cy;
    private readonly float _radius;
    private readonly float _sqrLookRadius;
    public RadiusRegionsEnumerator(Vector3 center, float radius) : this(new Vector2(center.x, center.z), radius) { }
    public RadiusRegionsEnumerator(Vector2 center, float radius)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        _radius = radius;
        _center = center;
        GetClampedCoordinates(center, out _cx, out _cy);

        float lookRadius = radius + Regions.WORLD_SIZE;
        _sqrLookRadius = lookRadius * lookRadius;

        float regions = radius / Regions.WORLD_SIZE / 2 + 1;

        _xyIterator = new SurroundingRegionsIterator(_cx, _cy, (byte)Math.Clamp(Math.Floor(regions), byte.MinValue, byte.MaxValue));
    }

    private RadiusRegionsEnumerator(in Vector2 center, float radius, byte cx, byte cy, float sqrLookRadius)
    {
        _center = center;
        _radius = radius;
        _cx = cx;
        _cy = cy;
        _sqrLookRadius = sqrLookRadius;
        _xyIterator = new SurroundingRegionsIterator(_cx, _cy, (byte)Math.Clamp(Math.Ceiling(radius / Regions.WORLD_SIZE), byte.MinValue, byte.MaxValue));
    }

    private static void GetClampedCoordinates(in Vector2 point, out byte x, out byte y)
    {
        x = (byte)Math.Clamp(Mathf.FloorToInt((point.x + 4096f) / Regions.REGION_SIZE), byte.MinValue, Regions.REGION_SIZE);
        y = (byte)Math.Clamp(Mathf.FloorToInt((point.y + 4096f) / Regions.REGION_SIZE), byte.MinValue, Regions.REGION_SIZE);
    }

    public bool MoveNext()
    {
        do
        {
            if (!_xyIterator.MoveNext())
            {
                _current = default;
                return false;
            }

            _current = _xyIterator.Current;
        } while (!IsInRange());

        return true;
    }

    private readonly bool IsInRange()
    {
        RegionCoord c = _current;
        if (c.x == _cx && c.y == _cy)
            return _radius > 0;

        byte regSize = Regions.REGION_SIZE;

        int posX = c.x * regSize - 4096 + regSize / 2;
        int posY = c.y * regSize - 4096 + regSize / 2;

        float dx = _center.x - posX,
              dy = _center.y - posY;

        return dx * dx + dy * dy <= _sqrLookRadius;
    }

    public void Reset()
    {
        _xyIterator.Reset();
        _current = default;
    }

    public RegionCoord Current => _current;
    object IEnumerator.Current => _current;
    public readonly RadiusRegionsEnumerator GetEnumerator() => new RadiusRegionsEnumerator(_center, _radius, _cx, _cy, _sqrLookRadius);
    readonly IEnumerator<RegionCoord> IEnumerable<RegionCoord>.GetEnumerator() => new RadiusRegionsEnumerator(_center, _radius, _cx, _cy, _sqrLookRadius);
    readonly IEnumerator IEnumerable.GetEnumerator() => new RadiusRegionsEnumerator(_center, _radius, _cx, _cy, _sqrLookRadius);
    readonly void IDisposable.Dispose() { }
}