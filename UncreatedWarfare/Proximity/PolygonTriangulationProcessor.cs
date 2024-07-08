using System;
using UnityEngine;

namespace Uncreated.Warfare.Proximity;

/*
 * Inspired by the information contained in the following lecture notes.
 * https://cs.gmu.edu/~jmlien/teaching/cs499-GC/uploads/Main/note02.pdf
 */

/// <summary>
/// Tool to split up polygons into triangles.
/// </summary>
public readonly ref struct PolygonTriangulationProcessor
{
    private readonly VertexInfo[] _vertices;
    private readonly int _pointCount;
    private readonly int _vertexIndexOffset;
    public PolygonTriangulationProcessor(Vector2[] points, int vertexIndexOffset)
    {
        _pointCount = points.Length;

        _vertexIndexOffset = vertexIndexOffset;

        _vertices = new VertexInfo[_pointCount];

        for (int i = 0; i < _pointCount; ++i)
        {
            ref VertexInfo vert = ref _vertices[i];
            vert.Vertices = _vertices;
            vert.Index = i;
            vert.Point = points[i];
        }
    }

    public int WriteTriangles(ArraySegment<int> tris, int triCount = -1)
    {
        if (tris.Count < (_pointCount - 2) * 3)
            throw new ArgumentException("Tri's segment must be at least (points.Count - 2) * 3.", nameof(tris));

        if (triCount == 0)
        {
            return 0;
        }

        if (triCount < 0)
        {
            triCount = _pointCount - 2;
        }

        for (int i = 0; i < _pointCount; ++i)
        {
            ref VertexInfo vert = ref _vertices[i];
            vert.PrevIndex = (i == 0 ? _pointCount : i) - 1;
            vert.NextIndex = (i + 1) % _pointCount;
        }

        for (int i = 0; i < _pointCount; ++i)
        {
            ref VertexInfo vert = ref _vertices[i];

            RecalcArea(ref vert);
            vert.IsEar = IsEar(in vert);

            Debug.Log($"{vert.PrevIndex}-{i}-{vert.NextIndex}: Is ear: {vert.IsEar}. Area: {vert.Area:F6}.");
        }

        int[] triArray = tris.Array!;
        int triOffset = tris.Offset;

        int vertsLeft = _pointCount;
        int breakCt = 0;

        int startIndex = 0;
        int triCountProgress = 0;
        while (vertsLeft > 3)
        {
            ref VertexInfo vertex = ref _vertices[startIndex];
            do
            {
                ++breakCt;
                if (breakCt > 1000)
                {
                    throw new TimeoutException($"Failed to triangulate at n={vertsLeft}.");
                }

                if (!vertex.IsEar)
                {
                    vertex = ref vertex.Next;
                    continue;
                }

                ref VertexInfo prev = ref vertex.Prev;
                ref VertexInfo next = ref vertex.Next;

                prev.NextIndex = vertex.NextIndex;
                next.PrevIndex = vertex.PrevIndex;

                RecalcArea(ref prev);
                RecalcArea(ref next);
                prev.IsEar = IsEar(in prev);
                next.IsEar = IsEar(in next);

                WriteCounterClockwiseTriangle(triArray, triOffset, in vertex);
                triOffset += 3;
                ++triCountProgress;
                if (triCountProgress == triCount)
                {
                    return triCountProgress;
                }
                --vertsLeft;
                startIndex = vertex.NextIndex;
                break;
            } while (vertex.Index != startIndex);
        }

        WriteCounterClockwiseTriangle(triArray, triOffset, in _vertices[startIndex]);

        for (int i = 0; i < _pointCount; ++i)
        {
            ref VertexInfo vert = ref _vertices[i];
            Debug.Log($"{vert.PrevIndex}-{i}-{vert.NextIndex}: Is ear: {vert.IsEar}. Area: {vert.Area:F6}.");
        }

        return triCountProgress + 1;
    }

    private static void RecalcArea(ref VertexInfo vert)
    {
        vert.Area = GetTriArea(in vert);
        if (Math.Abs(vert.Area) <= float.Epsilon)
        {
            vert.Area = 0f;
        }
    }

    private void WriteCounterClockwiseTriangle(int[] tris, int triOffset, in VertexInfo vertex)
    {
        tris[triOffset] = vertex.NextIndex + _vertexIndexOffset;
        tris[triOffset + 1] = vertex.Index + _vertexIndexOffset;
        tris[triOffset + 2] = vertex.PrevIndex + _vertexIndexOffset;
    }

    // an 'ear' is a vertex that's on the convex hull that could be triangulated.
    private static bool IsEar(in VertexInfo vertex)
    {
        return IsNonIntersectingDiagonal(in vertex.Prev, in vertex.Next);
    }

    // ReSharper disable InconsistentNaming
    private static bool AreSegmentsIntersecting(in Vector2 seg1v1, in Vector2 seg1v2, in Vector2 seg2v1, in Vector2 seg2v2)
    {
        if ((IsCounterClockwise(in seg1v1, in seg1v2, in seg2v1) ^ IsCounterClockwise(in seg1v1, in seg1v2, in seg2v2))
            && (IsCounterClockwise(in seg2v1, in seg2v2, in seg1v1) ^ IsCounterClockwise(in seg2v1, in seg2v2, in seg1v2)))
        {
            return true;
        }

        return IsPointOnLine(in seg1v1, in seg1v2, in seg2v1)
            || IsPointOnLine(in seg1v1, in seg1v2, in seg2v2)
            || IsPointOnLine(in seg2v1, in seg2v2, in seg1v1)
            || IsPointOnLine(in seg2v1, in seg2v2, in seg1v2);
    }
    // ReSharper restore InconsistentNaming

    private static bool IsPointOnLine(in Vector2 p1, in Vector2 p2, in Vector2 testPoint)
    {
        if (!IsCollinear(in p1, in p2, in testPoint))
        {
            return false;
        }

        if (Math.Abs(p1.x - p2.x) <= float.Epsilon)
        {
            return testPoint.x >= p1.x && testPoint.x <= p2.x || testPoint.x >= p2.x && testPoint.x <= p1.x;
        }

        return testPoint.y >= p1.y && testPoint.y <= p2.y || testPoint.y >= p2.y && testPoint.y <= p1.y;
    }

    private static bool IsNonIntersectingDiagonal(in VertexInfo v1, in VertexInfo v2)
    {
        if (!IsPointInternalToVertex(in v1, in v2.Point) || !IsPointInternalToVertex(in v2, v1.Point))
        {
            return false;
        }

        for (int i = 0; i < v1.Vertices.Length; ++i)
        {
            ref VertexInfo vert = ref v1.Vertices[i];
            ref VertexInfo next = ref v1.Vertices[(i + 1) % v1.Vertices.Length];

            if (vert.Index == v1.Index || vert.Index == v2.Index
                || next.Index == v1.Index || next.Index == v2.Index)
            {
                continue;
            }

            if (AreSegmentsIntersecting(in v1.Point, in v2.Point, in vert.Point, in next.Point))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPointInternalToVertex(in VertexInfo vertex, in Vector2 testPoint)
    {
        if (IsCounterClockwiseOrCollinear(in vertex))
        {
            return IsCounterClockwise(in vertex.Point, in vertex.Next.Point, in testPoint) && IsCounterClockwise(in vertex.Point, in testPoint, in vertex.Prev.Point);
        }

        return IsClockwiseOrCollinear(in vertex.Point, in testPoint, in vertex.Next.Point) || IsClockwiseOrCollinear(in testPoint, in vertex.Point, in vertex.Prev.Point);
    }

    private static bool IsCounterClockwiseOrCollinear(in VertexInfo vertex)
    {
        return vertex.Area >= 0;
    }

    private static bool IsCounterClockwise(in Vector2 a, in Vector2 b, in Vector2 c)
    {
        return GetTriArea(in a, in b, in c) > 0;
    }

    private static bool IsClockwiseOrCollinear(in Vector2 a, in Vector2 b, in Vector2 c)
    {
        return GetTriArea(in a, in b, in c) <= 0;
    }

    private static bool IsCollinear(in Vector2 a, in Vector2 b, in Vector2 c)
    {
        return Math.Abs(GetTriArea(in a, in b, in c)) <= float.Epsilon;
    }

    private static float GetTriArea(in VertexInfo vertex)
    {
        ref VertexInfo next = ref vertex.Next;
        ref VertexInfo prev = ref vertex.Prev;

        return (vertex.Point.x - prev.Point.x) * (next.Point.y - prev.Point.y) - (next.Point.x - prev.Point.x) * (vertex.Point.y - prev.Point.y);
    }
    private static float GetTriArea(in Vector2 prev, in Vector2 vertex, in Vector2 next)
    {
        return (vertex.x - prev.x) * (next.y - prev.y) - (next.x - prev.x) * (vertex.y - prev.y);
    }

    private struct VertexInfo
    {
        public VertexInfo[] Vertices;
        public int Index;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public bool IsEar;
        public int NextIndex;
        public int PrevIndex;
        public Vector2 Point;
        public float Area;

        public ref VertexInfo Next => ref Vertices[NextIndex];
        public ref VertexInfo Prev => ref Vertices[PrevIndex];
    }
}
