using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainPiece : MonoBehaviour {

	public TerrainMesh terrainMesh;
	public List<TerrainBlock> terrainBlocks;
	// Use this for initialization
	public void destroyTerrain(Vector2 position, float radius)
    {   
        var startTime = Time.realtimeSinceStartup;
        var explosionSteps = Mathf.Max(Mathf.CeilToInt(terrainMesh.steps * radius / terrainMesh.outerRadius * 2), 6);
        var removePolygon = createCirclePolygon(position, radius, explosionSteps);
		// TODO optimize re-calculating mesh and colliders by doing it in background
		// thread and just updating new vertices, triangles and collider paths
		UpdateTerrainBlocks(removePolygon);
		Debug.Log("Updated mesh and colliders took: " + Mathf.RoundToInt((Time.realtimeSinceStartup - startTime) * 1000) + "ms" );
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

	private void UpdateTerrainBlocks(PSPolygon removePolygon)
	{
        var newBlocks = new List<TerrainBlock>();
        foreach (var block in terrainBlocks)
        {   
            // TODO optimize generating new blocks by using old triangulation
            PSPolygon.remove(block.polygon, removePolygon).ForEach(p => newBlocks.Add(new TerrainBlock(p)));
        }
		terrainBlocks = newBlocks;
		GenerateMesh();
		GenerateColliders();
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
        foreach (var block in terrainBlocks)
        {
            allVertices.AddRange(block.vertices);
            allTriangles.AddRange(block.triangles.Select(i => i + currentIndex));
            currentIndex = allVertices.Count;
        }

        mesh.vertices = allVertices.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
        //mesh.normals = allVertices.Select(caveNormal).ToArray();
        mesh.uv = allVertices.Select(terrainMesh.getUV).ToArray();
        mesh.uv2 = allVertices.Select(terrainMesh.getUV2).ToArray();
        mesh.triangles = allTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
	private void GenerateColliders()
    {
#if UNITY_EDITOR
		if (EditorApplication.isPlaying) {
    		Destroy(GetComponent<PolygonCollider2D>());
        } else {
            DestroyImmediate(GetComponent<PolygonCollider2D>());
        }
#else
		Destroy(GetComponent<PolygonCollider2D>());
#endif
        // Create empty polygon collider
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.pathCount = 0;

        foreach (var block in terrainBlocks)
        {
            // Add path to polygon collider
            polygonCollider.pathCount++;
            polygonCollider.SetPath(polygonCollider.pathCount - 1, block.polygon.points);
        }
    }
}
