using System;

namespace Uncreated.Warfare.Proximity;

/// <summary>
/// Represents a line formed by two points in a list of points.
/// </summary>
public struct PolygonLineInfo
{
    /// <summary>
    /// Length of the line.
    /// </summary>
    public readonly float Length;

    /// <summary>
    /// Slope of the line (dx / dy).
    /// </summary>
    internal readonly float Slope;

    /// <summary>
    /// Point at which the line intercepts the Y-axis.
    /// </summary>
    internal readonly float Intercept;
    public PolygonLineInfo(in Vector2 point1, in Vector2 point2)
    {
        float x = point1.x - point2.x,
              y = point1.y - point2.y;
        
        Length = MathF.Sqrt(x * x + y * y);
        Slope = y / x;
        Intercept = -(Slope * point1.x - point1.y);
    }
}
