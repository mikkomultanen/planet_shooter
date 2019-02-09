using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using MoreLinq;
using UnityEngine;

class MeshToPolygonConverter {
    private struct Edge {
        public int v0;
        public int v1;
        public string key;

        public Edge(int v0, int v1)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.key = Mathf.Min(v0, v1) + ":" + Mathf.Max(v0, v1);
        }
    }

    public static IEnumerable<PSEdge> ContourEdges(List<Vector2> vertices, List<int> triangles)
    {
        return Edges(vertices, triangles).Values.Select(e => new PSEdge(vertices[e.v0], vertices[e.v1]));
    }

    public static List<PSPolygon> ContourPolygons(List<Vector2> vertices, List<int> triangles)
    {
        Dictionary<string, Edge> edges = new Dictionary<string, Edge>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                Edge edge = new Edge(triangles[i + e], triangles[i + e + 1 > i + 2 ? i : i + e + 1]);
                if (edges.ContainsKey(edge.key))
                {
                    edges.Remove(edge.key);
                }
                else
                {
                    edges.Add(edge.key, edge);
                }
            }
        }

        var lookup = edges.Values.ToLookup(e => e.v0);
        var polygons = new List<PSPolygon>();

        if (edges.Count == 0)
        {
            return polygons;
        }
        Edge startEdge = edges.Values.First();
        Edge nextEdge = startEdge;
        List<int> colliderPath = new List<int>();
        while (true)
        {
            colliderPath.Add(nextEdge.v0);
            var removed = edges.Remove(nextEdge.key);
            nextEdge = SelectEdge(vertices, nextEdge, lookup[nextEdge.v1]);

            if (nextEdge.key == startEdge.key)
            {
                var polygon = new PSPolygon(colliderPath.Select(index => vertices[index]));
                polygons.Add(polygon);

                colliderPath.Clear();
                if (edges.Count > 0)
                {
                    startEdge = edges.Values.First();
                    nextEdge = startEdge;

                    continue;
                }

                break;
            }
        }        

        return polygons;
    }

    public static IEnumerable<PSPolygon> FragmentPolygons(IEnumerable<PSPolygon> contours, IEnumerable<Vector2> clip)
    {
        return PSClipperHelper.intersection(contours.Select(c => c.points), clip).Select(p => new PSPolygon(p));
    }

    public static IEnumerable<PSPolygon> InsidePolygons(IEnumerable<Vector2> area, IEnumerable<PSPolygon> contours, float offset)
    {
        return PSClipperHelper.inverseWithOffset(area, contours.Select(c => c.points), offset).Select(p => new PSPolygon(p));
    }

    private static Dictionary<string, Edge> Edges(List<Vector2> vertices, List<int> triangles)
    {
        Dictionary<string, Edge> edges = new Dictionary<string, Edge>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                Edge edge = new Edge(triangles[i + e], triangles[i + e + 1 > i + 2 ? i : i + e + 1]);
                if (edges.ContainsKey(edge.key))
                {
                    edges.Remove(edge.key);
                }
                else
                {
                    edges.Add(edge.key, edge);
                }
            }
        }

        return edges;
    }

    private static Edge SelectEdge(List<Vector2> vertices, Edge from, IEnumerable<Edge> nextEdges)
    {
        Vector2 edgeV = vertices[from.v1] - vertices[from.v0];
        return nextEdges.MinBy(e => Vector2.SignedAngle(edgeV, (vertices[e.v1] - vertices[e.v0])));
    }
}

