using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
[RequireComponent(typeof(MeshFilter))]
public class TerrainDistanceField : MonoBehaviour {

	public float size = 256;
	public float nearClip = 0.01f;
    public float farClip = 500;	
	public LayerMask layerMask;
	public RenderTexture terrainDistanceField;
	private Camera cam;
	private Material material;
	private Material voronoiMaterial;
    private void Awake()
    {
        GenerateMesh();
    }

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
#endif
    private void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
		Mesh mesh = meshFilter.sharedMesh;
		if (mesh == null) {
			meshFilter.mesh = new Mesh ();
			mesh = meshFilter.sharedMesh;
		}

        var vertices = new Vector3[4];
        var halfSize = size / 2;

        vertices[0] = new Vector3(-halfSize, -halfSize, 0);
        vertices[1] = new Vector3(halfSize, -halfSize, 0);
        vertices[2] = new Vector3(-halfSize, halfSize, 0);
        vertices[3] = new Vector3(halfSize, halfSize, 0);

        mesh.vertices = vertices;

        var tri = new int[6];

        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;

        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;

        mesh.triangles = tri;

        var uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.uv = uv;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
    }
	
	private void OnEnable() {
		terrainDistanceField = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		terrainDistanceField.antiAliasing = 1;
		terrainDistanceField.isPowerOfTwo = true;
		terrainDistanceField.filterMode = FilterMode.Trilinear;
		terrainDistanceField.hideFlags = HideFlags.DontSave;
		GameObject go = new GameObject("DistanceFieldCamera", typeof(Camera));
		go.hideFlags = HideFlags.HideAndDontSave;
		go.transform.position = transform.position;
		go.transform.rotation = Quaternion.identity;
		cam = go.GetComponent<Camera>();
		cam.cullingMask = layerMask;
        var halfSize = size / 2;
		cam.backgroundColor = Color.black;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.projectionMatrix = Matrix4x4.Ortho(-halfSize, halfSize, -halfSize, halfSize, nearClip, farClip);
		cam.enabled = false;
		cam.SetReplacementShader(Shader.Find("Hidden/PlanetShooter/DistanceField"),"");
		material = new Material(Shader.Find("PlanetShooter/DistanceFieldDebug"));
		material.SetTexture("_MainTex", terrainDistanceField);
		MeshRenderer renderer = GetComponent<MeshRenderer>();
		renderer.material = material;
		voronoiMaterial = new Material(Shader.Find("Hidden/PlanetShooter/Voronoi"));
	}

	private void LateUpdate() {
		int textureSize = 2048;
		RenderTexture shapes = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear, 1);
		shapes.filterMode = FilterMode.Point;
		cam.targetTexture = shapes;
		cam.Render();
		cam.targetTexture = null;

		RenderTexture voronoi1 = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear, 1);
		voronoi1.filterMode = FilterMode.Point;

		Graphics.Blit(shapes, voronoi1, voronoiMaterial, 0);

		RenderTexture.ReleaseTemporary(shapes);

		RenderTexture voronoi2 = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear, 1);
		voronoi2.filterMode = FilterMode.Point;

		voronoiMaterial.SetInt("_Offset", 4);
		Graphics.Blit(voronoi1, voronoi2, voronoiMaterial, 1);
		voronoiMaterial.SetInt("_Offset", 2);
		Graphics.Blit(voronoi2, voronoi1, voronoiMaterial, 1);
		voronoiMaterial.SetInt("_Offset", 1);
		Graphics.Blit(voronoi1, voronoi2, voronoiMaterial, 1);

		voronoiMaterial.SetFloat("_DistanceScale", size);

		RenderTexture distanceField = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1);
		distanceField.filterMode = FilterMode.Bilinear;
		Graphics.Blit(voronoi2, distanceField, voronoiMaterial, 2);

		RenderTexture.ReleaseTemporary(voronoi1);
		RenderTexture.ReleaseTemporary(voronoi2);

		voronoiMaterial.SetFloat("_BoxOffset", 1);
		Graphics.Blit(distanceField, terrainDistanceField, voronoiMaterial, 3);

		RenderTexture.ReleaseTemporary(distanceField);
	}

	private void OnDisable() {
		DestroyImmediate(cam);
		DestroyImmediate(terrainDistanceField);
		DestroyImmediate(material);
		DestroyImmediate(voronoiMaterial);
	}
}
