using System;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// An axis-aligned bounding box attached to a transform. Scale, position, and rotation are applied.
/// </summary>
public class AttachedAABBProximity : IAttachedAABBProximity, IFormattable
{
    private readonly IAABBProximity _aabb;

    /// <inheritdoc />
    public Bounds Dimensions => _aabb.Dimensions;

    /// <summary>
    /// Axis-aligned bounds of the rectange.
    /// </summary>
    /// <remarks>Equal to <see cref="Dimensions"/> in this case.</remarks>
    public Bounds worldBounds
    {
        get
        {
            GameThread.AssertCurrent();

            Bounds bounds = _aabb.worldBounds;
            return AttachmentRoot == null ? bounds : new Bounds(AttachmentRoot.TransformPoint(bounds.center), AttachmentRoot.TransformVector(bounds.size));
        }
    }


    /// <inheritdoc />
    float IShapeVolume.internalVolume
    {
        get
        {
            GameThread.AssertCurrent();

            Vector3 size = _aabb.Dimensions.size;
            
            if (AttachmentRoot != null)
                size = AttachmentRoot.TransformVector(size);

            if (float.IsInfinity(size.x)) size.x = Level.size;
            if (float.IsInfinity(size.z)) size.z = Level.size;
            if (float.IsInfinity(size.y)) size.y = Level.HEIGHT * 2;

            return size.x * size.y * size.z;
        }
    }

    /// <inheritdoc />
    float IShapeVolume.surfaceArea
    {
        get
        {
            GameThread.AssertCurrent();

            Vector3 extents = _aabb.Dimensions.extents;

            if (AttachmentRoot != null)
                extents = AttachmentRoot.TransformVector(extents);

            if (float.IsInfinity(extents.x)) extents.x = Level.size / 2f;
            if (float.IsInfinity(extents.z)) extents.z = Level.size / 2f;
            if (float.IsInfinity(extents.y)) extents.y = Level.HEIGHT;

            return extents.x * 4 + extents.y * 4 + extents.z * 4;
        }
    }

    /// <inheritdoc />
    float IProximity.Area
    {
        get
        {
            GameThread.AssertCurrent();

            Vector3 extents = _aabb.Dimensions.extents;

            if (AttachmentRoot != null)
                extents = AttachmentRoot.TransformVector(extents);

            if (float.IsInfinity(extents.x)) extents.x = Level.size / 2f;
            if (float.IsInfinity(extents.z)) extents.z = Level.size / 2f;

            return extents.x * extents.z;
        }
    }

    /// <inheritdoc />
    public Transform? AttachmentRoot { get; }

    /// <summary>
    /// Create a 3D bounding box attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedAABBProximity(Transform attachmentRoot, in Bounds bounds)
    {
        AttachmentRoot = attachmentRoot;
        _aabb = new AABBProximity(in bounds);
    }

    /// <summary>
    /// Create an axis-aligned bounding box attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedAABBProximity(Transform attachmentRoot, in Vector3 center, in Vector3 size)
    {
        AttachmentRoot = attachmentRoot;
        _aabb = new AABBProximity(in center, in size);
    }

    /// <summary>
    /// Create a 2D bounding box with an infinite height attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedAABBProximity(Transform attachmentRoot, in Vector2 center, in Vector2 size)
    {
        AttachmentRoot = attachmentRoot;
        _aabb = new AABBProximity(in center, in size);
    }

    /// <summary>
    /// Create an axis-aligned bounding box from another <see cref="IAABBProximity"/> attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedAABBProximity(Transform attachmentRoot, IAABBProximity aabb)
    {
        AttachmentRoot = attachmentRoot;
        _aabb = aabb;
    }

    /// <inheritdoc />
    public bool TestPoint(in Vector3 position)
    {
        GameThread.AssertCurrent();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _aabb.TestPoint(position - worldPos);
    }

    /// <inheritdoc />
    public bool TestPoint(in Vector2 position)
    {
        GameThread.AssertCurrent();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _aabb.TestPoint(new Vector2(position.x - worldPos.x, position.y - worldPos.z));
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AttachedAABBProximity(AttachmentRoot!, _aabb);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _aabb.ToString();
    }

    /// <inheritdoc />
    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (_aabb is IFormattable f)
            return f.ToString(format, formatProvider);

        return _aabb.ToString();
    }
}