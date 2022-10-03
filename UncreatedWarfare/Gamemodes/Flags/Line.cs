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
    /// <summary>Slope</summary>
    internal readonly float m;
    /// <summary>Y-Intercept</summary>
    internal readonly float b;
    /// <summary>
    /// Create a line from two end points (order not important).
    /// </summary>
    public Line(in Vector2 pt1, in Vector2 pt2)
    {
        this.Point1 = pt1;
        this.Point2 = pt2;
        this.m = (pt1.y - pt2.y) / (pt1.x - pt2.x);
        this.b = -1 * (m * pt1.x - pt1.y);
        this.Length = Vector2.Distance(pt1, pt2);
    }
    /// <summary>
    /// Create a line from two end points (order not important).
    /// </summary>
    public Line(float p1x, float p1z, float p2x, float p2z)
    {
        this.Point1 = new Vector2(p1x, p1z);
        this.Point2 = new Vector2(p2x, p2z);
        this.m = (p1z - p2z) / (p1x - p2x);
        this.b = -1 * (m * p1x - p1z);
        this.Length = Vector2.Distance(this.Point1, this.Point2);
    }
    /// <summary>
    /// Round to the closest spacing that can fit an exact number of points.
    /// </summary>
    public readonly float NormalizeSpacing(float baseSpacing)
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
    public readonly bool IsIntersecting(float xPos, float yPos)
    {
        // if the y doesn't line up, return false.
        if (yPos < Mathf.Min(Point1.y, Point2.y) || yPos >= Mathf.Max(Point1.y, Point2.y)) return false;
        // if the line is completely vertical, return depending on what side of the line they're on
        if (Point1.x == Point2.x) return Point1.x >= xPos;
        float x = GetX(yPos);
        return x >= xPos;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float GetX(float y) => (y - b) / m; // inverse slope function
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float GetY(float x) => m * x + b;   // slope function
    /// <summary>
    /// Gets the point on the line <paramref name="alpha"/> through it starting at <see cref="Point1"/>.
    /// </summary>
    public readonly Vector2 GetPointAcrossP1(float alpha)
    {
        if (Point1.x == Point2.x) return new Vector2(Point1.x, Point1.y + (Point2.y - Point1.y) * alpha);
        float x = Point1.x + ((Point2.x - Point1.x) * alpha);
        return new Vector2(x, GetY(x));
    }
    /// <summary>
    /// Gets the point on the line <paramref name="distance"/> meters from <see cref="Point1"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 GetPointFromP1(float distance) => GetPointAcrossP1(distance / Length);
    /// <summary>
    /// Gets the point on the line <paramref name="alpha"/> through it starting at <see cref="Point2"/>.
    /// </summary>
    public readonly Vector2 GetPointAcrossP2(float alpha)
    {
        if (Point2.x == Point1.x) return new Vector2(Point2.x, Point2.y + (Point1.y - Point2.y) * alpha);
        float x = Point2.x + (Point1.x - Point2.x) * alpha;
        return new Vector2(x, GetY(x));
    }
    /// <summary>
    /// Gets the point on the line <paramref name="distance"/> meters from <see cref="Point2"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 GetPointFromP2(float distance) => GetPointAcrossP2(distance / Length);
}