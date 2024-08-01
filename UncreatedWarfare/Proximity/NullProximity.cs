using SDG.Framework.Utilities;
using System;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// Proximity that isn't possible to be in range of.
/// </summary>
public sealed class NullProximity : IAttachableProximity<IAttachedProximity>, INearestPointProximity
{
    /// <inheritdoc />
    bool IProximity.TestPoint(Vector3 position) => false;

    /// <inheritdoc />
    bool IProximity.TestPoint(Vector2 position) => false;

    /// <inheritdoc />
    public Vector3 GetNearestPointOnBorder(Vector3 fromLocation) => fromLocation;

    /// <inheritdoc />
    object ICloneable.Clone() => new NullProximity();

    /// <inheritdoc />
    IAttachedProximity IAttachableProximity<IAttachedProximity>.CreateAttachedProximity(Transform attachmentRoot) => new Attached(attachmentRoot);

    /// <inheritdoc />
    Bounds IShapeVolume.worldBounds => default;

    /// <inheritdoc />
    float IShapeVolume.internalVolume => 0f;

    /// <inheritdoc />
    float IShapeVolume.surfaceArea => 0f;

    private class Attached(Transform attachmentRoot) : IAttachedProximity
    {
        /// <inheritdoc />
        public Transform? AttachmentRoot { get; } = attachmentRoot;

        /// <inheritdoc />
        bool IProximity.TestPoint(Vector3 position) => false;

        /// <inheritdoc />
        bool IProximity.TestPoint(Vector2 position) => false;

        /// <inheritdoc />
        object ICloneable.Clone() => new NullProximity();

        /// <inheritdoc />
        Bounds IShapeVolume.worldBounds => default;

        /// <inheritdoc />
        float IShapeVolume.internalVolume => 0f;

        /// <inheritdoc />
        float IShapeVolume.surfaceArea => 0f;
    }
}
