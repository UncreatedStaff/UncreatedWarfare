using System;
using Uncreated.Warfare.Locations;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Represents an object existing in the world.
/// </summary>
public interface ITransformObject
{
    /// <summary>
    /// The world position of the object.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <exception cref="NotSupportedException">Not able to set position.</exception>
    Vector3 Position { get; set; }

    /// <summary>
    /// The world rotation of the object.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <exception cref="NotSupportedException">Not able to set rotation.</exception>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// The world scale of the object.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <exception cref="NotSupportedException">Not able to set scale.</exception>
    Vector3 Scale { get; set; }

    /// <summary>
    /// Set the position and rotation at the same time.
    /// </summary>
    /// <exception cref="GameThreadException">Not on main thread.</exception>
    /// <exception cref="NotSupportedException">Not able to set position or rotation.</exception>
    void SetPositionAndRotation(Vector3 position, Quaternion rotation);

    /// <summary>
    /// This object's position relative to the map grid.
    /// </summary>
    GridLocation GridLocation
    {
        get => new GridLocation(Position);
    }
}

public static class TransformObjectExtensions
{
    /// <summary>
    /// Check if this object is within a radius of <paramref name="position"/>.
    /// </summary>
    public static bool InRadiusOf(this ITransformObject @object, in Vector3 position, float radius, bool is2d = false)
    {
        Vector3 pos = @object.Position;
        float x = pos.x - position.x;
        x *= x;
        float y = pos.y - position.y;
        y *= y;
        float z = pos.z - position.z;
        z *= z;
        return (x + ((!is2d ? 1 : 0) * y) + z) <= radius * radius;
    }
}