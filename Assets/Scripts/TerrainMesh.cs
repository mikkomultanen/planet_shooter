using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
#if UNITY_EDITOR
using UnityEditor;
#endif

class Noise
{
    private float aMin;
    private float aMax;
    private float[] a;
    private int[] f;
    private float[] p;

    public Noise(float aMin, float aMax, int minF, int maxF, int n)
    {
        this.aMin = aMin;
        this.aMax = aMax;
        this.a = new float[n];
        this.f = new int[n];
        this.p = new float[n];
        float sumA = 0;
        for (int i = 0; i < n; i++)
        {
            this.a[i] = Random.Range(n - i - 0.9f, n - i);
            sumA += this.a[i];
            this.f[i] = Mathf.RoundToInt(Random.Range((i + 1) * minF, (i + 1) * maxF));
            this.p[i] = Random.Range(0, 2 * Mathf.PI);
        }
        float scale = 1.0f / sumA;
        for (int i = 0; i < n; i++)
        {
            this.a[i] *= scale;
        }
    }

    public float value(float angle)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * Mathf.Sin(f[i] * angle + p[i]);
        }
        return Mathf.Lerp(aMin, aMax, 0.5f * sum + 0.5f);
    }
}
class Cave
{
    public float r = 1;
    private Noise wave;
    private Noise thickness;
    public Cave(float r, float aMin, float aMax, float tMin, float tMax)
    {
        this.r = r;
        this.wave = new Noise(aMin, aMax, 1, 5, 5);
        this.thickness = new Noise(tMin, tMax, 11, 17, 5);
    }

    public Vector2 center(Vector2 direction)
    {
        var angle = Mathf.Atan2(direction.x, direction.y);
        return direction.normalized * centerMagnitude(angle);
    }

    public Vector3 caveNormal(float angle, float magnitude)
    {
        var floor = floorMagnitude(angle);
        var ceiling = ceilingMagnitude(angle);
        if (magnitude < floor)
        {
            // Under
            var position = 1 - Mathf.Clamp01(floor - magnitude); // from 1 floor to 0 under the floor
            return Quaternion.Euler(90 * position, 0, 0) * Vector3.back * position;
        }
        else if (magnitude <= ceiling)
        {
            // Inside
            var position = (magnitude - floor) / (ceiling - floor); // from 0 floor to 1 ceiling
            return Quaternion.Euler(-180 * (position - 0.5F), 0, 0) * Vector3.back;
        }
        else
        {
            var position = 1 - Mathf.Clamp01(magnitude - ceiling); // from 1 ceiling to 0 over the ceiling
            return Quaternion.Euler(-90 * position, 0, 0) * Vector3.back * position;
        }
    }

    public float ceilingMagnitude(float angle)
    {
        return waveValue(angle) + thicknessValue(angle) / 2;
    }

    public float centerMagnitude(float angle)
    {
        return waveValue(angle);
    }

    public float floorMagnitude(float angle)
    {
        return waveValue(angle) - thicknessValue(angle) / 2;
    }

    public float waveValue(float angle)
    {
        return wave.value(angle) + r;
    }

    public float thicknessValue(float angle)
    {
        return thickness.value(angle);
    }
}

[RequireComponent(typeof(MeshFilter))]
public class TerrainMesh : MonoBehaviour
{
    public ParticleSystem terrainParticleTemplate;
    public float outerRadius = 1;
    public float innerRadius = 0.1f;
    private float textureScaleU = 6;
    private float textureScaleV = 1;
    private float threshold = 1f;
    private Material material;
    private Texture2D mainTex;
    private Vector2 mainTexOffset;
    private Vector2 mainTexScale;
    private Texture2D overlayTex;
    private Vector2 overlayTexOffset;
    private Vector2 overlayTexScale;
    private Color tintColor;
    private float brightness;

    public List<GameObject> fragments = new List<GameObject>();

    private List<Cave> caves = new List<Cave>();
#if UNITY_EDITOR
    private List<PSPolygon> editorPolygons = new List<PSPolygon>();
    private List<PSEdge> editorEdges = new List<PSEdge>();
    private List<Vector2> editorPoints = new List<Vector2>();
#endif

    private void Awake()
    {
        if (caves.Count == 0)
        {
            GenerateTerrain();
        }
    }

    public static Vector2 RandomPointOnUnitCircle()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float x = Mathf.Sin(angle);
        float y = Mathf.Cos(angle);
        return new Vector2(x, y);
    }

    public Vector2 randomCaveCenter()
    {
        return caves[Random.Range(0, caves.Count)].center(RandomPointOnUnitCircle());
    }

    public Vector2[] startPositions(int playerCount)
    {
        if (caves.Count == 0)
        {
            GenerateTerrain();
        }
        float step = Mathf.PI * 2 / playerCount;
        float phase = Random.Range(0f, Mathf.PI * 2);
        float angle, magnitude;
        Vector2[] result = new Vector2[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            angle = i * step + phase;
            magnitude = caves.Select(c => c.centerMagnitude(angle)).Max();
            result[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * magnitude;
        }
        return result;
    }

    public float upperCaveCenterMagnitude(Vector2 position)
    {
        var angle = Mathf.Atan2(position.x, position.y);
        return caves.Select(c => c.centerMagnitude(angle)).Max();
    }

    private void GenerateCaves()
    {
        float r = (innerRadius + outerRadius) / 2;
        float t = (outerRadius - innerRadius);
        caves.Clear();
        int n = Random.Range(2, 4);
        for (int i = 0; i < n; i++)
        {
            caves.Add(new Cave(r - t / 4 * i / (n - 1), -t / 4, t / 4 + t / 8, t / 8, t / 4));
        }
    }

    private void InitMaterial()
    {
        if (material == null)
        {
            material = GetComponent<MeshRenderer>().sharedMaterial;
            mainTex = material.GetTexture("_MainTex") as Texture2D;
            mainTexOffset = material.GetTextureOffset("_MainTex");
            mainTexScale = material.GetTextureScale("_MainTex");
            overlayTex = material.GetTexture("_OverlayTex") as Texture2D;
            overlayTexOffset = material.GetTextureOffset("_OverlayTex");
            overlayTexScale = material.GetTextureScale("_OverlayTex");
            tintColor = material.GetColor("_Color");
            brightness = material.GetFloat("_Brightness");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate terrain")]
#endif
    private void GenerateTerrain()
    {
        GenerateCaves();

        var points = new List<Vector2>();

        Vector2 v00, v10, v01, v11;
        float f00, f10, f01, f11;
        bool b00, b10, b01, b11;
        var r = outerRadius + 1;
        for (float x = -r; x < r; x++)
        {
            for (float y = -r; y < r; y++)
            {
                v00 = new Vector2(x, y);
                v10 = new Vector2(x + 1, y);
                v01 = new Vector2(x, y + 1);
                v11 = new Vector2(x + 1, y + 1);
                f00 = caveFieldValue(v00);
                f10 = caveFieldValue(v10);
                f01 = caveFieldValue(v01);
                f11 = caveFieldValue(v11);
                b00 = f00 > threshold;
                b10 = f10 > threshold;
                b01 = f01 > threshold;
                b11 = f11 > threshold;

                if (b00 != b10)
                {
                    addBorderPoint(points, findBorder(v00, f00, v10, f10));
                }
                if (b00 != b01)
                {
                    addBorderPoint(points, findBorder(v00, f00, v01, f01));
                }
                if (b11 != b10)
                {
                    addBorderPoint(points, findBorder(v11, f11, v10, f10));
                }
                if (b11 != b01)
                {
                    addBorderPoint(points, findBorder(v11, f11, v01, f01));
                }
            }
        }

        var pointSets = new List<List<Vector2>>();
        pointSets.Add(points.Where(p => p.x >= 0 && p.y >= 0).ToList());
        pointSets.Add(points.Where(p => p.x >= 0 && p.y <= 0).ToList());
        pointSets.Add(points.Where(p => p.x <= 0 && p.y >= 0).ToList());
        pointSets.Add(points.Where(p => p.x <= 0 && p.y <= 0).ToList());

        var polygons = pointSets.SelectMany(s => GenerateMeshAndPolygons(s)).ToList();

#if UNITY_EDITOR
        //editorPoints = points;
        editorPolygons = polygons;
        editorEdges = editorPolygons.SelectMany(getFloorEdges).ToList();
#endif

        GenerateFragments(polygons);
    }

    private Vector2 findBorder(Vector2 v0, float f0, Vector2 v1, float f1)
    {
        Vector2 minV, maxV, middleV;
        float middleF;
        if (f0 < f1)
        {
            minV = v0;
            maxV = v1;
        }
        else
        {
            minV = v1;
            maxV = v0;
        }
        int iterations = 0;
        do
        {
            middleV = (minV + maxV) * 0.5f;
            middleF = caveFieldValue(middleV);
            if (middleF < threshold)
            {
                minV = middleV;
            }
            else
            {
                maxV = middleV;
            }
            iterations++;
        } while ((minV - maxV).sqrMagnitude > 0.0001f && iterations < 10);
        return (minV + maxV) * 0.5f;
    }

    public static Contour createContour(IEnumerable<Vector2> points)
    {
        return new Contour(points.Select(toVertex));
    }

    private static void addBorderPoint(List<Vector2> coords, Vector2 coord)
    {
        var oldIndex = indexOf(coords, coord);
        if (oldIndex < 0)
        {
            coords.Add(coord);
        }
        else if (coord.x == 0 || coord.y == 0)
        {
            coords[oldIndex] = coord;
        }
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

    public static Vector2 getCenter(List<Vector2> coords)
    {
        return coords.Aggregate(Vector2.zero, (center, next) => center + next) / coords.Count;
    }

    public static Vertex toVertex(Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }
    public static Vector2 toVector2(Vertex vertex)
    {
        return new Vector2((float)vertex.X, (float)vertex.Y);
    }

    private bool shouldAdd(List<Vector2> triangle)
    {
        return !insideCave(getCenter(triangle));
    }

    private bool insideCave(Vector2 coord)
    {
        return caveFieldValue(coord) > threshold;
    }

    private float caveFieldValue(Vector2 coord)
    {
        var magnitude = coord.magnitude;
        var innerValue = stepValue(magnitude - innerRadius);
        var outerValue = stepValue(outerRadius - magnitude);
        return caves.Select(c => caveFieldValue(c, coord)).Sum() + innerValue + outerValue;
    }

    private float caveFieldValue(Cave cave, Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        var d = Mathf.Abs(magnitude - cave.centerMagnitude(angle));
        return stepValue(d - cave.thicknessValue(angle) / 2);
    }

    private static float STEP_V = 5;
    private static float stepValue(float x)
    {
        return -Mathf.Sin(Mathf.Clamp(x / STEP_V, -1f, 1f) * Mathf.PI / 2) + 1;
    }

    private List<PSPolygon> GenerateMeshAndPolygons(List<Vector2> points)
    {
        var imesh = new GenericMesher().Triangulate(points.Select(toVertex).ToList());
        var coords = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var triangle in imesh.Triangles)
        {
            var list = triangle.vertices.Select(toVector2).Reverse().ToList();
            if (!insideCave(getCenter(list)))
            {
                foreach (var v in list)
                {
                    var index = indexOf(coords, v);
                    if (index < 0)
                    {
                        index = coords.Count;
                        coords.Add(v);
                    }
                    triangles.Add(index);
                }
            }
        }
        /*
                var meshFilter = GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    meshFilter.mesh = new Mesh();
                    mesh = meshFilter.sharedMesh;
                }
                mesh.vertices = coords.Select(p => new Vector3(p.x, p.y, 0)).ToArray();
                mesh.uv = coords.Select(getUV).ToArray();
                mesh.uv2 = coords.Select(getUV2).ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                meshFilter.mesh = mesh;
         */
        return GeneratePolygons(coords, triangles);
    }
    private List<PSPolygon> GeneratePolygons(List<Vector2> vertices, List<int> triangles)
    {
        // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
        Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
        for (int i = 0; i < triangles.Count; i += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                int vert1 = triangles[i + e];
                int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                if (edges.ContainsKey(edge))
                {
                    edges.Remove(edge);
                }
                else
                {
                    edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                }
            }
        }

        // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
        HashSet<int> validVertices = new HashSet<int>();
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> edge in edges.Values)
        {
            if (lookup.ContainsKey(edge.Key) == false)
            {
                validVertices.Add(edge.Key);
                lookup.Add(edge.Key, edge.Value);
            }
        }

        var polygons = new List<PSPolygon>();

        // Loop through edge vertices in order
        int startVert = 0;
        int nextVert = startVert;
        List<int> colliderPath = new List<int>();
        while (true)
        {

            // Add vertex to collider path
            colliderPath.Add(nextVert);
            var removed = validVertices.Remove(nextVert);
            if (!removed)
            {
                // Edges share a vertex
                colliderPath.Clear();

                // Go to next shape if one exists
                if (validVertices.Count > 0)
                {
                    // Set starting and next vertices
                    startVert = validVertices.First();
                    nextVert = startVert;

                    // Continue to next loop
                    continue;
                }

                // No more verts
                break;
            }

            // Get next vertex
            nextVert = lookup[nextVert];

            // Shape complete
            if (nextVert == startVert)
            {
                var onAxisBorder = colliderPath.Any(index =>
                {
                    var v = vertices[index];
                    return v.x == 0 || v.y == 0;
                });
                if (onAxisBorder || colliderPath.Count > 5)
                {
                    var polygon = new PSPolygon(colliderPath.Select(index => vertices[index]));
                    if (onAxisBorder || polygon.Area > 20)
                    {
                        polygons.Add(polygon);
                    }
                }
                colliderPath.Clear();

                // Go to next shape if one exists
                if (validVertices.Count > 0)
                {
                    // Set starting and next vertices
                    startVert = validVertices.First();
                    nextVert = startVert;

                    // Continue to next loop
                    continue;
                }

                // No more verts
                break;
            }
        }

        return polygons;
    }

    private static List<PSEdge> getFloorEdges(PSPolygon polygon)
    {
        var floorEdges = new List<PSEdge>();
        Vector2 previous = polygon.points.Last();
        bool previousIsFloor = isFloorPoint(previous, polygon);
        bool currentIsFloor;
        foreach (var p in polygon.points)
        {
            currentIsFloor = isFloorPoint(p, polygon);
            if ((currentIsFloor || previousIsFloor) && Vector3.Cross(previous, p).sqrMagnitude > 0)
            {
                floorEdges.Add(new PSEdge(previous, p));
            }
            previous = p;
            previousIsFloor = currentIsFloor;
        }
        return floorEdges;
    }

    private static bool isFloorPoint(Vector2 point, PSPolygon polygon)
    {
        return polygon.PointInPolygon(point + (point.normalized * -0.005f));
    }

    private Vector3 caveNormal(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        var normal = Vector3.zero;
        foreach (Cave cave in caves)
        {
            normal += cave.caveNormal(angle, magnitude);
        }
        if (normal.magnitude < 0.01)
        {
            return Vector3.back;
        }
        else
        {
            return Quaternion.FromToRotation(Vector3.up, coord) * normal.normalized;
        }
    }

    public Vector2 getUV(Vector2 coord, bool doNotWrap)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        if (doNotWrap && angle > 0)
        {
            angle = angle - 2 * Mathf.PI;
        }
        var magnitude = coord.magnitude;
        float u = angle / (2 * Mathf.PI) * textureScaleU;
        float v = Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV;
        return new Vector2(u, v);
    }

    public Vector2 getUV2(Vector2 coord, bool doNotWrap, List<PSEdge> floorEdges)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        if (doNotWrap && angle > 0)
        {
            angle = angle - 2 * Mathf.PI;
        }
        var magnitude = coord.magnitude;
        float u = angle / (2 * Mathf.PI) * textureScaleU * 10;
        var direction = coord.normalized;
        var floorMagnitude = floorEdges.Select(e => e.IntersectMagnitude(direction)).FirstOrDefault(m => m > 0f);
        var d = outerRadius - innerRadius;
        var v = Mathf.Clamp(floorMagnitude - magnitude, 0, d) / d * textureScaleV * 10;
        return new Vector2(u, v);
    }

    public Color32 getColor(Vector2 coord, bool doNotWrap, List<PSEdge> floorEdges)
    {
        var uv = getUV(coord, doNotWrap);
        uv = new Vector2(uv.x * mainTexScale.x, uv.y * mainTexScale.y) + mainTexOffset;
        var color = mainTex.GetPixelBilinear(uv.x, uv.y);
        color = color * tintColor * brightness; 
        color.a = 1f;
        uv = getUV2(coord, doNotWrap, floorEdges);
        uv = new Vector2(uv.x * overlayTexScale.x, uv.y * overlayTexScale.y) + overlayTexOffset;
        var overlayColor = overlayTex.GetPixelBilinear(uv.x, uv.y);
        var a = overlayColor.a;
        overlayColor.a = 1f;
        return Color.Lerp(color, overlayColor, a);
    }

#if UNITY_EDITOR
    [ContextMenu("Delete fragments")]
#endif
    private void DeleteFragments()
    {
        foreach (GameObject frag in fragments)
        {
#if UNITY_EDITOR
            DestroyImmediate(frag);
#else
				Destroy (frag);
#endif
        }
        fragments.Clear();
    }
    private void GenerateFragments(List<PSPolygon> polygons)
    {
        DeleteFragments();
        InitMaterial();
        fragments.AddRange(polygons.Select(p => GenerateFragment(p, material)));
        Resources.UnloadUnusedAssets();
    }

    private GameObject GenerateFragment(PSPolygon polygon, Material mat)
    {
        GameObject piece = new GameObject(gameObject.name + " piece");
        piece.transform.position = gameObject.transform.position;
        piece.transform.rotation = gameObject.transform.rotation;
        piece.transform.localScale = gameObject.transform.localScale;

        MeshFilter meshFilter = (MeshFilter)piece.AddComponent(typeof(MeshFilter));
        piece.AddComponent(typeof(MeshRenderer));

        Mesh uMesh = piece.GetComponent<MeshFilter>().sharedMesh;
        if (uMesh == null)
        {
            meshFilter.mesh = new Mesh();
            uMesh = meshFilter.sharedMesh;
        }

        var poly = new Polygon();
        poly.Add(TerrainMesh.createContour(polygon.points));
        var quality = new QualityOptions();
        quality.MinimumAngle = 36;
        quality.MaximumAngle = 91;
        var imesh = poly.Triangulate(quality);
        var meshVectices = imesh.Triangles.SelectMany(t => t.vertices.Select(TerrainMesh.toVector2).Reverse());

        var vertices = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var v in meshVectices)
        {
            var index = TerrainMesh.indexOf(vertices, v);
            if (index < 0)
            {
                index = vertices.Count;
                vertices.Add(v);
            }
            triangles.Add(index);
        }

        var polygonBoundsCenter = polygon.Bounds.center;
        var doNotWrapUV = polygonBoundsCenter.x < 0 && polygonBoundsCenter.y < 0;
        var floorEdges = getFloorEdges(polygon);

        uMesh.vertices = vertices.Select(p => new Vector3(p.x, p.y, 0)).ToArray();
        uMesh.uv = vertices.Select(v => getUV(v, doNotWrapUV)).ToArray();
        uMesh.uv2 = vertices.Select(v => getUV2(v, doNotWrapUV, floorEdges)).ToArray();
        uMesh.triangles = triangles.ToArray();
        uMesh.RecalculateNormals();
        uMesh.RecalculateBounds();

        piece.GetComponent<MeshRenderer>().sharedMaterial = mat;
        meshFilter.mesh = uMesh;

        PolygonCollider2D collider = piece.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, polygon.points);

        var terrainPiece = piece.AddComponent<TerrainPiece>();
        terrainPiece.terrainMesh = this;
        terrainPiece.doNotWrapUV = doNotWrapUV;
        terrainPiece.floorEdges = floorEdges;

        piece.layer = gameObject.layer;

        return piece;
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
