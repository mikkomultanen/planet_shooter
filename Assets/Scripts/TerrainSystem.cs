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
	public float size = 256;
    public float tiling = 6;
    [Range(1,4)]
    public int iterations;
    [Range(0,1)]
    public float threshold;
    [Range(0,1)]
    public float thresholdAmplitude;
    [Range(0,1)]
    public float innerRadius;
    [Range(0,1)]
    public float outerRadius;
    public float terrainSeed;

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
        fastNoise.SetFrequency(1);
        Debug.Log("GenerateTerrain start" + fastNoise.GetSimplex(2, 2.5f));
        DeleteTerrain();
        int r = Mathf.RoundToInt(size / 2 + 1);

        float scale = tiling / size;
        var allPoints = new ConcurrentBag<Vector2>();
        Parallel.For(-r, r, x => {
            Vector2 v00, v10, v01, v11;
            bool b00, b10, b01, b11;
            for (int y = -r; y < r; y++)
            {
                v00 = new Vector2(x, y) * scale;
                v10 = new Vector2(x + 1, y) * scale;
                v01 = new Vector2(x, y + 1) * scale;
                v11 = new Vector2(x + 1, y + 1) * scale;
                b00 = insideCave(v00);
                b10 = insideCave(v10);
                b01 = insideCave(v01);
                b11 = insideCave(v11);

                if (b00 != b10)
                {
                    allPoints.Add(findBorder(v00, b00, v10, b10) / scale);
                }
                if (b00 != b01)
                {
                    allPoints.Add(findBorder(v00, b00, v01, b01) / scale);
                }
                if (b11 != b10)
                {
                    allPoints.Add(findBorder(v11, b11, v10, b10) / scale);
                }
                if (b11 != b01)
                {
                    allPoints.Add(findBorder(v11, b11, v01, b01) / scale);
                }
            }
        });

        var points = allPoints.Distinct(Vector2EqualComparer.Instance).ToList();

#if UNITY_EDITOR
        editorPoints = points;
        //editorPolygons = polygons;
        //editorEdges = editorPolygons.SelectMany(getFloorEdges).ToList();
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
            Vector3 coord = position * s;
            coord.z = terrainSeed;
            float n = Mathf.Abs(fastNoise.GetSimplex(coord.x, coord.y, coord.z));
            n = 1 - n;
            //n *= n;
            //n *= n;
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
        float d = coord.magnitude / tiling * 2;
        float n = fractalNoise(coord);
        float a = smoothstep(innerRadius, innerRadius + 0.1f, d) * (1 - smoothstep(outerRadius - 0.1f, outerRadius, d));
        Vector3 vCoord = coord;
        vCoord.z = terrainSeed + 1;
        float v = fastNoise.GetSimplex(vCoord.x, vCoord.y, vCoord.z);
        return threshold + thresholdAmplitude * v <= n * a;
    }

#if UNITY_EDITOR
    [ContextMenu("Delete terrain")]
#endif
    private void DeleteTerrain()
    {
        _ready = false;
#if UNITY_EDITOR
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

            Gizmos.color = Color.yellow;
            editorPolygons.ForEach(p => DrawPolygon(p, offset));

            Gizmos.color = Color.cyan;
            editorEdges.ForEach(e => DrawEdge(e, offset));

            Gizmos.color = Color.red;
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
        Gizmos.DrawLine(edge.v0 + offset, edge.v1 + offset);
    }

    private void DrawDiamonds(IEnumerable<Vector2> points, Vector2 offset)
    {
        float diamondSize = 0.2f;
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
