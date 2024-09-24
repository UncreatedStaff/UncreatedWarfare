using System;
using System.Globalization;

namespace Uncreated.Warfare.Proximity;

/// <inheritdoc cref="IAABBProximity" />
public class AABBProximity : IAABBProximity, IFormattable
{
    private Bounds _bounds;

    /// <inheritdoc />
    public Bounds Dimensions => _bounds;

    /// <summary>
    /// Axis-aligned bounds of the rectange.
    /// </summary>
    /// <remarks>Equal to <see cref="Dimensions"/> in this case.</remarks>
    public Bounds worldBounds => _bounds;

    /// <summary>
    /// Create a 3D bounding box.
    /// </summary>
    public AABBProximity(Bounds bounds)
    {
        _bounds = bounds;
    }

    /// <summary>
    /// Create a 3D bounding box.
    /// </summary>
    public AABBProximity(Vector3 center, Vector3 size)
    {
        _bounds = new Bounds(center, size);
    }

    /// <summary>
    /// Create a 2D bounding box with an infinite height.
    /// </summary>
    public AABBProximity(Vector2 center, Vector2 size)
    {
        _bounds = new Bounds(new Vector3(center.x, 0f, center.y), new Vector3(size.x, float.PositiveInfinity, size.y));
    }

    /// <summary>
    /// Create a bounding box from another <see cref="IAABBProximity"/>.
    /// </summary>
    public AABBProximity(IAABBProximity other)
    {
        _bounds = other.Dimensions;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        Vector3 extents = _bounds.extents;
        Vector3 center = _bounds.center;

        return (!float.IsFinite(extents.x) || position.x >= center.x - extents.x && position.x <= center.x + extents.x)
            && (!float.IsFinite(extents.y) || position.y >= center.y - extents.y && position.y <= center.y + extents.y)
            && (!float.IsFinite(extents.z) || position.z >= center.z - extents.z && position.z <= center.z + extents.z);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        Vector3 extents = _bounds.extents;
        Vector3 center = _bounds.center;

        return (!float.IsFinite(extents.x) || position.x >= center.x - extents.x && position.x <= center.x + extents.x)
            && (!float.IsFinite(extents.z) || position.y >= center.z - extents.z && position.y <= center.z + extents.z);
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AABBProximity(_bounds);
    }

    /// <inheritdoc />
    public override string ToString() => ToString(null, null);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= "F2";
        formatProvider ??= CultureInfo.InvariantCulture;

        Vector3 extents = _bounds.extents;
        Vector3 center = _bounds.center;

        return $"({(center.x - extents.x).ToString(format, formatProvider)}:{(center.x + extents.x).ToString(format, formatProvider)}, {(center.y - extents.y).ToString(format, formatProvider)}:{(center.y + extents.y).ToString(format, formatProvider)}, {(center.z - extents.z).ToString(format, formatProvider)}:{(center.z + extents.z).ToString(format, formatProvider)})";
    }
}