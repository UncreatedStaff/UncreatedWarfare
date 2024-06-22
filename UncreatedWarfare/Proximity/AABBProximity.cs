using UnityEngine;

namespace Uncreated.Warfare.Proximity;

/// <inheritdoc />
public class AABBProximity : IAABBProximity
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
        Vector3 size = _bounds.size;
        Vector3 min = _bounds.min;

        return (float.IsInfinity(size.x) || position.x > min.x && position.x < min.x + size.x)
            && (float.IsInfinity(size.y) || position.y > min.y && position.y < min.y + size.y)
            && (float.IsInfinity(size.z) || position.z > min.z && position.z < min.z + size.z);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        Vector3 size = _bounds.size;
        Vector3 min = _bounds.min;

        return (float.IsInfinity(size.x) || position.x > min.x && position.x < min.x + size.x)
            && (float.IsInfinity(size.z) || position.y > min.z && position.y < min.z + size.z);
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AABBProximity(_bounds);
    }
}