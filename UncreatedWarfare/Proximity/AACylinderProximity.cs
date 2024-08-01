using System;

namespace Uncreated.Warfare.Proximity;

/// <inheritdoc />
public class AACylinderProximity : IAACylinderProximity
{
    private readonly Bounds _bounds;
    private readonly SnapAxis _axis;
    private readonly float _radius;
    private readonly float _radSqr;
    private readonly float _height;
    private Vector3 _center;

    /// <inheritdoc />
    public Bounds worldBounds => _bounds;

    /// <inheritdoc />
    public SnapAxis Axis => _axis;

    /// <inheritdoc />
    public float Radius => _radius;

    /// <inheritdoc />
    public float Height => _height;

    /// <inheritdoc />
    public Vector3 Center => _center;

    /// <summary>
    /// Create an axis-aligned cylinder.
    /// </summary>
    /// <param name="center">Center of the cylinder. Depending on the axis one component will be discarded if <paramref name="height"/> isn't finite. Other components must be finite.</param>
    /// <param name="radius">Radius of the cylinder. Must be finite.</param>
    /// <param name="height">Height of the cylinder. Can be infinity or NaN (converts to infinity).</param>
    /// <param name="axis">Axis to align to. Defaults to the Y axis. Must either be <see cref="SnapAxis.X"/>, <see cref="SnapAxis.Y"/>, or <see cref="SnapAxis.Z"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Axis is not X, Y, or Z.</exception>
    /// <exception cref="ArgumentException">Radius or center is not finite.</exception>
    public AACylinderProximity(Vector3 center, float radius, float height, SnapAxis axis = SnapAxis.Y)
    {
        if (_axis is not SnapAxis.X and not SnapAxis.Y and not SnapAxis.Z)
            throw new ArgumentOutOfRangeException(nameof(axis), "Axis must be X, Y, or Z.");

        if (!float.IsFinite(radius))
            throw new ArgumentException("Not finite.", nameof(radius));

        if (float.IsNaN(height))
            height = float.PositiveInfinity;
        else if (float.IsNegative(height))
            height = -height;

        if (float.IsInfinity(height))
            center[axis switch { SnapAxis.X => 0, SnapAxis.Y => 1, _ => 2 }] = 0f;

        if (!center.IsFinite())
            throw new ArgumentException("Not finite.", nameof(center));

        _axis = axis;
        _radius = radius;
        _radSqr = radius * radius;
        _height = height;
        _center = center;
        CalculateBounds(ref _bounds);
    }

    /// <summary>
    /// Create an axis-aligned cylinder on the Y axis.
    /// </summary>
    /// <param name="center">Center of the cylinder as (X, Z). Components must be finite.</param>
    /// <param name="radius">Radius of the cylinder. Must be finite.</param>
    /// <param name="height">Height of the cylinder. Can be infinity or NaN (converts to infinity).</param>
    /// <exception cref="ArgumentOutOfRangeException">Axis is not X, Y, or Z.</exception>
    /// <exception cref="ArgumentException">Radius or center is not finite.</exception>
    public AACylinderProximity(Vector2 center, float radius, float height = float.PositiveInfinity)
    {
        if (!float.IsFinite(center.x) || !float.IsFinite(center.y))
            throw new ArgumentException("Not finite.", nameof(center));

        if (!float.IsFinite(radius))
            throw new ArgumentException("Not finite.", nameof(radius));

        if (float.IsNaN(height) || float.IsNegativeInfinity(height))
            height = float.PositiveInfinity;
        else if (height < 0)
            height = -height;

        _axis = SnapAxis.Y;
        _radius = radius;
        _radSqr = radius * radius;
        _height = height;
        _center = center;
        CalculateBounds(ref _bounds);
    }

    private AACylinderProximity(SnapAxis axis, float radius, float height, Vector3 center, Bounds bounds)
    {
        _axis = axis;
        _radius = radius;
        _radSqr = radius * radius;
        _height = height;
        _center = center;
        _bounds = bounds;
    }

    private void CalculateBounds(ref Bounds bounds)
    {
        ref Vector3 center = ref _center;
        float radius = _radius;
        switch (_axis)
        {
            case SnapAxis.X:
                bounds.center = new Vector3(0f, center.y, center.z);
                bounds.extents = new Vector3(_height, radius, radius);
                break;

            case SnapAxis.Y:
                bounds.center = new Vector3(center.x, 0f, center.z);
                bounds.extents = new Vector3(radius, _height, radius);
                break;

            default: // Z
                bounds.center = new Vector3(center.x, center.y, 0f);
                bounds.extents = new Vector3(radius, radius, _height);
                break;
        }
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        Vector3 pos = _center;
        float x = pos.x - position.x,
              y = pos.y - position.y,
              z = pos.z - position.z;

        float h = _height / 2f;
        float dist, testCoord;

        switch (_axis)
        {
            case SnapAxis.X:
                dist = y * y + z * z;
                testCoord = x;
                break;

            case SnapAxis.Y:
                dist = x * x + z * z;
                testCoord = y;
                break;

            default: // Z
                dist = x * x + y * y;
                testCoord = z;
                break;
        }

        if (dist > _radSqr)
            return false;

        if (!float.IsFinite(h))
            return true;

        return testCoord >= -h && testCoord <= h;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        Vector3 pos = _center;
        switch (_axis)
        {
            case SnapAxis.X:
                float h = _height / 2f, r = _radius;
                return (!float.IsFinite(h) || position.y >= pos.x - h && position.y <= pos.x + h) && position.x >= pos.z - r && position.x <= pos.z + r;

            case SnapAxis.Y:
                float x = pos.x - position.x,
                      z = pos.z - position.y;

                float dist = x * x + z * z;
                return dist <= _radSqr;

            default: // Z
                h = _height / 2f; r = _radius;
                return (!float.IsFinite(h) || position.x >= pos.z - h && position.x <= pos.z + h) && position.y >= pos.x - r && position.y <= pos.x + r;
        }
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AACylinderProximity(_axis, _radius, _height, _center, _bounds);
    }
}
