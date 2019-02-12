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

interface IClipShape
{
    bool ShouldClipTriangle(IEnumerable<Vector2> points);
    PSPolygon ClipPolygon();
}

class CircleClipShape : IClipShape
{
    private Vector2 position;
    private float radius;
    private Rect bounds;

    public CircleClipShape(Vector2 position, float radius)
    {
        this.position = position;
        this.radius = radius;
        var halfSize = new Vector2(radius, radius);
        this.bounds = new Rect(position - halfSize, halfSize * 2);
    }

    public bool ShouldClipTriangle(IEnumerable<Vector2> points)
    {
        if (points.Min(p => p.x) > bounds.xMax) return false;
        if (points.Max(p => p.x) < bounds.xMin) return false;
        if (points.Min(p => p.y) > bounds.yMax) return false;
        if (points.Max(p => p.y) < bounds.yMin) return false;
        var v0 = points.Last();
        foreach (var v1 in points)
        {
            if (PSEdge.PointDistanceToEdge(position, v0, v1) <= radius) return true;
        }
        return false;
    }
    public PSPolygon ClipPolygon()
    {
        var steps = Mathf.Max(Mathf.FloorToInt(2 * Mathf.PI * radius), 12);
        var points = new List<Vector2>(steps);
        var v = new Vector2(radius, 0);
        for (int i = 0; i < steps; i++)
        {
            points.Add(v + position);
            v = Quaternion.Euler(0, 0, -360.0f / steps) * v;
        }
        return new PSPolygon(points);
    }
}

class CapsuleClipShape : IClipShape
{
    private Vector2 start;
    private Vector2 direction;
    private float radius;
    private CircleClipShape startCircle;
    private CircleClipShape endCircle;

    public CapsuleClipShape(Vector2 start, Vector2 direction, float radius)
    {
        this.start = start;
        this.direction = direction;
        this.radius = radius;
        this.startCircle = new CircleClipShape(start, radius);
        this.endCircle = new CircleClipShape(start + direction, radius);
    }

    public bool ShouldClipTriangle(IEnumerable<Vector2> points)
    {
        var v0 = points.Last();
        foreach (var v1 in points)
        {
            if (PSEdge.SegmentsCross(start, direction, v0, v1 - v0)) return true;
        }
        if (startCircle.ShouldClipTriangle(points)) return true;
        if (endCircle.ShouldClipTriangle(points)) return true;
        return false;
    }

    public PSPolygon ClipPolygon()
    {
        var steps = Mathf.Max(Mathf.FloorToInt(2 * Mathf.PI * radius), 8);
        var halfSteps = Mathf.CeilToInt(steps * 0.5f);
        var points = new List<Vector2>(2 * halfSteps + 2);
        var r = direction.normalized * radius;
        var v = new Vector2(-r.y, r.x);
        var angleDelta = 180.0f / halfSteps;
        var end = start + direction;
        for (int i = 0; i <= halfSteps; i++)
        {
            points.Add(v + start);
            v = Quaternion.Euler(0, 0, angleDelta) * v;
        }
        v = new Vector2(r.y, -r.x);
        for (int i = 0; i <= halfSteps; i++)
        {
            points.Add(v + end);
            v = Quaternion.Euler(0, 0, angleDelta) * v;
        }
        return new PSPolygon(points);
    }
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainPiece : MonoBehaviour
{
    public ParticleSystem terrainParticleTemplate;
    public MeshFilter background;

    private CompositeDisposable disposeBag = new CompositeDisposable();

    private sealed class TerrainParticles
    {
        public Vector2[] positions;
        public Vector2[] mainTexUV;
        public Vector2[] overlayTexUV;
        public Color[] colors;
        public Vector2 center;

        public TerrainParticles(Vector2[] positions, Vector2[] mainTexUV, Vector2[] overlayTexUV, Color[] colors, Vector2 center)
        {
            this.positions = positions;
            this.mainTexUV = mainTexUV;
            this.overlayTexUV = overlayTexUV;
            this.colors = colors;
            this.center = center;
        }
    }

    private sealed class MeshData
    {
        public Vector3[] vertices;
        public Color32[] colors32;
        public Vector2[] uv;
        public Vector2[] uv2;
        public int[] triangles;
        public Vector2[][] paths;
        public TerrainParticles[] particles;

        public MeshData(Vector3[] vertices, Color32[] colors32, Vector2[] uv, Vector2[] uv2, int[] triangles, Vector2[][] paths, TerrainParticles[] particles)
        {
            this.vertices = vertices;
            this.colors32 = colors32;
            this.uv = uv;
            this.uv2 = uv2;
            this.triangles = triangles;
            this.paths = paths;
            this.particles = particles;
        }

        public static MeshData from(Mesh mesh, PolygonCollider2D collider)
        {
            var paths = Enumerable.Range(0, collider.pathCount)
                .Select(i => collider.GetPath(i))
                .ToArray();

            return new MeshData(mesh.vertices, mesh.colors32, mesh.uv, mesh.uv2, mesh.triangles, paths, new TerrainParticles[0]);
        }
    }

    private Subject<IClipShape> clipSubject = new Subject<IClipShape>();
    private int maxParticles;
    private Mesh mesh;
    private void Start()
    {
        maxParticles = terrainParticleTemplate.main.maxParticles;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        var initialMeshData = MeshData.from(mesh, GetComponent<PolygonCollider2D>());
        clipSubject
        .ObserveOn(Scheduler.ThreadPool)
        .Scan(initialMeshData, (data, clipPolygon) => UpdateMeshData(data, clipPolygon))
        .ObserveOnMainThread()
        .BatchFrame(1, FrameCountType.Update)
        .Subscribe(onNext: result =>
        {
            UpdateMesh(result.Last());
            UpdateColliders(result.Last());
            EmitParticles(result.SelectMany(d => d.particles));
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
        clipSubject.OnNext(new CircleClipShape(position, radius));
    }

    public void destroyTerrain(Vector2 start, Vector2 direction, float radius)
    {
        clipSubject.OnNext(new CapsuleClipShape(start, direction, radius));
    }

    private MeshData UpdateMeshData(MeshData oldData, IClipShape clipShape)
    {
        var clipPolygon = clipShape.ClipPolygon();
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
                if (newTriangle.All(newIndex => newIndex > -1) && !clipShape.ShouldClipTriangle(newTriangle.Select(newIndex => newVertices[newIndex])))
                    newTriangles.AddRange(newTriangle);
                else
                    clippedTriangles.Add(oldTriangle.ToArray());
            }
        }

        var newPolygons = clippedTriangles
        .Select(t => t.Select(oldIndex => (Vector2)oldVertices[oldIndex]))
        .SelectMany(t => PSClipperHelper.difference(t, clipPolygon.points));

        var mesher = new GenericMesher();
        foreach (var points in newPolygons)
        {
            var poly = new Polygon();
            poly.Add(TerrainMesh.createContour(points));
            var imesh = mesher.Triangulate(poly);
            TerrainMesh.getTriangles(imesh, ref newVertices, ref newTriangles);
        }

        var newPaths = oldPaths
            .SelectMany(p => PSClipperHelper.difference(p, clipPolygon.points))
            .Select(p => p.ToArray())
            .ToArray();

        var particles = oldPaths
            .SelectMany(p => PSClipperHelper.intersection(p, clipPolygon.points))
            .Select(p => GenerateParticles(new PSPolygon(p)))
            .ToArray();

        return new MeshData(
            newVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray(), 
            newVertices.Select(c => (Color32)terrainMesh.terrainTintColor(c, doNotWrapUV)).ToArray(), 
            newVertices.Select(c => terrainMesh.getUV(c, doNotWrapUV)).ToArray(), 
            newVertices.Select(c => terrainMesh.getUV2(c, doNotWrapUV, floorEdges)).ToArray(), 
            newTriangles.ToArray(), 
            newPaths, 
            particles);
    }

    private TerrainParticles GenerateParticles(PSPolygon shape)
    {
        var center = shape.Bounds.center;
        var coords = new List<Vector2>();
        Vector2 coord;
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
        if (coords.Count > maxParticles)
        {
            int everyNth = coords.Count / maxParticles + 1;
            coords = coords.Where((p, i) => i % everyNth == 0).ToList();
        }
        var mainTexUV = coords.Select(c => terrainMesh.getMainTexUV(c, doNotWrapUV));
        var overlayTexUV = coords.Select(c => terrainMesh.getOverlayTexUV(c, doNotWrapUV, floorEdges));
        var colors = coords.Select(c => terrainMesh.terrainTintColor(c, doNotWrapUV));
        return new TerrainParticles(coords.ToArray(), mainTexUV.ToArray(), overlayTexUV.ToArray(), colors.ToArray(), center);
    }

    private void UpdateMesh(MeshData data)
    {
        mesh.Clear();
        mesh.vertices = data.vertices;
        mesh.colors32 = data.colors32;
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

    private void EmitParticles(IEnumerable<TerrainParticles> allParticles)
    {
        foreach (var terrainParticles in allParticles)
        {
            var direction = terrainParticles.center;
            var ps = Instantiate(terrainParticleTemplate);
            ps.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
            ps.gameObject.SetActive(true);
            int count = Mathf.Min(ps.main.maxParticles, terrainParticles.positions.Length);
            ps.Emit(count);
            var particles = new ParticleSystem.Particle[count];
            int activeCount = ps.GetParticles(particles);
            for (int i = 0; i < activeCount; i++)
            {
                particles[i].position = new Vector3(terrainParticles.positions[i].x, terrainParticles.positions[i].y, transform.position.z);
                particles[i].startColor = terrainMesh.getColor(terrainParticles.mainTexUV[i], terrainParticles.overlayTexUV[i], terrainParticles.colors[i]);
            }
            ps.SetParticles(particles, activeCount);
            ps.Play();
        }
    }
}
