using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Vector2EqualComparer : IEqualityComparer<Vector2>
{
    public static Vector2EqualComparer Instance = new Vector2EqualComparer();

    public bool Equals(Vector2 x, Vector2 y)
    {
        return (x - y).sqrMagnitude < 0.01f;
    }
    public int GetHashCode(Vector2 obj)
    {
        return new Vector2Int(Mathf.RoundToInt(obj.x), Mathf.RoundToInt(obj.y)).GetHashCode();
    }
}

public class TerrainSystem : MonoBehaviour
{
    public float innerRadius = 35;
    public float outerRadius = 125;
    public float tiling = 6;
    [Range(1,4)]
    public int iterations;
    [Range(0,1)]
    public float threshold;
    [Range(0,1)]
    public float thresholdAmplitude;
    [Range(0,1)]
    public float innerThreshold;
    [Range(0,1)]
    public float outerThreshold;
    public int terrainSeed;
    [Range(0,10)]
    public float insideOffset = 2;
    public float insideInnerRadius = 50;

    private bool _ready = false;
    public bool Ready { get { return _ready; }}
    private FastNoise fastNoise = new FastNoise(0);
#if UNITY_EDITOR
    private List<PSPolygon> editorPolygons = new List<PSPolygon>();
    private List<PSEdge> editorEdges = new List<PSEdge>();
    private List<Vector2> editorPoints = new List<Vector2>();
#endif

#if UNITY_EDITOR
    [ContextMenu("Generate terrain")]
#endif
    private void GenerateTerrain()
    {
        fastNoise.SetSeed(terrainSeed);
        fastNoise.SetFrequency(tiling / (outerRadius * 2));
        Debug.Log("GenerateTerrain start");
        DeleteTerrain();
        int r = Mathf.RoundToInt(outerRadius + 1);

        var allPoints = new ConcurrentBag<Vector2>();
        Parallel.For(-r, r, x => {
            Vector2 v00, v10, v01, v11;
            bool b00, b10, b01, b11;
            for (int y = -r; y < r; y++)
            {
                v00 = new Vector2(x, y);
                v10 = new Vector2(x + 1, y);
                v01 = new Vector2(x, y + 1);
                v11 = new Vector2(x + 1, y + 1);
                b00 = insideCave(v00);
                b10 = insideCave(v10);
                b01 = insideCave(v01);
                b11 = insideCave(v11);

                if (b00 != b10)
                {
                    allPoints.Add(findBorder(v00, b00, v10, b10));
                }
                if (b00 != b01)
                {
                    allPoints.Add(findBorder(v00, b00, v01, b01));
                }
                if (b11 != b10)
                {
                    allPoints.Add(findBorder(v11, b11, v10, b10));
                }
                if (b11 != b01)
                {
                    allPoints.Add(findBorder(v11, b11, v01, b01));
                }
            }
        });

        var points = allPoints.Distinct(Vector2EqualComparer.Instance).ToList();

        Debug.Log("GenerateTerrain start generate triangles");
        List<Vector2> vertices;
        List<int> triangles;
        GenerateTerrainTriangles(points, out vertices, out triangles);
        Debug.Log("GenerateTerrain start generate contours");
        var contours = MeshToPolygonConverter.ContourPolygons(vertices, triangles).ToList();
        Debug.Log("GenerateTerrain start generate polygons");
        var segments = new Vector2[][]{
            new Vector2[]{new Vector2(0,0), new Vector2(-r,0), new Vector2(-r,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(-r,r), new Vector2(0,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(0,r), new Vector2(r,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,r), new Vector2(r,0)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,0), new Vector2(r,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,-r), new Vector2(0,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(0,-r), new Vector2(-r,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(-r,-r), new Vector2(-r,0)}
        };
        var polygons = segments.AsParallel().SelectMany(segment => MeshToPolygonConverter.FragmentPolygons(contours, segment)).ToList();
        var area = new Vector2[]{
            new Vector2(r,r), new Vector2(r,-r), new Vector2(-r,-r), new Vector2(-r,r)
        };
        Debug.Log("GenerateTerrain start generate inside points");
        var insidePolygons = MeshToPolygonConverter.InsidePolygons(area, polygons, -insideOffset).ToList();
        insidePolygons.ForEach(p => p.Precalc());
        var insideLookup = insidePolygons.ToLookup(p => p.IsHole);
        var magnitudeStep = 1f;
        var magnitudeCount = Mathf.CeilToInt((outerRadius - insideInnerRadius) / magnitudeStep);
        var insidePoints = Enumerable.Range(0, 360).AsParallel().Select(i => {
            var angle = i * Mathf.PI / 180f;
            var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            return Enumerable.Range(0, magnitudeCount).Select(j => direction * (insideInnerRadius + j * magnitudeStep)).Where(p => {
                return insideLookup[false].Any(c => c.PointInPolygon(p)) && insideLookup[true].All(c => !c.PointInPolygon(p));
            }).ToList();
        }).Where(l => l.Count > 0).ToList();
#if UNITY_EDITOR
        Debug.Log("GenerateTerrain start generate mesh");
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        var verts = new List<Vector2>();
        var tris = new List<int>();
        mesh.vertices = vertices.Select(p => new Vector3(p.x, p.y, 0)).ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;

        editorPoints = insidePoints.SelectMany(l => l).ToList();
        editorPolygons = insidePolygons;
        //editorEdges = MeshToPolygonConverter.ContourEdges(vertices, triangles).ToList();//editorPolygons.SelectMany(getFloorEdges).ToList();
#endif

        _ready = true;
        Debug.Log("GenerateTerrain done");
    }

    private Vector2 findBorder(Vector2 v0, bool b0, Vector2 v1, bool b1)
    {
        Vector2 inV = b0 ? v0 : v1;
        Vector2 outV = b0 ? v1 : v0;
        Vector2 middleV;
        int iterations = 0;
        do
        {
            middleV = (inV + outV) * 0.5f;
            if (insideCave(middleV))
            {
                inV = middleV;
            }
            else
            {
                outV = middleV;
            }
            iterations++;
        } while ((inV - outV).sqrMagnitude > 0.0001f && iterations < 10);
        return (inV + outV) * 0.5f;
    }

    private float fractalNoise(Vector2 position) {
        float o = 0;
        float w = 0.5f;
        float s = 1;
        for (int i = 0; i < iterations; i++) {
            Vector2 coord = position * s;
            float n = Mathf.Abs(fastNoise.GetSimplex(coord.x, coord.y));
            n = 1 - n;
            n *= n;
            n *= n;
            o += n * w;
            s *= 2.0f;
            w *= 0.5f;
        }
        return o;
    }

    private static float smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    private bool insideCave(Vector2 coord)
    {
        float d = coord.magnitude;
        if (d <= innerRadius || d >= outerRadius) return true;
        float n = fractalNoise(coord);
        float a = smoothstep(innerThreshold, innerThreshold + 0.1f, d / outerRadius) * (1 - smoothstep(outerThreshold - 0.1f, outerThreshold, d / outerRadius));
        float v = fastNoise.GetSimplex(coord.x, coord.y);
        return threshold + thresholdAmplitude * v <= n * a;
    }

    public static Vector2 getCenter(List<Vector2> coords)
    {
        return coords.Aggregate(Vector2.zero, (center, next) => center + next) / coords.Count;
    }
    public static int indexOf(List<Vector2> coords, Vector2 coord)
    {
        for (int i = 0; i < coords.Count; i++)
        {
            if ((coords[i] - coord).sqrMagnitude < 0.01)
            {
                return i;
            }
        }
        return -1;
    }
    public Vertex toVertex(Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }
    public Vector2 toVector2(Vertex vertex)
    {
        return new Vector2((float)vertex.X, (float)vertex.Y);
    }
    private void GenerateTerrainTriangles(List<Vector2> points, out List<Vector2> vertices, out List<int> triangles)
    {
        var imesh = (new GenericMesher().Triangulate(points.Select(toVertex).ToList()));
        vertices = new List<Vector2>();
        triangles = new List<int>();
        foreach (var triangle in imesh.Triangles)
        {
            var list = triangle.vertices.Select(toVector2).Reverse().ToList();
            if (!insideCave(getCenter(list)))
            {
                foreach (var v in list)
                {
                    var index = indexOf(vertices, v);
                    if (index < 0)
                    {
                        index = vertices.Count;
                        vertices.Add(v);
                    }
                    triangles.Add(index);
                }
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Delete terrain")]
#endif
    private void DeleteTerrain()
    {
        _ready = false;
#if UNITY_EDITOR
    var meshFilter = GetComponent<MeshFilter>();
    var mesh = meshFilter.sharedMesh;
    if (mesh == null)
    {
        meshFilter.mesh = new Mesh();
        mesh = meshFilter.sharedMesh;
    }
    mesh.triangles = new int[0];
    mesh.vertices = new Vector3[0];
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    meshFilter.mesh = mesh;


    editorPolygons = new List<PSPolygon>();
    editorEdges = new List<PSEdge>();
    editorPoints = new List<Vector2>();
#endif
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2 offset = (Vector2)transform.position * 0;

            editorPolygons.ForEach(p => {
                Gizmos.color = p.IsHole ? Color.red : Color.white;
                DrawPolygon(p, offset);
            });

            Gizmos.color = Color.cyan;
            editorEdges.ForEach(e => DrawEdge(e, offset));

            Gizmos.color = Color.white;
            DrawDiamonds(editorPoints, offset);
        }
    }

    private void DrawPolygon(PSPolygon polygon, Vector2 offset)
    {
        for (int i = 0; i < polygon.points.Length; i++)
        {
            if (i + 1 == polygon.points.Length)
            {
                Gizmos.DrawLine(polygon.points[i] + offset, polygon.points[0] + offset);
            }
            else
            {
                Gizmos.DrawLine(polygon.points[i] + offset, polygon.points[i + 1] + offset);
            }
        }
    }

    private void DrawEdge(PSEdge edge, Vector2 offset)
    {
        var v0 = edge.v0 + offset;
        var v1 = edge.v1 + offset;
        var arrow = 0.9f * v1 + 0.1f * v0;
        var perpendicular = 0.1f * Vector2.Perpendicular(v1 - v0);
        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(arrow + perpendicular, v1);
        Gizmos.DrawLine(arrow - perpendicular, v1);
    }

    private void DrawDiamonds(IEnumerable<Vector2> points, Vector2 offset)
    {
        float diamondSize = 0.1f;
        List<Vector2> diamond = new List<Vector2>();
        diamond.Add(new Vector2(-diamondSize / transform.lossyScale.x, 0f));
        diamond.Add(new Vector2(0f, diamondSize / transform.lossyScale.y));
        diamond.Add(new Vector2(diamondSize / transform.lossyScale.x, 0f));
        diamond.Add(new Vector2(0f, -diamondSize / transform.lossyScale.y));
        foreach (Vector2 point in points)
        {
            for (int i = 0; i < diamond.Count; i++)
            {
                Vector2 diamondOffset = point + offset;
                if (i + 1 == diamond.Count)
                {
                    Gizmos.DrawLine(diamond[i] + diamondOffset, diamond[0] + diamondOffset);
                }
                else
                {
                    Gizmos.DrawLine(diamond[i] + diamondOffset, diamond[i + 1] + diamondOffset);
                }
            }
        }
    }
#endif
}
