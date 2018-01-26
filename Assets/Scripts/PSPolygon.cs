using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

sealed class PSPolygon
{
    public readonly Vector2[] points;
    private float[] constant = null;
    private float[] multiple = null;

    public PSPolygon(List<Vector2> points)
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

    public static float CalculateArea(Vector2[] p)
    {
        int n = p.Length;
        float sum = p[0].x * (p[1].y - p[n - 1].y);
        for (int i = 1; i < n - 1; ++i)
        {
            sum += p[i].x * (p[i + 1].y - p[i - 1].y);
        }
        sum += p[n - 1].x * (p[0].y - p[n - 2].y);
        return Mathf.Abs(0.5f * sum);
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
}
