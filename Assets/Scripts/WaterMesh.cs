using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterMesh : MonoBehaviour
{

    public float radius = 1;
    public int steps = 128;
    public float textureScaleU = 1;
    public float textureScaleV = 1;

    private Mesh mesh;

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
    void GenerateMesh()
    {
        GenerateWater();
    }
#endif

    private void Awake()
    {
        GenerateWater();
    }

    private void GenerateWater()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }

        var vertices = new Vector3[1 + steps + 1];
        var normals = new Vector3[1 + steps + 1];
        var uv = new Vector2[1 + steps + 1];

        vertices[0] = new Vector3(0, 0, 0);
        normals[0] = new Vector3(0, 0, -1);
        uv[0] = new Vector2(0.5f, 0);
		var direction = new Vector2(radius, 0);
        for (int i = 0; i < steps; i++)
        {
			vertices[1 + i] = new Vector3(direction.x, direction.y, 0);
			normals[1 + i] = vertices[1 + i].normalized;
			uv[1 + i] = new Vector2(textureScaleU * i / steps, textureScaleV);
			direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
        vertices[1 + steps] = vertices[1];
        normals[1 + steps] = normals[1];
        uv[1 + steps] = new Vector2(textureScaleU, textureScaleV);
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;

        var tri = new int[steps * 3];
        for (int i = 0; i < steps; i++)
        {
			tri[3 * i] = 0;
			tri[3 * i + 1] = 1 + i; 
			tri[3 * i + 2] = 1 + (i + 1); 
        }
        mesh.triangles = tri;
		mesh.RecalculateBounds();
    }
}
