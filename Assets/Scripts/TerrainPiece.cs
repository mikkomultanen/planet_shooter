using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using UniRx;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainPiece : MonoBehaviour
{
    private CompositeDisposable disposeBag = new CompositeDisposable();

    private sealed class MeshData
    {
        public Vector3[] vertices;
        public Vector2[] uv;
        public Vector2[] uv2;
        public int[] triangles;
        public Vector2[][] paths;
        public PSPolygon[] cuttedParts;

        public MeshData(Vector3[] vertices, Vector2[] uv, Vector2[] uv2, int[] triangles, Vector2[][] paths, PSPolygon[] cuttedParts)
        {
            this.vertices = vertices;
            this.uv = uv;
            this.uv2 = uv2;
            this.triangles = triangles;
            this.paths = paths;
            this.cuttedParts = cuttedParts;
        }

        public static MeshData from(Mesh mesh, PolygonCollider2D collider)
        {
            var paths = Enumerable.Range(0, collider.pathCount)
                .Select(i => collider.GetPath(i))
                .ToArray();

            return new MeshData(mesh.vertices, mesh.uv, mesh.uv2, mesh.triangles, paths, new PSPolygon[0]);
        }
    }

    private Subject<PSPolygon> clipSubject = new Subject<PSPolygon>();
    private void Start()
    {
        var initialMeshData = MeshData.from(GetComponent<MeshFilter>().sharedMesh, GetComponent<PolygonCollider2D>());
        clipSubject
        .ObserveOn(Scheduler.ThreadPool)
        .Scan(initialMeshData, (data, clipPolygon) => UpdateMeshData(data, clipPolygon))
        .ObserveOnMainThread()
        .BatchFrame(1, FrameCountType.Update)
        .Subscribe(onNext: result =>
        {
            UpdateMesh(result.Last());
            UpdateColliders(result.Last());
            EmitParticles(result.SelectMany(d => d.cuttedParts));
        })
        .AddTo(disposeBag);
    }

    private void OnDestroy()
    {
        disposeBag.Dispose();
        disposeBag = null;
    }

    public TerrainMesh terrainMesh;
    public bool doNotWrapUV;
    public List<PSEdge> floorEdges;
    public void destroyTerrain(Vector2 position, float radius)
    {
        var startTime = Time.realtimeSinceStartup;
        var explosionSteps = Mathf.Max(Mathf.FloorToInt(2 * Mathf.PI * radius), 12);
        var clipPolygon = createCirclePolygon(position, radius, explosionSteps);
        clipSubject.OnNext(clipPolygon);
    }

    private PSPolygon createCirclePolygon(Vector2 position, float radius, int steps)
    {
        var points = new List<Vector2>(steps);
        var direction = new Vector2(1, 0);
        for (int i = 0; i < steps; i++)
        {
            points.Add(direction * radius + position);
            direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
        return new PSPolygon(points);
    }

    private MeshData UpdateMeshData(MeshData oldData, PSPolygon clipPolygon)
    {
        var oldVertices = oldData.vertices;
        var oldTriangles = oldData.triangles;
        var oldPaths = oldData.paths;

        var newVertices = new List<Vector2>(oldVertices.Length);

        var vertexIndex = new Dictionary<int, int>(oldVertices.Length);
        Vector2 v;
        for (int i = 0; i < oldVertices.Length; i++)
        {
            v = oldVertices[i];
            if (clipPolygon.PointInPolygon(v))
            {
                vertexIndex.Add(i, -1);
            }
            else
            {
                vertexIndex.Add(i, newVertices.Count);
                newVertices.Add(v);
            }
        }

        var newTriangles = new List<int>(oldTriangles.Length);
        var clippedTriangles = new List<int[]>();
        List<int> oldTriangle = new List<int>(3);
        for (int i = 0; i < oldTriangles.Length / 3; i++)
        {
            oldTriangle.Clear();
            oldTriangle.Add(oldTriangles[3 * i]);
            oldTriangle.Add(oldTriangles[3 * i + 1]);
            oldTriangle.Add(oldTriangles[3 * i + 2]);
            var newTriangle = oldTriangle.Select(oldIndex => vertexIndex[oldIndex]).ToArray();
            if (newTriangle.Any(newIndex => newIndex > -1))
            {
                if (newTriangle.All(newIndex => newIndex > -1))
                    newTriangles.AddRange(newTriangle);
                else
                    clippedTriangles.Add(oldTriangle.ToArray());
            }
        }

        var newPolygons = clippedTriangles
        .Select(t => t.Select(p => (Vector2)oldVertices[p]))
        .SelectMany(t => PSPolygon.difference(t.ToArray(), clipPolygon.points));

        var mesher = new GenericMesher();
        foreach (var points in newPolygons)
        {
            var poly = new Polygon();
            poly.Add(TerrainMesh.createContour(points));
            var imesh = mesher.Triangulate(poly);
            var meshVertices = imesh.Triangles.SelectMany(t => t.vertices.Select(TerrainMesh.toVector2).Reverse());
            foreach (var vertex in meshVertices)
            {
                var index = TerrainMesh.indexOf(newVertices, vertex);
                if (index < 0)
                {
                    index = newVertices.Count;
                    newVertices.Add(vertex);
                }
                newTriangles.Add(index);
            }
        }

        var newPaths = oldPaths
            .SelectMany(p => PSPolygon.difference(p, clipPolygon.points))
            .Select(p => p.ToArray())
            .ToArray();

        var cuttedParts = oldPaths
            .SelectMany(p => PSPolygon.intersection(p, clipPolygon.points))
            .Select(p => new PSPolygon(p))
            .ToArray();

        var vertices = newVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        var uv = newVertices.Select(c => terrainMesh.getUV(c, doNotWrapUV)).ToArray();
        var uv2 = newVertices.Select(c => terrainMesh.getUV2(c, doNotWrapUV, floorEdges)).ToArray();
        return new MeshData(vertices, uv, uv2, newTriangles.ToArray(), newPaths, cuttedParts);
    }

    private void UpdateMesh(MeshData data)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        mesh.Clear();

        mesh.vertices = data.vertices;
        //mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = data.uv;
        mesh.uv2 = data.uv2;
        mesh.triangles = data.triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void UpdateColliders(MeshData data)
    {
        foreach (var c in gameObject.GetComponents<PolygonCollider2D>())
        {
            Destroy(c);
        }
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.pathCount = 0;

        foreach (var p in data.paths)
        {
            polygonCollider.pathCount++;
            polygonCollider.SetPath(polygonCollider.pathCount - 1, p.ToArray());
        }
    }

    private void EmitParticles(IEnumerable<PSPolygon> shapes)
    {
        if (shapes.Take(1).Count() == 0)
        {
            return;
        }
        var direction = shapes.Aggregate(Vector2.zero, (d, p) => d + p.Bounds.center);
        var ps = Instantiate(terrainMesh.terrainParticleTemplate);
        ps.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
        ps.gameObject.SetActive(true);
        var coords = new List<Vector2>();
        Vector2 coord;
        foreach (var shape in shapes)
        {
            var xMin = shape.Bounds.xMin;
            var xMax = shape.Bounds.xMax;
            var yMin = shape.Bounds.yMin;
            var yMax = shape.Bounds.yMax;
            for (float x = xMin; x < xMax; x += 0.1f)
            {
                for (float y = yMin; y < yMax; y += 0.1f)
                {
                    coord = new Vector2(x, y);
                    if (shape.PointInPolygon(coord))
                    {
                        coords.Add(coord);
                    }
                }
            }
        }
        if (coords.Count > ps.main.maxParticles)
        {
            int everyNth = coords.Count / ps.main.maxParticles + 1;
            coords = coords.Where((p, i) => i % everyNth == 0).ToList();
        }
        int count = Mathf.Min(ps.main.maxParticles, coords.Count);
        ps.Emit(count);
        var particles = new ParticleSystem.Particle[count];
        int activeCount = ps.GetParticles(particles);
        for (int i = 0; i < activeCount; i++)
        {
            particles[i].position = new Vector3(coords[i].x, coords[i].y, transform.position.z);
            particles[i].startColor = terrainMesh.getColor(coords[i], doNotWrapUV, floorEdges);
        }
        ps.SetParticles(particles, activeCount);
        ps.Play();
    }
}
