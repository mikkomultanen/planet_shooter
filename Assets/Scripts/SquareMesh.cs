using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SquareMesh : MonoBehaviour
{

    public float width = 1;
    public float height = 1;
	public float textureScaleU = 100;
	public float textureScaleV = 100;

    private Mesh mesh;

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
    void GenerateMesh()
    {
        GenerateSquare();
    }
#endif

    private void Awake()
    {
        GenerateSquare();
    }

    private void GenerateSquare()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
		Mesh mesh = meshFilter.sharedMesh;
		if (mesh == null) {
			meshFilter.mesh = new Mesh ();
			mesh = meshFilter.sharedMesh;
		}

        var vertices = new Vector3[4];
        var halfWidth = width / 2;
        var halfHeight = height / 2;

        vertices[0] = new Vector3(-halfWidth, -halfHeight, 0);
        vertices[1] = new Vector3(halfWidth, -halfHeight, 0);
        vertices[2] = new Vector3(-halfWidth, halfHeight, 0);
        vertices[3] = new Vector3(halfWidth, halfHeight, 0);

        mesh.vertices = vertices;

        var tri = new int[6];

        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;

        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;

        mesh.triangles = tri;

		SpriteExploder.calcNormals(gameObject);

        var uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(textureScaleU, 0);
        uv[2] = new Vector2(0, textureScaleV);
        uv[3] = new Vector2(textureScaleU, textureScaleV);

        mesh.uv = uv;
    }
}
