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
    private sealed class MeshData
    {
        public Vector2[] vertices;
        public int[] triangles;

        public MeshData(Vector2[] vertices, int[] triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }

        public static MeshData from(Mesh mesh)
        {
            return new MeshData(mesh.vertices.Select(v => (Vector2)v).ToArray(), mesh.triangles);
        }
    }

    private Subject<PSPolygon> clipSubject = new Subject<PSPolygon>();
    private void Start()
    {
        var initialMeshData = MeshData.from(GetComponent<MeshFilter>().sharedMesh);
        clipSubject
        .ObserveOn(Scheduler.ThreadPool)
        .Scan(initialMeshData, (data, clipPolygon) => UpdateMeshData(data, clipPolygon))
        .ObserveOnMainThread()
        .Subscribe(onNext: result => UpdateMesh(result));
    }
    public TerrainMesh terrainMesh;
    public void destroyTerrain(Vector2 position, float radius)
    {
        var startTime = Time.realtimeSinceStartup;
        var explosionSteps = Mathf.Max(Mathf.CeilToInt(terrainMesh.steps * radius / terrainMesh.outerRadius * 2), 6);
        var clipPolygon = createCirclePolygon(position, radius, explosionSteps);
        clipSubject.OnNext(clipPolygon);
        UpdateColliders(clipPolygon);
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

        return new MeshData(newVertices.ToArray(), newTriangles.ToArray());
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

        mesh.vertices = data.vertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        //mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = data.vertices.Select(terrainMesh.getUV).ToArray();
        mesh.uv2 = data.vertices.Select(terrainMesh.getUV2).ToArray();
        mesh.triangles = data.triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void UpdateColliders(PSPolygon clipPolygon)
    {
        var oldCollider = GetComponent<PolygonCollider2D>();
        var newPaths = Enumerable.Range(0, oldCollider.pathCount)
        .Select(i => oldCollider.GetPath(i))
        .SelectMany(p => PSPolygon.difference(p, clipPolygon.points))
        .ToList();
        Destroy(oldCollider);

        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.pathCount = 0;

        foreach (var p in newPaths)
        {
            polygonCollider.pathCount++;
            polygonCollider.SetPath(polygonCollider.pathCount - 1, p.ToArray());
        }
    }
}
