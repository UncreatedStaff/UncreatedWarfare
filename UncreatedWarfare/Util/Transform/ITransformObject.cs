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
    /// A matrix transforming from world space to local space.
    /// </summary>
    Matrix4x4 WorldToLocal { get; }

    /// <summary>
    /// A matrix transforming from local space to world space.
    /// </summary>
    Matrix4x4 LocalToWorld { get; }

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
