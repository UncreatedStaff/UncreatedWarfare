using SDG.Framework.Utilities;
using System;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// A sphere attached to a transform. Scale and position are applied.
/// </summary>
public class AttachedSphereProximity : IAttachedSphereProximity
{
    private readonly ISphereProximity _sphere;

    /// <inheritdoc />
    public BoundingSphere Sphere => _sphere.Sphere;

    /// <inheritdoc />
    public Bounds worldBounds
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            Bounds bounds = _sphere.worldBounds;
            return AttachmentRoot == null ? bounds : new Bounds(AttachmentRoot.TransformPoint(bounds.center), AttachmentRoot.TransformVector(bounds.size));
        }
    }

    /// <inheritdoc />
    public Transform? AttachmentRoot { get; }

    /// <inheritdoc />
    float IShapeVolume.internalVolume
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            float rad = Sphere.radius;
            Vector3 scale = AttachmentRoot != null ? AttachmentRoot.lossyScale : Vector3.one;
            return 4f / 3f * Mathf.PI * rad * scale.x * rad * scale.y * rad * scale.z;
        }
    }

    /// <inheritdoc />
    float IShapeVolume.surfaceArea
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            float rad = Sphere.radius;
            Vector3 scale = AttachmentRoot != null ? AttachmentRoot.lossyScale : Vector3.one;
            if (scale.IsNearlyEqual(Vector3.one))
            {
                return 4f * Mathf.PI * rad * rad;
            }

            return 4f * Mathf.PI *
                MathF.Pow(
                    (MathF.Pow(scale.x * scale.y, 1.6075f) + MathF.Pow(scale.x * scale.z, 1.6075f) + MathF.Pow(scale.y * scale.z, 1.6075f)) / 3,
                    1f / 1.6075f
                );
        }
    }

    /// <summary>
    /// Create a sphere from a <see cref="BoundingSphere"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Position or radius isn't finite (not NaN or Infinity).</exception>
    public AttachedSphereProximity(Transform attachmentRoot, BoundingSphere sphere)
    {
        AttachmentRoot = attachmentRoot;
        _sphere = new SphereProximity(sphere);
    }

    /// <summary>
    /// Create a sphere from a position and radius.
    /// </summary>
    /// <exception cref="ArgumentException">Position or radius isn't finite (not NaN or Infinity).</exception>
    public AttachedSphereProximity(Transform attachmentRoot, Vector3 position, float radius)
    {
        AttachmentRoot = attachmentRoot;
        _sphere = new SphereProximity(position, radius);
    }

    /// <summary>
    /// Create a sphere from another <see cref="ISphereProximity"/> attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedSphereProximity(Transform attachmentRoot, ISphereProximity sphere)
    {
        AttachmentRoot = attachmentRoot;
        _sphere = sphere;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _sphere.TestPoint(position - worldPos);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? Vector3.zero : AttachmentRoot.InverseTransformPoint(position);
        return _sphere.TestPoint(new Vector2(position.x - worldPos.x, position.y - worldPos.z));
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AttachedSphereProximity(AttachmentRoot!, _sphere);
    }
}
