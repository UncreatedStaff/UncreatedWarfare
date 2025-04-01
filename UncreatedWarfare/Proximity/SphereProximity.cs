using System;

namespace Uncreated.Warfare.Proximity;

/// <inheritdoc cref="ISphereProximity" />
public class SphereProximity : ISphereProximity, IFormattable
{
    private readonly BoundingSphere _sphere;
    private readonly Bounds _bounds;
    private readonly float _radSqr;

    /// <summary>
    /// Axis-aligned bounds of the sphere.
    /// </summary>
    public Bounds worldBounds => _bounds;

    /// <inheritdoc />
    public BoundingSphere Sphere => _sphere;

    /// <summary>
    /// Create a sphere from a <see cref="BoundingSphere"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Position or radius isn't finite (not NaN or Infinity).</exception>
    public SphereProximity(in BoundingSphere sphere)
    {
        if (!float.IsFinite(sphere.radius) || !sphere.position.IsFinite())
            throw new ArgumentException("Not finite.", nameof(sphere));

        _sphere = sphere;
        _radSqr = sphere.radius * sphere.radius;
        CalculateBounds(ref _bounds, in sphere);
    }

    /// <summary>
    /// Create a sphere from a position and radius.
    /// </summary>
    /// <exception cref="ArgumentException">Position or radius isn't finite (not NaN or Infinity).</exception>
    public SphereProximity(in Vector3 position, float radius)
    {
        if (!float.IsFinite(radius))
            throw new ArgumentException("Not finite.", nameof(radius));
        
        if (!position.IsFinite())
            throw new ArgumentException("Not finite.", nameof(position));

        _sphere.position = position;
        _sphere.radius = radius;
        _radSqr = radius * radius;
        CalculateBounds(ref _bounds, in _sphere);
    }

    /// <summary>
    /// Create a sphere from another <see cref="ISphereProximity"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Position or radius isn't finite (not NaN or Infinity).</exception>
    public SphereProximity(ISphereProximity other)
    {
        BoundingSphere sphere = other.Sphere;
        _sphere = sphere;
        _radSqr = sphere.radius * sphere.radius;

        if (!float.IsFinite(sphere.radius) || !sphere.position.IsFinite())
            throw new ArgumentException("Not finite.", nameof(other));

        _bounds = other.worldBounds;
    }

    private SphereProximity(in BoundingSphere sphere, in Bounds bounds)
    {
        _sphere = sphere;
        _radSqr = sphere.radius * sphere.radius;
        _bounds = bounds;
    }

    private static void CalculateBounds(ref Bounds bounds, in BoundingSphere sphere)
    {
        Vector3 extents = default;
        float bound = sphere.radius * 2;
        extents.x = bound; extents.y = bound; extents.z = bound;

        bounds.center = sphere.position;
        bounds.extents = extents;
    }

    /// <inheritdoc />
    public bool TestPoint(in Vector3 position)
    {
        Vector3 pos = _sphere.position;
        float x = pos.x - position.x,
              y = pos.y - position.y,
              z = pos.z - position.z;

        return x * x + y * y + z * z <= _radSqr;
    }

    /// <inheritdoc />
    public bool TestPoint(in Vector2 position)
    {
        Vector3 pos = _sphere.position;
        float x = pos.x - position.x,
              z = pos.z - position.y;

        return x * x + z * z <= _radSqr;
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new SphereProximity(in _sphere, in _bounds);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        ref readonly Vector3 center = ref _sphere.position;
        float r = _sphere.radius;

        return $"r = {r}, b = ({(center.x - r).ToString(format, formatProvider)}:{(center.x + r).ToString(format, formatProvider)}, {(center.y - r).ToString(format, formatProvider)}:{(center.y + r).ToString(format, formatProvider)}, {(center.z - r).ToString(format, formatProvider)}:{(center.z + r).ToString(format, formatProvider)})";
    }
}
