using System.Drawing;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

/// <summary>Cached read-only data about a line.</summary>
public readonly struct Line
{
    /// <summary>An end point of the line.</summary>
    public readonly Vector2 Point1;
    /// <summary>An end point of the line.</summary>
    public readonly Vector2 Point2;
    /// <summary>Length of the line.</summary>
    public readonly float Length;
    /// <summary>Slope of the line.</summary>
    internal readonly float Slope;
    /// <summary>Y-Intercept of the line.</summary>
    internal readonly float Intercept;
    /// <summary>
    /// Create a line from two end points (order not important).
    /// </summary>
    public Line(in Vector2 pt1, in Vector2 pt2)
    {
        Point1 = pt1;
        Point2 = pt2;
        Slope = (pt1.y - pt2.y) / (pt1.x - pt2.x);
        Intercept = -1 * (Slope * pt1.x - pt1.y);
        Length = Vector2.Distance(pt1, pt2);
    }
    /// <summary>
    /// Create a line from two end points (order not important).
    /// </summary>
    public Line(float p1X, float p1Z, float p2X, float p2Z)
    {
        Point1 = new Vector2(p1X, p1Z);
        Point2 = new Vector2(p2X, p2Z);
        Slope = (p1Z - p2Z) / (p1X - p2X);
        Intercept = -1 * (Slope * p1X - p1Z);
        Length = Vector2.Distance(Point1, Point2);
    }
    /// <summary>
    /// Round to the closest spacing that can fit an exact number of points.
    /// </summary>
    public float NormalizeSpacing(float baseSpacing)
    {
        float answer = Length / baseSpacing;
        int remainder = Mathf.RoundToInt((answer - Mathf.Floor(answer)) * baseSpacing);
        int canfit = Mathf.FloorToInt(answer);
        if (remainder == 0) return baseSpacing;
        if (remainder < baseSpacing / 2)     // extend all others
            return Length / canfit;
        else                                //add one more and subtend all others
            return Length / (canfit + 1);
    }
    /// <summary>
    /// Checks if the input X is left of but on the same Y level as the line 
    /// </summary>
    public bool IsIntersecting(float xPos, float yPos)
    {
        // if the y doesn't line up, return false.
        Vector2 pt1 = Point1, pt2 = Point2;
        if (yPos < Mathf.Min(pt1.y, pt2.y) || yPos >= Mathf.Max(pt1.y, pt2.y)) return false;
        // if the line is completely vertical, return depending on what side of the line they're on
        if (pt1.x == pt2.x) return pt1.x >= xPos;
        float x = GetX(yPos);
        return x >= xPos;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetX(float y) => (y - Intercept) / Slope; // inverse slope function
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GetY(float x) => Slope * x + Intercept;   // slope function
    /// <summary>
    /// Gets the point on the line <paramref name="alpha"/> through it starting at <see cref="Point1"/>.
    /// </summary>
    public Vector2 GetPointAcrossP1(float alpha)
    {
        if (Point1.x == Point2.x) return new Vector2(Point1.x, Point1.y + (Point2.y - Point1.y) * alpha);
        float x = Point1.x + ((Point2.x - Point1.x) * alpha);
        return new Vector2(x, GetY(x));
    }
    /// <summary>
    /// Gets the point on the line <paramref name="distance"/> meters from <see cref="Point1"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetPointFromP1(float distance) => GetPointAcrossP1(distance / Length);
    /// <summary>
    /// Gets the point on the line <paramref name="alpha"/> through it starting at <see cref="Point2"/>.
    /// </summary>
    public Vector2 GetPointAcrossP2(float alpha)
    {
        if (Point2.x == Point1.x) return new Vector2(Point2.x, Point2.y + (Point1.y - Point2.y) * alpha);
        float x = Point2.x + (Point1.x - Point2.x) * alpha;
        return new Vector2(x, GetY(x));
    }
    /// <summary>
    /// Gets the point on the line <paramref name="distance"/> meters from <see cref="Point2"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetPointFromP2(float distance) => GetPointAcrossP2(distance / Length);
}