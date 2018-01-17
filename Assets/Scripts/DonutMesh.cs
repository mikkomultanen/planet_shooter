using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DonutMesh : MonoBehaviour
{

    public float outerRadius = 1;
	public float innerRadius = 0.1f;	
    public int steps = 128;
    public float textureScaleU = 1;
    public float textureScaleV = 1;

    private Mesh mesh;

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
    void GenerateMesh()
    {
        GenerateDonut();
    }
#endif

    private void Awake()
    {
        GenerateDonut();
    }

    private void GenerateDonut()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }

		var verticesCount = 2 * (steps + 1);
        var vertices = new Vector3[verticesCount];
        var normals = new Vector3[verticesCount];
        var uv = new Vector2[verticesCount];

		var direction = new Vector2(1, 0);
        for (int i = 0; i < steps; i++)
        {
			var j = 2 * i;
			vertices[j] = new Vector3(direction.x, direction.y, 0) * outerRadius;
			normals[j] = vertices[2 * i].normalized;
			uv[j] = new Vector2(textureScaleU * i / steps, textureScaleV);
			direction = Quaternion.Euler(0, 0, -180.0f / steps) * direction;
			vertices[j + 1] = new Vector3(direction.x, direction.y, 0) * innerRadius;
			normals[j + 1] = new Vector3(0, 0, -1);
			uv[j + 1] = new Vector2(textureScaleU * (i + 0.5f) / steps, 0);
			direction = Quaternion.Euler(0, 0, -180.0f / steps) * direction;
        }
        vertices[2 * steps] = vertices[0];
        normals[2 * steps] = normals[0];
        uv[2 * steps] = new Vector2(textureScaleU, textureScaleV);
        vertices[2 * steps + 1] = vertices[1];
        normals[2 * steps + 1] = normals[1];
        uv[2 * steps + 1] = new Vector2(textureScaleU * (steps + 0.5f) / steps, 0);
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;

        var tri = new int[steps * 6];
        for (int i = 0; i < steps; i++)
        {
			var j = 2 * i;
			var index1 = j;
			var index2 = j + 1;
			var index3 = (j + 2) % verticesCount;
			var index4 = (j + 3) % verticesCount;

			tri[3 * j] = index1;
			tri[3 * j + 1] = index3; 
			tri[3 * j + 2] = index2; 

			tri[3 * j + 3] = index2;
			tri[3 * j + 4] = index3; 
			tri[3 * j + 5] = index4; 
        }
        mesh.triangles = tri;
		mesh.RecalculateBounds();
    }
}
