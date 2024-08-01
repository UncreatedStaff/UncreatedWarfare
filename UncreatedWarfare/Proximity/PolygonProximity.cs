using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Uncreated.Warfare.Proximity;

/// <inheritdoc />
public class PolygonProximity : IPolygonProximity
{
    private readonly Bounds _bounds;
    private readonly float _minHeight, _maxHeight;
    private readonly Vector2[] _points;
    internal readonly PolygonLineInfo[] Lines;
    private ReadOnlyCollection<Vector2>? _pointsReadOnly;

    /// <inheritdoc />
    public float? MinHeight => float.IsNaN(_minHeight) ? null : _minHeight;

    /// <inheritdoc />
    public float? MaxHeight => float.IsNaN(_maxHeight) ? null : _maxHeight;

    /// <inheritdoc />
    public IReadOnlyList<Vector2> Points => _pointsReadOnly ??= new ReadOnlyCollection<Vector2>(_points);

    /// <inheritdoc />
    public Bounds worldBounds => _bounds;

    /// <inheritdoc />
    public float internalVolume { get; }

    /// <inheritdoc />
    public float surfaceArea { get; }

    /// <summary>
    /// Create a polygon with a set of points.
    /// </summary>
    /// <param name="points">A set of points with at least 3 elements.</param>
    /// <param name="minHeight">Optional minimum Y value.</param>
    /// <param name="maxHeight">Optional maximum Y value.</param>
    /// <exception cref="ArgumentException"><paramref name="points"/> has less than 3 elements or one of the points isn't finite.</exception>
    public PolygonProximity(IEnumerable<Vector2> points, float? minHeight, float? maxHeight) : this(points.ToArray(), minHeight, maxHeight) { }

    /// <summary>
    /// Create a polygon with a raw array of points.
    /// </summary>
    /// <param name="points">An array of points with at least 3 elements.</param>
    /// <param name="minHeight">Optional minimum Y value.</param>
    /// <param name="maxHeight">Optional maximum Y value.</param>
    /// <exception cref="ArgumentException"><paramref name="points"/> has less than 3 elements or one of the points isn't finite.</exception>
    internal PolygonProximity(Vector2[] points, float? minHeight, float? maxHeight)
    {
        if (points.Length < 3)
            throw new ArgumentException("Must have at least 3 points.", nameof(points));
        
        _points = points;
        _minHeight = minHeight.HasValue && float.IsFinite(minHeight.Value) ? minHeight.Value : float.NaN;
        _maxHeight = maxHeight.HasValue && float.IsFinite(maxHeight.Value) ? maxHeight.Value : float.NaN;

        Lines = new PolygonLineInfo[_points.Length];

        float height;
        if (float.IsNaN(_minHeight))
        {
            if (float.IsNaN(_maxHeight))
                height = Level.HEIGHT * 2;
            else
                height = Level.HEIGHT + _maxHeight;
        }
        else if (float.IsNaN(_maxHeight))
        {
            height = Level.HEIGHT - _minHeight;
        }
        else
            height = _maxHeight - _minHeight;

        // Area of a polygon: https://web.archive.org/web/20100405070507/http://valis.cs.uiuc.edu/~sariel/research/CG/compgeom/msg00831.html
        float ttlArea = 0;
        float sideSurfaceArea = 0;
        Vector2 max = points[0], min = points[0];
        for (int i = 0; i < _points.Length; ++i)
        {
            ref Vector2 point1 = ref _points[i];
            if (!float.IsFinite(point1.x) || !float.IsFinite(point1.y))
                throw new ArgumentException($"Not finite at index {i}.", nameof(points));

            ref Vector2 point2 = ref _points[(i + 1) % _points.Length];
            Lines[i] = new PolygonLineInfo(in point1, in point2);

            ttlArea += point1.x * point2.y - point1.y * point2.x;
            sideSurfaceArea += Lines[i].Length * height;

            if (i == 0)
                continue;

            if (point1.x > max.x)
                max.x = point1.x;
            else if (point1.x < min.x)
                min.x = point1.x;

            if (point1.y > max.y)
                max.y = point1.y;
            else if (point1.y < min.y)
                min.y = point1.y;
        }

        ttlArea = Mathf.Abs(ttlArea) / 2f;

        internalVolume = ttlArea * height;
        surfaceArea = ttlArea * 2 + sideSurfaceArea;

        _bounds.SetMinMax(min, max);
    }

    private PolygonProximity(Bounds bounds, float minHeight, float maxHeight, Vector2[] points, PolygonLineInfo[] lines, ReadOnlyCollection<Vector2>? pointsReadOnly, float area, float volume)
    {
        _bounds = bounds;
        _minHeight = minHeight;
        _maxHeight = maxHeight;
        _points = points;
        Lines = lines;
        _pointsReadOnly = pointsReadOnly;
        surfaceArea = area;
        internalVolume = volume;
    }

    /// <inheritdoc />
    public bool TestPoint(Vector3 position)
    {
        Vector3 size = _bounds.size;
        Vector3 min = _bounds.min;

        // check bounds first
        if (!(float.IsInfinity(size.x) || position.x > min.x && position.x < min.x + size.x)
         || !(float.IsInfinity(size.y) || position.y > min.y && position.y < min.y + size.y)
         || !(float.IsInfinity(size.z) || position.z > min.z && position.z < min.z + size.z))
        {
            return false;
        }

        if (!float.IsNaN(_minHeight) && position.y < _minHeight
         || !float.IsNaN(_maxHeight) && position.y > _maxHeight)
        {
            return false;
        }

        return IsInsidePolygon(position.x, position.z);
    }

    /// <inheritdoc />
    public bool TestPoint(Vector2 position)
    {
        Vector3 size = _bounds.size;
        Vector3 min = _bounds.min;

        // check bounds first
        if (!(float.IsInfinity(size.x) || position.x > min.x && position.x < min.x + size.x)
         || !(float.IsInfinity(size.z) || position.y > min.z && position.y < min.z + size.z))
        {
            return false;
        }

        return IsInsidePolygon(position.x, position.y);
    }

    private bool IsInsidePolygon(float x, float y)
    {
        int intersects = 0;
        for (int i = 0; i < Lines.Length; i++)
        {
            ref PolygonLineInfo line = ref Lines[i];

            ref Vector2 point1 = ref _points[i];
            ref Vector2 point2 = ref _points[(i + 1) % _points.Length];

            if (y < Math.Min(point1.y, point2.y) || y >= Math.Max(point1.y, point2.y))
                continue;

            if (Math.Abs(point1.x - point2.x) < 0.001f)
            {
                if (point2.x >= x)
                    ++intersects;
                continue;
            }

            float xPos = (y - line.Intercept) / line.Slope;
            if (xPos >= x)
                ++intersects;
        }

        return intersects % 2 == 1;
    }

    internal static void CalculateAreaAndVolume(Transform transform, float? minHeight, float? maxHeight, IReadOnlyList<Vector2> points, out float surfaceArea, out float volume)
    {
        ThreadUtil.assertIsGameThread();

        float ttlArea = 0;
        float sideSurfaceArea = 0;
        float height;
        if (!minHeight.HasValue)
        {
            if (!maxHeight.HasValue)
                height = Level.HEIGHT * 2;
            else
                height = Level.HEIGHT + maxHeight.Value;
        }
        else if (!maxHeight.HasValue)
        {
            height = Level.HEIGHT - minHeight.Value;
        }
        else
            height = maxHeight.Value - minHeight.Value;

        height = transform.TransformVector(new Vector3(0, height, 0)).magnitude;

        Vector2[] pointsArr = points.Select(pt =>
        {
            Vector3 pt2 = transform.TransformPoint(new Vector3(pt.x, 0f, pt.y));
            return new Vector2(pt2.x, pt2.z);
        }).ToArray();

        for (int i = 0; i < pointsArr.Length; ++i)
        {
            pointsArr[i] = transform.TransformPoint(pointsArr[i]);
            Vector2 point1 = pointsArr[i];
            Vector2 point2 = pointsArr[(i + 1) % pointsArr.Length];
            if (!float.IsFinite(point1.x) || !float.IsFinite(point1.y))
                throw new NotSupportedException($"Point not finite at index {i}.");

            ttlArea += point1.x * point2.y - point1.y * point2.x;
            sideSurfaceArea += Vector2.Distance(point1, point2) * height;
        }

        ttlArea = Mathf.Abs(ttlArea) / 2f;

        volume = ttlArea * height;
        surfaceArea = ttlArea * 2 + sideSurfaceArea;
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new PolygonProximity(_bounds, _minHeight, _maxHeight, _points, Lines, _pointsReadOnly, surfaceArea, internalVolume);
    }
}
