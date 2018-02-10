using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;

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

    public Vector2 center(Vector2 direction)
    {
        var angle = Mathf.Atan2(direction.x, direction.y);
        return direction.normalized * centerMagnitude(angle);
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
        var right = Vector2.right;
        var up = Vector2.up;
        var left = Vector2.left;
        var down = Vector2.down;
        float p;
        float angle;
        for (int i = 1; i <= steps; i++)
        {
            p = i * d;
            angle = p * w;
            result.Add(right * p + up * thicknessX.value(angle));
            result.Add(right * p + down * thicknessX.value(angle));
            result.Add(left * p + up * thicknessX.value(-angle));
            result.Add(left * p + down * thicknessX.value(-angle));
            result.Add(up * p + right * thicknessY.value(angle));
            result.Add(up * p + left * thicknessY.value(angle));
            result.Add(down * p + right * thicknessY.value(-angle));
            result.Add(down * p + left * thicknessY.value(-angle));
        }
        return result;
    }
    public bool inside(Vector2 coord)
    {
        return Mathf.Abs(coord.x) < thicknessY.value(coord.y * w) || Mathf.Abs(coord.y) < thicknessX.value(coord.x * w);
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

    public List<GameObject> fragments = new List<GameObject>();

    private List<Cave> caves = new List<Cave>();
    private Shafts shafts;
    private List<Vector2> points = new List<Vector2>();
    private List<Vector2> gridPoints = new List<Vector2>();
    private List<PSPolygon> polygons = new List<PSPolygon>();
    private List<PSPolygon> piecePolygons = new List<PSPolygon>();

    private void Awake()
    {
#if UNITY_EDITOR
#else
        GenerateTerrain();
#endif
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
                points.Add(direction * ceiling);
                points.Add(direction * floor);
            }
            points.Add(direction * outerRadius);
            direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
        var shaftSteps = Mathf.RoundToInt(steps / (2 * Mathf.PI));
        points.AddRange(shafts.coords(shaftSteps));
        points = points.Where(p => shouldAdd(p)).ToList();

        var imesh = new GenericMesher().Triangulate(points.Select(toVertex).ToList());
        var coords = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var triangle in imesh.Triangles)
        {
            var list = triangle.vertices.Select(toVector2).Reverse().ToList();
            if (shouldAdd(list))
            {   
                foreach (var v in list)
                {
                    var index = indexOf(coords, v);
                    if (index < 0) {
                        index = coords.Count;
                        coords.Add(v);
                    }
                    triangles.Add(index);
                }
            }
        }
        GeneratePolygons(coords, triangles);

        points = polygons.Aggregate(new List<Vector2>(), (list, p) =>
        {
            list.AddRange(p.points);
            return list;
        });

        //GenerateGridPoints();

        //GeneratePiecePolygons();
        //GenerateFragments();

        GenerateMesh();
        GenerateColliders();
    }

    private static Contour createContour(IEnumerable<Vector2> points)
    {
        return new Contour(points.Select(toVertex));
    }

    private int indexOf(List<Vector2> coords, Vector2 coord)
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

    private Vector2 getCenter(List<Vector2> triangle)
    {
        return triangle.Aggregate(Vector2.zero, (center, next) => center + next) / 3;
    }

    private static Vertex toVertex(Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }
    private static Vector2 toVector2(Vertex vertex)
    {
        return new Vector2((float)vertex.X, (float)vertex.Y);
    }

    private bool shouldAdd(List<Vector2> triangle)
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
        List<int> colliderPath = new List<int>();
        while (true)
        {

            // Add vertex to collider path
            colliderPath.Add(nextVert);
            var removed = validVertices.Remove(nextVert);
            if (!removed)
            {
                // Edges share a vertex
                var loop = colliderPath.SkipWhile(index => index != nextVert).Skip(1).ToList();
                if (loop.Count > 5)
                {
                    var polygon = new PSPolygon(loop.Select(index => vertices[index]));
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

            // Get next vertex
            nextVert = lookup[nextVert];

            // Shape complete
            if (nextVert == startVert)
            {

                if (colliderPath.Count > 5)
                {
                    var polygon = new PSPolygon(colliderPath.Select(index => vertices[index]));
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

    /*
    private void GenerateGridPoints()
    {
        gridPoints.Clear();

        Rect rect = new Rect(-outerRadius, -outerRadius, 2 * outerRadius, 2 * outerRadius);
        float ratio = Mathf.Sqrt(3) / 2;
        float n = Mathf.Sqrt(20000 / ratio);
        int rows = Mathf.RoundToInt(n);
        float rowHeight = rect.height / rows;
        int columns = Mathf.RoundToInt(n * ratio);
        float columnWidth = rect.width / columns;

        float x0 = rect.width / -2 + rect.center.x + columnWidth / 2;
        float y0 = rect.height / -2 + rect.center.y + rowHeight / 2;

        float x, y, xOffset;
        float randomVariation = 0.4f;
        Vector2 point;
        for (int i = 0; i < rows; i++)
        {
            y = y0 + i * rowHeight;
            xOffset = i % 2 == 0 ? -columnWidth / 4 : columnWidth / 4;
            for (int j = 0; j < columns; j++)
            {
                x = x0 + j * columnWidth + xOffset;
                point = new Vector2(
                    Random.Range(randomVariation * columnWidth / -2 + x, randomVariation * columnWidth / 2 + x),
                    Random.Range(randomVariation * rowHeight / -2 + y, randomVariation * rowHeight / 2 + y)
                );
                if (polygons.Any(p => p.PointInPolygon(point)))
                {
                    gridPoints.Add(point);
                }
            }
        }
    }

    private void GeneratePiecePolygons()
    {
        piecePolygons.Clear();
        piecePolygons.AddRange(polygons.SelectMany(SplitPolygon));
    }

    private IEnumerable<PSPolygon> SplitPolygon(PSPolygon polygon)
    {
        var points = gridPoints.Where(polygon.PointInPolygon).ToList();
        var borderPoints = polygon.points.ToList();
        Voronoi voronoi = new Delaunay.Voronoi(points, null, polygon.Bounds);
        return voronoi.Regions()
            .SelectMany(region => ClipperHelper.clip(borderPoints, region))
            .Select(clippedRegion => new PSPolygon(clippedRegion));
    }

*/
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
        foreach (PSPolygon polygon in polygons)
        {
            var poly = new Polygon();
            poly.Add(createContour(polygon.points));
            var quality = new QualityOptions();
            quality.MinimumAngle = 36;
            quality.MaximumAngle = 91;
            var imesh = poly.Triangulate(quality);
            var coords = new List<Vector2>();
            var triangles = new List<int>();
            foreach (Triangle triangle in imesh.Triangles)
            {
                var list = triangle.vertices.Select(toVector2).Reverse().ToList();
                foreach (var v in list)
                {
                    var index = indexOf(coords, v);
                    if (index < 0) {
                        index = coords.Count;
                        coords.Add(v);
                    }
                    triangles.Add(currentIndex + index);
                }
            }
            allVertices.AddRange(coords);
            allTriangles.AddRange(triangles);
            currentIndex = allVertices.Count;
        }

        mesh.vertices = allVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        //mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = allVertices.Select(getUV).ToArray();
        mesh.uv2 = allVertices.Select(getUV2).ToArray();
        mesh.triangles = allTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
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

    private Vector2 getUV(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        return new Vector2(angle / (2 * Mathf.PI) * textureScaleU, Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV);
    }

    private Vector2 getUV2(Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        var u = angle / (2 * Mathf.PI) * textureScaleU * (outerRadius - innerRadius);
        var v = caves.Aggregate(outerRadius - innerRadius, (d, cave) =>
        {
            var floorMagnitude = cave.floorMagnitude(angle);
            if (magnitude <= floorMagnitude + 0.1)
            {
                return Mathf.Clamp(floorMagnitude - magnitude, 0, d);
            }
            else
            {
                return d;
            }
        }) * textureScaleV;
        return new Vector2(u, v);
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
    /*
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
    private void GenerateFragments()
    {
        DeleteFragments();
        var mat = GetComponent<MeshRenderer>().sharedMaterial;
        foreach (PSPolygon piecePolygon in piecePolygons)
        {
            fragments.Add(GenerateVoronoiPiece(piecePolygon, mat));
        }

        Resources.UnloadUnusedAssets();
    }

    private GameObject GenerateVoronoiPiece(PSPolygon polygon, Material mat)
    {
        GameObject piece = new GameObject(gameObject.name + " piece");
        piece.transform.position = gameObject.transform.position;
        piece.transform.rotation = gameObject.transform.rotation;
        piece.transform.localScale = gameObject.transform.localScale;

        //Create and Add Mesh Components
        MeshFilter meshFilter = (MeshFilter)piece.AddComponent(typeof(MeshFilter));
        piece.AddComponent(typeof(MeshRenderer));

        Mesh uMesh = piece.GetComponent<MeshFilter>().sharedMesh;
        if (uMesh == null)
        {
            meshFilter.mesh = new Mesh();
            uMesh = meshFilter.sharedMesh;
        }

        // TODO replace with better triangulator
        Voronoi voronoi = new Voronoi(polygon.points.ToList(), null, polygon.Bounds);

        var vertices = voronoi.SiteCoords();
        var triangles = new List<int>();
        foreach (Triangle triangle in voronoi.Triangles())
        {
            if (polygon.PointInPolygon(getCenter(triangle)))
            {
                foreach (Site site in triangle.sites)
                {
                    triangles.Add(indexOf(vertices, site.Coord));
                }
            }
        }
        var center = vertices.Aggregate(Vector2.zero, (c, v) => c + v) / vertices.Count;

        uMesh.vertices = vertices.Select(p => new Vector3(p.x - center.x, p.y - center.y, 0)).ToArray();
        uMesh.uv = vertices.Select(getUV).ToArray();
        uMesh.uv2 = vertices.Select(getUV2).ToArray();
        uMesh.triangles = triangles.ToArray();
        uMesh.RecalculateNormals();

        //calculate and assign adjusted trasnsform position
        piece.transform.position += new Vector3(center.x, center.y, 0);


        //set transform properties before fixing the pivot for easier rotation
        //piece.transform.localScale = origScale;
        //piece.transform.localRotation = origRotation;

        uMesh.RecalculateBounds();

        piece.GetComponent<MeshRenderer>().sharedMaterial = mat;

        //assign mesh
        meshFilter.mesh = uMesh;

        //Create and Add Polygon Collider
        PolygonCollider2D collider = piece.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, polygon.points.Select(p => p - center).ToArray());

        //Create and Add Rigidbody
        Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D>();
        rigidbody.mass = polygon.Area * 100;
        rigidbody.bodyType = RigidbodyType2D.Static;

        Explodable fragExp = piece.AddComponent<Explodable>();
        fragExp.shatterType = Explodable.ShatterType.Voronoi;
        fragExp.allowRuntimeFragmentation = true;
        fragExp.extraPoints = 0;
        //fragExp.fragmentLayer = ;
        //fragExp.sortingLayerName = ;
        //fragExp.orderInLayer = ;

        piece.AddComponent<EarthBlock>();
        piece.layer = gameObject.layer;

        return piece;
    }
     */
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2 offset = (Vector2)transform.position * 0;

            Gizmos.color = Color.yellow;
            polygons.ForEach(p => DrawPolygon(p, offset));
            Gizmos.color = Color.cyan;
            piecePolygons.ForEach(p => DrawPolygon(p, offset));

            Gizmos.color = Color.red;
            DrawDiamonds(points, offset);
            Gizmos.color = Color.green;
            DrawDiamonds(gridPoints, offset);
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
