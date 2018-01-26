using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;

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

    public Vector2 ceiling(Vector2 direction)
    {
        var angle = Mathf.Atan2(direction.x, direction.y);
        return direction.normalized * ceilingMagnitude(angle);
    }

    public Vector2 floor(Vector2 direction)
    {
        var angle = Mathf.Atan2(direction.x, direction.y);
        return direction.normalized * floorMagnitude(angle);
    }

    public bool inside(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        return magnitude - 0.005f > floorMagnitude(angle) && magnitude + 0.005f < ceilingMagnitude(angle);
    }

    public bool isFloor(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        return Mathf.Abs(magnitude - floorMagnitude(angle)) < 0.1f;
    }

    public bool isCeiling(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        return Mathf.Abs(magnitude - ceilingMagnitude(angle)) < 0.1f;
    }

    public float ceilingMagnitude(float angle)
    {
        return waveValue(angle) + thicknessValue(angle) / 2;
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

class Shafts
{
    private float radius;
    private float w;
    private Noise thicknessX = new Noise(1, 2, 5, 23, 3);
    private Noise thicknessY = new Noise(1, 2, 5, 23, 3);

    public Shafts(float radius)
    {
        this.radius = radius;
        w = 1.0f / radius * Mathf.PI;
    }
    public List<Vector2> coords(int steps)
    {
        var result = new List<Vector2>();
        var d = radius / steps;
        var dir1 = new Vector2(1, 0);
        var dir2 = new Vector2(0, 1);
        var dir3 = new Vector2(-1, 0);
        var dir4 = new Vector2(0, -1);
        float p;
        float angle;
        for (int i = 1; i <= steps; i++)
        {
            p = i * d;
            angle = p * w;
            result.Add(dir1 * p + dir2 * thicknessX.value(angle));
            result.Add(dir1 * p + dir4 * thicknessX.value(angle));
            result.Add(dir3 * p + dir2 * thicknessX.value(-angle));
            result.Add(dir3 * p + dir4 * thicknessX.value(-angle));
            result.Add(dir2 * p + dir1 * thicknessY.value(angle));
            result.Add(dir2 * p + dir3 * thicknessY.value(angle));
            result.Add(dir4 * p + dir1 * thicknessY.value(-angle));
            result.Add(dir4 * p + dir3 * thicknessY.value(-angle));
        }
        return result;
    }
    public bool inside(Vector2 coord)
    {
        return Mathf.Abs(coord.x) < thicknessY.value(coord.y * w) || Mathf.Abs(coord.y) < thicknessX.value(coord.x * w);
    }
}

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

[RequireComponent(typeof(MeshFilter))]
public class TerrainMesh : MonoBehaviour
{

    public float outerRadius = 1;
    public float innerRadius = 0.1f;
    public int steps = 128;
    public float textureScaleU = 6;
    public float textureScaleV = 1;

    private List<Cave> caves = new List<Cave>();
    private Shafts shafts;
    private List<Vector2> points = new List<Vector2>();
    private List<Vector2> additionalPoints = new List<Vector2>();
    private List<PSPolygon> polygons = new List<PSPolygon>();

    private void Awake()
    {
#if UNITY_EDITOR
#else
        GenerateTerrain();
#endif
    }

    private void GenerateCaves()
    {
        shafts = new Shafts(outerRadius);
        float r = (innerRadius + outerRadius) / 2;
        float t = (outerRadius - innerRadius);
        caves.Clear();
        int n = Random.Range(2, 4);
        for (int i = 0; i < n; i++)
        {
            caves.Add(new Cave(r - t / 4 * i / (n - 1), -t / 4, t / 4 + t / 8, t / 8, t / 4));
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate terrain")]
#endif
    private void GenerateTerrain()
    {
        GenerateCaves();
        points.Clear();
        additionalPoints.Clear();

        var direction = new Vector2(1, 0);
        var innerStepsMod = Mathf.CeilToInt(outerRadius / innerRadius);
        for (int i = 0; i < steps; i++)
        {
            if (i % innerStepsMod == 0)
            {
                points.Add(direction * innerRadius);
            }
            var angle = Mathf.Atan2(direction.x, direction.y);
            foreach (Cave cave in caves)
            {
                var ceiling = cave.ceilingMagnitude(angle);
                var floor = cave.floorMagnitude(angle);
                additionalPoints.Add(direction * (ceiling + 1));
                points.Add(direction * ceiling);
                points.Add(direction * floor);
                additionalPoints.Add(direction * (floor - 1));
            }
            points.Add(direction * outerRadius);
            direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
        var shaftSteps = Mathf.RoundToInt(steps / (2 * Mathf.PI));
        points.AddRange(shafts.coords(shaftSteps));
        points = points.Where(p => shouldAdd(p)).ToList();
        additionalPoints = additionalPoints.Where(p => shouldAdd(p)).ToList();

        Rect rect = new Rect(-outerRadius, -outerRadius, 2 * outerRadius, 2 * outerRadius);
        Voronoi voronoi = new Delaunay.Voronoi(points, null, rect);
        var coords = voronoi.SiteCoords();
        var triangles = new List<int>();
        foreach (Triangle triangle in voronoi.Triangles())
        {
            if (shouldAdd(triangle))
            {
                foreach (Site site in triangle.sites)
                {
                    triangles.Add(indexOf(coords, site.Coord));
                }
            }
        }
        GeneratePolygons(coords, triangles);

        points = polygons.Aggregate(new List<Vector2>(), (list, polygon) =>
        {
            list.AddRange(polygon.points);
            return list;
        });
        additionalPoints = additionalPoints.Where(point => polygons.Any(p => p.PointInPolygon(point))).ToList();

        GenerateMesh();
        GenerateColliders();
    }

    private int indexOf(List<Vector2> coords, Vector2 coord)
    {
        for (int i = 0; i < coords.Count; i++)
        {
            if (Site.CloseEnough(coords[i], coord))
            {
                return i;
            }
        }
        return -1;
    }

    private Vector2 getCenter(Triangle triangle)
    {
        return triangle.sites.Aggregate(new Vector2(0, 0), (center, next) => center + next.Coord) / 3;
    }

    private bool shouldAdd(Triangle triangle)
    {
        return shouldAdd(getCenter(triangle));
    }

    private bool shouldAdd(Vector2 coord)
    {
        var magnitude = coord.magnitude;
        var insideTerrain = magnitude + 0.005 > innerRadius && magnitude - 0.005f < outerRadius;
        var insideCave = this.insideCave(coord);
        var insideShaft = shafts.inside(coord);
        return insideTerrain && !insideCave && !insideShaft;
    }

    private bool insideCave(Vector2 coord)
    {
        foreach (Cave cave in caves)
        {
            if (cave.inside(coord))
            {
                return true;
            }
        }
        return false;
    }

    private void GeneratePolygons(List<Vector2> vertices, List<int> triangles)
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

        polygons.Clear();

        // Loop through edge vertices in order
        int startVert = 0;
        int nextVert = startVert;
        List<Vector2> colliderPath = new List<Vector2>();
        while (true)
        {

            // Add vertex to collider path
            colliderPath.Add(vertices[nextVert]);
            var removed = validVertices.Remove(nextVert);
            if (!removed)
            {
                // Edges share a vertex
                colliderPath.Clear();
                Debug.Log("ColliderPath invalid validVertices " + validVertices.Count);

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

                if (colliderPath.Count > 5)
                {
                    var polygon = new PSPolygon(colliderPath);
                    if (polygon.Area > 20)
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
    }

    private void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        mesh.Clear();

        var allVertices = new List<Vector2>();
        var allTriangles = new List<int>();
        int currentIndex = 0;
        List<Vector2> points = new List<Vector2>();
        foreach (PSPolygon polygon in polygons)
        {
            points.Clear();
            Rect rect = polygon.Bounds;
            points.AddRange(polygon.points);
            points.AddRange(additionalPoints.Where(polygon.PointInPolygon));
            Voronoi voronoi = new Delaunay.Voronoi(points, null, rect);
            var coords = voronoi.SiteCoords();
            var triangles = new List<int>();
            foreach (Triangle triangle in voronoi.Triangles())
            {
                if (polygon.PointInPolygon(getCenter(triangle)))
                {
                    foreach (Site site in triangle.sites)
                    {
                        triangles.Add(currentIndex + indexOf(coords, site.Coord));
                    }
                }
            }
            allVertices.AddRange(coords);
            allTriangles.AddRange(triangles);
            currentIndex = allVertices.Count;
        }

        mesh.vertices = allVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = allVertices.Select(getUV).ToArray();
        mesh.triangles = allTriangles.ToArray();
        mesh.RecalculateBounds();
    }

    private Vector3 caveNormal(Vector2 coord)
    {
        foreach (Cave cave in caves)
        {
            if (cave.isFloor(coord))
            {
                return new Vector3(coord.x, coord.y, 0).normalized;
            }
        }
        foreach (Cave cave in caves)
        {
            if (cave.isCeiling(coord))
            {
                return new Vector3(-coord.x, -coord.y, 0).normalized;
            }
        }
        return new Vector3(0, 0, 0);
    }

    private Vector2 getUV(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        return new Vector2(angle / (2 * Mathf.PI) * textureScaleU, Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV);
    }

    private void GenerateColliders()
    {
#if UNITY_EDITOR
        DestroyImmediate(GetComponent<PolygonCollider2D>());
#else
		Destroy(GetComponent<PolygonCollider2D>());
#endif
        // Create empty polygon collider
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.pathCount = 0;

        foreach (PSPolygon polygon in polygons)
        {
            // Add path to polygon collider
            polygonCollider.pathCount++;
            polygonCollider.SetPath(polygonCollider.pathCount - 1, polygon.points);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2 offset = (Vector2)transform.position * 0;
            foreach (PSPolygon polygon in polygons)
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

            Gizmos.color = Color.red;
            DrawDiamonds(points, offset);
            Gizmos.color = Color.green;
            DrawDiamonds(additionalPoints, offset);
        }
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
