using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using UnityEngine;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// An axis-aligned cylinder attached to a transform. Scale, position, and rotation are applied.
/// </summary>
public class AttachedAACylinderProximity : IAttachedAACylinderProximity
{
    private readonly IAACylinderProximity _cylinder;

    /// <inheritdoc />
    public SnapAxis Axis => _cylinder.Axis;

    /// <inheritdoc />
    public float Radius => _cylinder.Radius;

    /// <inheritdoc />
    public float Height => _cylinder.Height;

    /// <inheritdoc />
    public Vector3 Center => _cylinder.Center;

    /// <summary>
    /// Axis-aligned bounds of the rectange.
    /// </summary>
    /// <remarks>Equal to <see cref="Dimensions"/> in this case.</remarks>
    public Bounds worldBounds
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            Bounds bounds = _cylinder.worldBounds;
            return AttachmentRoot == null ? bounds : new Bounds(AttachmentRoot.TransformPoint(bounds.center), AttachmentRoot.TransformVector(bounds.size));
        }
    }


    /// <inheritdoc />
    float IShapeVolume.internalVolume
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            Vector3 v = GetTrasformedVector();
            return Mathf.PI * v.x * v.y * v.z;
        }
    }

    /// <inheritdoc />
    float IShapeVolume.surfaceArea
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            Vector3 v = GetTrasformedVector();
            return _cylinder.Axis switch
            {
                SnapAxis.X => Mathf.PI * (v.y + v.z) * v.x + 2 * Mathf.PI * v.y * v.z,
                SnapAxis.Y => Mathf.PI * (v.x + v.z) * v.y + 2 * Mathf.PI * v.x * v.z,
                SnapAxis.Z => Mathf.PI * (v.x + v.y) * v.z + 2 * Mathf.PI * v.x * v.y,
                _ => throw new InvalidOperationException("IAACylinderProximity has an axis other than X, Y, and Z.")
            };
        }
    }

    /// <inheritdoc />
    public Transform? AttachmentRoot { get; }

    /// <summary>
    /// Create an axis-aligned cylinder attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    /// <param name="center">Center of the cylinder. Depending on the axis one component will be discarded if <paramref name="height"/> isn't finite. Other components must be finite.</param>
    /// <param name="radius">Radius of the cylinder. Must be finite.</param>
    /// <param name="height">Height of the cylinder. Can be infinity or NaN (converts to infinity).</param>
    /// <param name="axis">Axis to align to. Defaults to the Y axis. Must either be <see cref="SnapAxis.X"/>, <see cref="SnapAxis.Y"/>, or <see cref="SnapAxis.Z"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Axis is not X, Y, or Z.</exception>
    /// <exception cref="ArgumentException">Radius or center is not finite.</exception>
    public AttachedAACylinderProximity(Transform attachmentRoot, Vector3 center, float radius, float height, SnapAxis axis = SnapAxis.Y)
    {
        AttachmentRoot = attachmentRoot;
        _cylinder = new AACylinderProximity(center, radius, height, axis);
    }

    /// <summary>
    /// Create an axis-aligned cylinder on the Y axis attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    /// <param name="center">Center of the cylinder as (X, Z). Components must be finite.</param>
    /// <param name="radius">Radius of the cylinder. Must be finite.</param>
    /// <param name="height">Height of the cylinder. Can be infinity or NaN (converts to infinity).</param>
    /// <exception cref="ArgumentOutOfRangeException">Axis is not X, Y, or Z.</exception>
    /// <exception cref="ArgumentException">Radius or center is not finite.</exception>
    public AttachedAACylinderProximity(Transform attachmentRoot, Vector2 center, float radius, float height = float.PositiveInfinity)
    {
        AttachmentRoot = attachmentRoot;
        _cylinder = new AACylinderProximity(center, radius, height);
    }

    /// <summary>
    /// Create an axis-aligned cylinder from another <see cref="IAACylinderProximity"/> attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedAACylinderProximity(Transform attachmentRoot, IAACylinderProximity cylinder)
    {
        AttachmentRoot = attachmentRoot;
        _cylinder = cylinder;
    }

    private Vector3 GetTrasformedVector()
    {
        float rad = Radius;
        float height = Height;

        if (float.IsInfinity(height)) height = Level.HEIGHT * 2;

        Vector3 v = _cylinder.Axis switch
        {
            SnapAxis.X => new Vector3(height, rad, rad),
            SnapAxis.Y => new Vector3(rad, height, rad),
            SnapAxis.Z => new Vector3(rad, rad, height),
            _ => throw new InvalidOperationException("IAACylinderProximity has an axis other than X, Y, and Z.")
        };

        if (AttachmentRoot != null)
            v = AttachmentRoot.TransformVector(v);

        return v;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _cylinder.TestPoint(position - worldPos);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _cylinder.TestPoint(new Vector2(position.x - worldPos.x, position.y - worldPos.z));
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AttachedAACylinderProximity(AttachmentRoot!, _cylinder);
    }
}
