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

public class TerrainPiece : MonoBehaviour
{

    public TerrainMesh terrainMesh;
    public void destroyTerrain(Vector2 position, float radius)
    {
        var startTime = Time.realtimeSinceStartup;
        var explosionSteps = Mathf.Max(Mathf.CeilToInt(terrainMesh.steps * radius / terrainMesh.outerRadius * 2), 6);
        var clipPolygon = createCirclePolygon(position, radius, explosionSteps);
        // TODO optimize re-calculating mesh and colliders by doing it in background
        // thread and just updating new vertices, triangles and collider paths
        UpdateMesh(clipPolygon);
        UpdateColliders(clipPolygon);
        Debug.Log("Updated mesh and colliders took: " + Mathf.RoundToInt((Time.realtimeSinceStartup - startTime) * 1000) + "ms");
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

    private void UpdateMesh(PSPolygon clipPolygon)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        var oldVertices = mesh.vertices;
        var oldTriangles = mesh.triangles;
        mesh.Clear();

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

        mesh.vertices = newVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        //mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = newVertices.Select(terrainMesh.getUV).ToArray();
        mesh.uv2 = newVertices.Select(terrainMesh.getUV2).ToArray();
        mesh.triangles = newTriangles.ToArray();
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
