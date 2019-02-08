using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ClipperLib;
using ClipperLibPolygon = System.Collections.Generic.List<ClipperLib.IntPoint>;
using ClipperLibPolygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

public sealed class PSPolygon
{
    public readonly Vector2[] points;
    private float[] constant = null;
    private float[] multiple = null;

    public PSPolygon(IEnumerable<Vector2> points)
    {
        this.points = points.ToArray();
    }

    private float _Area = -1;
    public float Area
    {
        get
        {
            if (_Area < 0)
            {
                _Area = CalculateArea(points);
            }
            return _Area;
        }
    }

    private Rect _Bounds;
    public Rect Bounds
    {
        get
        {
            if (_Bounds == Rect.zero)
            {
                _Bounds = CalculateBounds(points);
            }
            return _Bounds;
        }
    }

    private void precalcValues()
    {
        if (constant == null)
        {
            var polyCorners = points.Length;
            constant = new float[polyCorners];
            multiple = new float[polyCorners];
            int i, j = polyCorners - 1;
            for (i = 0; i < polyCorners; i++)
            {
                if (points[j].y == points[i].y)
                {
                    constant[i] = points[i].x;
                    multiple[i] = 0;
                }
                else
                {
                    constant[i] = points[i].x - (points[i].y * points[j].x) / (points[j].y - points[i].y) + (points[i].y * points[i].x) / (points[j].y - points[i].y);
                    multiple[i] = (points[j].x - points[i].x) / (points[j].y - points[i].y);
                }
                j = i;
            }
        }
    }

    public bool PointInPolygon(Vector2 point)
    {
        precalcValues();
        var polyCorners = points.Length;
        float x = point.x;
        float y = point.y;
        bool oddNodes = false, current = points[polyCorners - 1].y > y, previous;
        for (int i = 0; i < polyCorners; i++)
        {
            previous = current;
            current = points[i].y > y;
            if (current != previous)
            {
                oddNodes ^= y * multiple[i] + constant[i] < x;
            }
        }
        return oddNodes;
    }

    public static float CalculateSignedArea(Vector2[] p)
    {
        int n = p.Length;
        float sum = p[0].x * (p[1].y - p[n - 1].y);
        for (int i = 1; i < n - 1; ++i)
        {
            sum += p[i].x * (p[i + 1].y - p[i - 1].y);
        }
        sum += p[n - 1].x * (p[0].y - p[n - 2].y);
        return 0.5f * sum;
    }

    public static float CalculateArea(Vector2[] p)
    {
        return Mathf.Abs(CalculateSignedArea(p));
    }

    public static Rect CalculateBounds(Vector2[] p)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 point in p)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public static List<PSPolygon> difference(PSPolygon subject, PSPolygon clip)
    {
        return PSClipperHelper.difference(subject.points, clip.points).Select(p => new PSPolygon(p)).ToList();
    }
}

public sealed class PSEdge
{
    public readonly Vector2 v0;
    public readonly Vector2 v1;
    private Vector2 s;
    public PSEdge(Vector2 v0, Vector2 v1)
    {
        this.v0 = v0;
        this.v1 = v1;
        this.s = v1 - v0;
    }

    public float IntersectMagnitude(Vector2 direction)
    {
        Vector2 r = direction;
        float rxs = Cross(r, s);

        if (rxs == 0f) return 0f; // Parallel with the segment
        float rxsr = 1f / rxs;

        float u = Cross(v0, r) * rxsr;
        if (u < 0f || u > 1f) return 0f; // Outside of the segment

        return Cross(v0, s) * rxsr;
    }

    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public static float PointDistanceToEdge(Vector2 p, Vector2 v0, Vector2 v1)
    {
        Vector2 s = v1 - v0;
        Vector2 r = new Vector2(s.y, -s.x).normalized;
        float rxs = Cross(r, s);

        if (rxs == 0f) return 0f; // Parallel with the segment
        float rxsr = 1f / rxs;

        float u = (Cross(v0, r) - Cross(p, r)) * rxsr;
        if (u < 0f) return (p - v0).magnitude;
        if (u > 1f) return (p - v1).magnitude;

        return Mathf.Abs((Cross(v0, s) - Cross(p, s)) * rxsr);
    }

    public static bool SegmentsCross(Vector2 p0, Vector2 r, Vector2 v0, Vector2 s)
    {
        float rxs = Cross(r, s);
        if (rxs == 0f) return false; // Parallel
        float rxsr = 1f / rxs;
        float u = (Cross(v0, r) - Cross(p0, r)) * rxsr;
        if (u < 0f || u > 1f) return false;
        float t = (Cross(v0, s) - Cross(p0, s)) * rxsr;
        return t >= 0f && t <= 1f;
    }
}

public sealed class PSClipperHelper
{
    private static float multiplier = 1000;

    public static IEnumerable<IEnumerable<Vector2>> difference(IEnumerable<Vector2> subject, IEnumerable<Vector2> clip)
    {
        return operation(subject, clip, ClipType.ctDifference);
    }

    public static IEnumerable<IEnumerable<Vector2>> intersection(IEnumerable<Vector2> subject, IEnumerable<Vector2> clip)
    {
        return operation(subject, clip, ClipType.ctIntersection);
    }

    public static IEnumerable<IEnumerable<Vector2>> intersection(IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip)
    {
        return operation(subjects, clip, ClipType.ctIntersection);
    }

    private static IEnumerable<IEnumerable<Vector2>> operation(IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, ClipType clipType)
    {
        ClipperLibPolygons result = new ClipperLibPolygons();
        Clipper c = new Clipper();
        c.ReverseSolution = true;
        c.AddPath(createPolygon(clip), PolyType.ptClip, true);
        c.AddPath(createPolygon(subject), PolyType.ptSubject, true);
        c.Execute(clipType, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        return result.Select(createPoints);
    }

    private static IEnumerable<IEnumerable<Vector2>> operation(IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip, ClipType clipType)
    {
        PolyTree tree = new PolyTree();
        Clipper c = new Clipper();
        c.ReverseSolution = true;
        c.AddPath(createPolygon(clip), PolyType.ptClip, true);
        c.AddPaths(subjects.Select(createPolygon).ToList(), PolyType.ptSubject, true);
        c.Execute(clipType, tree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        return tree.Childs.Select(toPath);
    }

    private static IEnumerable<Vector2> toPath(PolyNode node) {
        // TODO handle holes and polygons inside holes
        return createPoints(node.Contour);
    }

    private static ClipperLibPolygon createPolygon(IEnumerable<Vector2> source)
    {
        return source.Select(p => new IntPoint(p.x * multiplier, p.y * multiplier)).ToList();
    }

    private static IEnumerable<Vector2> createPoints(IEnumerable<IntPoint> path)
    {
        return path.Select(p => new Vector2(p.X, p.Y) / multiplier);
    }
}