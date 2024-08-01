using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// Defines an object that can test points to see if they're contained in the proximity.
/// </summary>
/// <remarks>
/// Also implements <see cref="IShapeVolume"/>.
/// Any components implementing this interface should destroy themselves in <see cref="IDisposable.Dispose"/>.
/// </remarks>
public interface IProximity : IShapeVolume, ICloneable
{
    /// <summary>
    /// Check if a position is within the proximity.
    /// </summary>
    bool TestPoint(Vector3 position);

    /// <summary>
    /// Check if a position is within the proximity while ignoring Y position.
    /// </summary>
    bool TestPoint(Vector2 position);

    bool IShapeVolume.containsPoint(Vector3 point) => TestPoint(point);
}

/// <summary>
/// Able to listen for a certain type of object to enter or leave the proximity.
/// </summary>
public interface IEventBasedProximity<out T> : IProximity
{
    /// <summary>
    /// Invoked when an object goes in proximity.
    /// </summary>
    event Action<T> OnObjectEntered;

    /// <summary>
    /// Invoked when an object leaves proximity.
    /// </summary>
    event Action<T> OnObjectExited;
}

/// <summary>
/// Able to listen for a certain type of object to enter or leave the proximity, and keeps a list of objects currently in proximity.
/// </summary>
public interface ITrackingProximity<T> : IEventBasedProximity<T>
{
    /// <summary>
    /// List of all objects currently in proximity.
    /// </summary>
    /// <remarks>
    /// This should be updated before <see cref="IEventBasedProximity{T}.OnObjectEntered"/>
    /// and <see cref="IEventBasedProximity{T}.OnObjectExited"/> are updated.
    /// </remarks>
    IReadOnlyList<T> ActiveObjects { get; }

    /// <summary>
    /// If this object is in proximity.
    /// </summary>
    bool Contains(T obj);
}

/// <summary>
/// Allows implementations of <see cref="IProximity"/> to implement a function to get the nearest point on the border of the proximity to another point.
/// </summary>
public interface INearestPointProximity : IProximity
{
    /// <summary>
    /// Get the nearest point on the border of this proximity to <paramref name="fromLocation"/>.
    /// </summary>
    Vector3 GetNearestPointOnBorder(Vector3 fromLocation);
}

/// <summary>
/// Allows implementations of <see cref="IProximity"/> to implement a class that attaches a proximity to a <see cref="Transform"/>.
/// </summary>
public interface IAttachableProximity<out TAttachedProximity> : IProximity
    where TAttachedProximity : class, IAttachedProximity
{
    /// <summary>
    /// Attach this proximity to a <paramref name="attachmentRoot"/>.
    /// </summary>
    TAttachedProximity CreateAttachedProximity(Transform attachmentRoot);
}

/// <summary>
/// Defines a proximity attached to a <see cref="Transform"/>.
/// </summary>
public interface IAttachedProximity : IProximity
{
    /// <summary>
    /// The transform this proximity is attached to.
    /// </summary>
    Transform? AttachmentRoot { get; }
}

/// <summary>
/// Defines an 'axis-aligned bounding box' proximity attached to a <see cref="Transform"/>.
/// </summary>
public interface IAttachedAABBProximity : IAABBProximity, IAttachedProximity;

/// <summary>
/// Defines an 'axis-aligned bounding box' proximity.
/// </summary>
public interface IAABBProximity : INearestPointProximity, IAttachableProximity<IAttachedAABBProximity>
{
    /// <summary>
    /// The position and dimensions of the bounding box in world space.
    /// </summary>
    /// <remarks>Any size/extents components may be <see cref="float.PositiveInfinity"/>.</remarks>
    Bounds Dimensions { get; }
    float IShapeVolume.internalVolume
    {
        get
        {
            Vector3 size = Dimensions.size;
            if (float.IsInfinity(size.x)) size.x = Level.size;
            if (float.IsInfinity(size.z)) size.z = Level.size;
            if (float.IsInfinity(size.y)) size.y = Level.HEIGHT * 2;
            return size.x * size.y * size.z;
        }
    }
    float IShapeVolume.surfaceArea
    {
        get
        {
            Vector3 extents = Dimensions.extents;
            if (float.IsInfinity(extents.x)) extents.x = Level.size / 2f;
            if (float.IsInfinity(extents.z)) extents.z = Level.size / 2f;
            if (float.IsInfinity(extents.y)) extents.y = Level.HEIGHT;
            return extents.x * 4 + extents.y * 4 + extents.z * 4;
        }
    }

    Vector3 INearestPointProximity.GetNearestPointOnBorder(Vector3 fromLocation) => ProximityExtensions.GetNearestPointOnBorder(this, fromLocation);
    IAttachedAABBProximity IAttachableProximity<IAttachedAABBProximity>.CreateAttachedProximity(Transform attachmentRoot) => new AttachedAABBProximity(attachmentRoot, this);
}

/// <summary>
/// Defines a proximity in the shape of a sphere attached to a <see cref="Transform"/>.
/// </summary>
public interface IAttachedSphereProximity : ISphereProximity, IAttachedProximity;

/// <summary>
/// Defines a proximity in the shape of a sphere.
/// </summary>
public interface ISphereProximity : INearestPointProximity, IAttachableProximity<IAttachedSphereProximity>
{
    /// <summary>
    /// Sphere used for detection.
    /// </summary>
    BoundingSphere Sphere { get; }
    float IShapeVolume.internalVolume
    {
        get
        {
            float rad = Sphere.radius;
            return 4f / 3f * Mathf.PI * rad * rad * rad;
        }
    }
    float IShapeVolume.surfaceArea
    {
        get
        {
            float rad = Sphere.radius;
            return 4f * Mathf.PI * rad * rad;
        }
    }

    Vector3 INearestPointProximity.GetNearestPointOnBorder(Vector3 fromLocation) => ProximityExtensions.GetNearestPointOnBorder(this, fromLocation);
    IAttachedSphereProximity IAttachableProximity<IAttachedSphereProximity>.CreateAttachedProximity(Transform attachmentRoot) => new AttachedSphereProximity(attachmentRoot, this);
}

/// <summary>
/// Defines an 'axis-aligned' cylinder proximity attached to a <see cref="Transform"/>.
/// </summary>
public interface IAttachedAACylinderProximity : IAACylinderProximity, IAttachedProximity;

/// <summary>
/// Defines an 'axis-aligned' cylinder proximity.
/// </summary>
public interface IAACylinderProximity : INearestPointProximity, IAttachableProximity<IAttachedAACylinderProximity>
{
    /// <summary>
    /// The axis the cylinder is aligned to. Must be 'X', 'Y', or 'Z'.
    /// </summary>
    /// <remarks>Defaults to 'Y'.</remarks>
    SnapAxis Axis { get; }

    /// <summary>
    /// Radius of the cylinder.
    /// </summary>
    float Radius { get; }

    /// <summary>
    /// Cylinder height.
    /// </summary>
    /// <remarks>Can be <see cref="float.PositiveInfinity"/>.</remarks>
    float Height { get; }
    
    /// <summary>
    /// Center position of the cylinder.
    /// </summary>
    Vector3 Center { get; }
    float IShapeVolume.internalVolume
    {
        get
        {
            float rad = Radius;
            float height = Height;
            if (float.IsInfinity(height)) height = Level.HEIGHT * 2;
            return Mathf.PI * rad * rad * height;
        }
    }
    float IShapeVolume.surfaceArea
    {
        get
        {
            float rad = Radius;
            float height = Height;
            if (float.IsInfinity(height)) height = Level.HEIGHT * 2;
            return 2f * Mathf.PI * rad * height + 2f * Mathf.PI * rad * rad;
        }
    }

    Vector3 INearestPointProximity.GetNearestPointOnBorder(Vector3 fromLocation) => ProximityExtensions.GetNearestPointOnBorder(this, fromLocation);
    IAttachedAACylinderProximity IAttachableProximity<IAttachedAACylinderProximity>.CreateAttachedProximity(Transform attachmentRoot) => new AttachedAACylinderProximity(attachmentRoot, this);
}

/// <summary>
/// Defines a proximity outlined by a 2D polygon attached to a <see cref="Transform"/>.
/// </summary>
public interface IAttachedPolygonProximity : IPolygonProximity, IAttachedProximity;

/// <summary>
/// Defines a proximity outlined by a 2D polygon.
/// </summary>
public interface IPolygonProximity : INearestPointProximity, IAttachableProximity<IAttachedPolygonProximity>
{
    /// <summary>
    /// Ordered list of all points in the polygon.
    /// </summary>
    IReadOnlyList<Vector2> Points { get; }

    /// <summary>
    /// Maximum Y value of the polygon.
    /// </summary>
    float? MaxHeight { get; }
    
    /// <summary>
    /// Minimum Y value of the polygon.
    /// </summary>
    float? MinHeight { get; }

    Vector3 INearestPointProximity.GetNearestPointOnBorder(Vector3 fromLocation) => ProximityExtensions.GetNearestPointOnBorder(this, fromLocation);
    IAttachedPolygonProximity IAttachableProximity<IAttachedPolygonProximity>.CreateAttachedProximity(Transform attachmentRoot) => new AttachedPolygonProximity(attachmentRoot, this);
}