using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// A polygon attached to a transform. Scale, position, and rotation are applied.
/// </summary>
public class AttachedPolygonProximity : IAttachedPolygonProximity
{
    private readonly IPolygonProximity _polygon;
    private float _volume;
    private float _surfaceArea;
    private float _area;
    private Vector3 _lastScale = Vector3.one;

    /// <inheritdoc />
    public IReadOnlyList<Vector2> Points => _polygon.Points;

    /// <inheritdoc />
    public float? MinHeight => _polygon.MinHeight;

    /// <inheritdoc />
    public float? MaxHeight => _polygon.MaxHeight;

    /// <inheritdoc />
    public Bounds worldBounds
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            Bounds bounds = _polygon.worldBounds;
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

            if (AttachmentRoot == null)
                return _polygon.internalVolume;

            Vector3 scale = AttachmentRoot.lossyScale;
            if (scale.IsNearlyEqual(Vector3.one))
                return _polygon.internalVolume;

            if (scale.IsNearlyEqual(_lastScale))
                return _volume;

            PolygonProximity.CalculateAreaAndVolume(AttachmentRoot, MinHeight, MaxHeight, Points, out float area, out float surfaceArea, out float volume);
            _area = area;
            _surfaceArea = surfaceArea;
            _volume = volume;
            _lastScale = scale;

            return volume;
        }
    }

    /// <inheritdoc />
    float IShapeVolume.surfaceArea
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            if (AttachmentRoot == null)
                return _polygon.surfaceArea;
            
            Vector3 scale = AttachmentRoot.lossyScale;
            if (scale.IsNearlyEqual(Vector3.one))
                return _polygon.surfaceArea;

            if (scale.IsNearlyEqual(_lastScale))
                return _surfaceArea;

            PolygonProximity.CalculateAreaAndVolume(AttachmentRoot, MinHeight, MaxHeight, Points, out float area, out float surfaceArea, out float volume);
            _area = area;
            _surfaceArea = surfaceArea;
            _volume = volume;
            _lastScale = scale;

            return surfaceArea;
        }
    }

    /// <inheritdoc />
    float IProximity.Area
    {
        get
        {
            ThreadUtil.assertIsGameThread();

            if (AttachmentRoot == null)
                return _polygon.Area;
            
            Vector3 scale = AttachmentRoot.lossyScale;
            if (scale.IsNearlyEqual(Vector3.one))
                return _polygon.Area;

            if (scale.IsNearlyEqual(_lastScale))
                return _area;

            PolygonProximity.CalculateAreaAndVolume(AttachmentRoot, MinHeight, MaxHeight, Points, out float area, out float surfaceArea, out float volume);
            _area = area;
            _surfaceArea = surfaceArea;
            _volume = volume;
            _lastScale = scale;

            return surfaceArea;
        }
    }

    /// <summary>
    /// Create a polygon with a set of points attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    /// <param name="points">A set of points with at least 3 elements.</param>
    /// <param name="minHeight">Optional minimum Y value.</param>
    /// <param name="maxHeight">Optional maximum Y value.</param>
    /// <exception cref="ArgumentException"><paramref name="points"/> has less than 3 elements or one of the points isn't finite.</exception>
    public AttachedPolygonProximity(Transform attachmentRoot, IEnumerable<Vector2> points, float? minHeight, float? maxHeight) : this(attachmentRoot, points.ToArray(), minHeight, maxHeight) { }

    /// <summary>
    /// Create a polygon with a raw array of points attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    /// <param name="points">An array of points with at least 3 elements.</param>
    /// <param name="minHeight">Optional minimum Y value.</param>
    /// <param name="maxHeight">Optional maximum Y value.</param>
    /// <exception cref="ArgumentException"><paramref name="points"/> has less than 3 elements or one of the points isn't finite.</exception>
    internal AttachedPolygonProximity(Transform attachmentRoot, Vector2[] points, float? minHeight, float? maxHeight)
    {
        AttachmentRoot = attachmentRoot;
        _polygon = new PolygonProximity(points, minHeight, maxHeight);
        _volume = _polygon.internalVolume;
        _surfaceArea = _polygon.surfaceArea;
    }

    /// <summary>
    /// Create a polygon from another <see cref="IAACylinderProximity"/> attached to <paramref name="attachmentRoot"/>.
    /// </summary>
    public AttachedPolygonProximity(Transform attachmentRoot, IPolygonProximity polygon)
    {
        AttachmentRoot = attachmentRoot;
        _polygon = polygon;
        _volume = _polygon.internalVolume;
        _surfaceArea = _polygon.surfaceArea;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? position : AttachmentRoot.InverseTransformPoint(position);
        return _polygon.TestPoint(worldPos);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        ThreadUtil.assertIsGameThread();

        Vector3 worldPos = AttachmentRoot == null ? position : AttachmentRoot.InverseTransformPoint(position);
        return _polygon.TestPoint(new Vector2(worldPos.x, worldPos.z));
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new AttachedPolygonProximity(AttachmentRoot!, _polygon);
    }
}
