using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;

class Cave {
	public float r = 1;
	public float a = 1;
	public float f = 1;
	public float p = 0;
	public float tMin = 1;
	public float tMax = 2;
	public float tF = 1;
	public float tP = 0;

	public Cave(float r, float a, float f, float p, float tMin, float tMax, float tF, float tP) {
		this.r = r;
		this.a = a;
		this.f = f;
		this.p = p;
		this.tMin = tMin;
		this.tMax = tMax;
		this.tF = tF;
		this.tP = tP;
	}

	public Vector2 ceiling(Vector2 direction) {
		var angle = Mathf.Atan2(direction.x, direction.y);
		return direction.normalized * ceilingMagnitude(angle);
	}

	public Vector2 floor(Vector2 direction) {
		var angle = Mathf.Atan2(direction.x, direction.y);
		return direction.normalized * floorMagnitude(angle);
	}

	public bool inside(Vector2 coord) {
		var angle = Mathf.Atan2(coord.x, coord.y);
		var magnitude = coord.magnitude;
		return magnitude - 0.005f > floorMagnitude(angle) && magnitude + 0.005f < ceilingMagnitude(angle);
	}

	private float ceilingMagnitude(float angle) {
		return wave(angle) + thickness(angle) / 2;
	}

	private float floorMagnitude(float angle) {
		return wave(angle) - thickness(angle) / 2;
	}

	private float wave(float angle) {
		return a * Mathf.Sin(f * angle + p) + r;
	}

	private float thickness(float angle) {
		return Mathf.Lerp(tMin, tMax, 0.5f * Mathf.Sin(tF * angle + tP) + 0.5f);
	}
}

class Shafts {
	public float t = 2;

	public List<Vector2> coords(float radius, int steps) {
		var result = new List<Vector2>();
		var d = radius / steps;
		var dir1 = new Vector2(1, 0);
		var dir2 = new Vector2(0, 1);
		var dir3 = new Vector2(-1, 0);
		var dir4 = new Vector2(0, -1);
		for (int i = 1; i <= steps; i++)
		{
			result.Add(dir1 * i * d + dir2 * t);
			result.Add(dir1 * i * d + dir4 * t);
			result.Add(dir3 * i * d + dir2 * t);
			result.Add(dir3 * i * d + dir4 * t);
			result.Add(dir2 * i * d + dir1 * t);
			result.Add(dir2 * i * d + dir3 * t);
			result.Add(dir4 * i * d + dir1 * t);
			result.Add(dir4 * i * d + dir3 * t);
		}
		return result;
	}
	public bool inside(Vector2 coord) {
		return Mathf.Abs(coord.x) < t || Mathf.Abs(coord.y) < t;
	}
}

[RequireComponent(typeof(MeshFilter))]
public class TerrainMesh : MonoBehaviour {

    public float outerRadius = 1;
	public float innerRadius = 0.1f;	
    public int steps = 128;
    public float textureScaleU = 6;
    public float textureScaleV = 1;

	private List<Cave> caves = new List<Cave>();
	private Shafts shafts = new Shafts();
    private Mesh mesh;
	private List<Vector2> points = new List<Vector2> ();

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
    void GenerateMesh()
    {
        GenerateTerrain();
    }
#endif

    private void Awake()
    {
        GenerateTerrain();
    }

	private void GenerateCaves() {
		float ar = (innerRadius + outerRadius) / 2;
		float t = (outerRadius - innerRadius);
		caves.Clear();
		for (int i = 0; i < 4; i++) {
			var r = ar + Random.Range(-t / 2, 0);
			var a = Random.Range(t/16, t / 8);
			var f = Mathf.RoundToInt(Random.Range(5, 11));
			var p = Random.Range(0, 2 * Mathf.PI);
			var tMin = t/(10 * i + 10);
			var tMax = Random.Range(tMin, t / 2);
			var tF = Mathf.RoundToInt(Random.Range(11, 17));
			var tP = Random.Range(0, 2 * Mathf.PI);
			caves.Add(new Cave(r, a, f, p, tMin, tMax, tF, tP));
		}
	}

    private void GenerateTerrain()
    {
		GenerateCaves();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
		mesh.Clear();
		points.Clear();

		var direction = new Vector2(1, 0);
        for (int i = 0; i < steps; i++)
        {
			var j = 2 * i;
			points.Add(new Vector2(direction.x, direction.y) * innerRadius);
			foreach (Cave cave in caves) {
				points.Add(cave.floor(direction));
				points.Add(cave.ceiling(direction));
			}
			points.Add(new Vector2(direction.x, direction.y) * outerRadius);
			direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
		points.AddRange(shafts.coords(outerRadius, 20));
		points = points.Where(p => shouldAdd(p)).ToList();

		Rect rect = new Rect(-outerRadius, -outerRadius, 2*outerRadius, 2*outerRadius);
		Voronoi voronoi = new Delaunay.Voronoi (points, null, rect);
		var coords = voronoi.SiteCoords();
		mesh.vertices = coords.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
		mesh.uv = coords.Select(getUV).ToArray();


		var triangles = new List<int>();
		foreach (Triangle triangle in voronoi.Triangles()) {
			if (shouldAdd(triangle)) {
				foreach (Site site in triangle.sites) {
					triangles.Add(indexOf(coords, site.Coord));
				}
			}
		}
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
    }

	private int indexOf(List<Vector2> coords, Vector2 coord) {
		for (int i = 0; i < coords.Count; i++) {
			if (Site.CloseEnough(coords[i], coord)) {
				return i;
			}
		}
		return -1;
	}

	private Vector2 getUV(Vector2 coord) {
		var angle = Mathf.Atan2(coord.x, coord.y);
		var magnitude = coord.magnitude;
		return new Vector2(angle / Mathf.PI * textureScaleU, Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV);
	}

	private Vector2 getCenter(Triangle triangle) {
		return triangle.sites.Aggregate(new Vector2(0, 0), (center, next) => center + next.Coord) / 3;
	}

	private bool shouldAdd(Triangle triangle) {
		return shouldAdd(getCenter(triangle));
	}

	private bool shouldAdd(Vector2 coord) {
		var magnitude = coord.magnitude;
		var insideTerrain = magnitude + 0.005 > innerRadius && magnitude - 0.005f < outerRadius;
		var insideCave = this.insideCave(coord);
		var insideShaft = shafts.inside(coord);
		return insideTerrain && !insideCave && !insideShaft;
	}

	private bool insideCave(Vector2 coord) {
		foreach (Cave cave in caves) {
			if (cave.inside(coord)) {
				return true;
			}
		}
		return false;
	}


#if UNITY_EDITOR
	void OnDrawGizmos ()
	{
		if (Application.isEditor) {
			Gizmos.color = Color.red;
			float diamondSize = 0.2f;
			List<Vector2> diamond = new List<Vector2> ();
			diamond.Add (new Vector2 (-diamondSize / transform.lossyScale.x, 0f));
			diamond.Add (new Vector2 (0f, diamondSize / transform.lossyScale.y));
			diamond.Add (new Vector2 (diamondSize / transform.lossyScale.x, 0f));
			diamond.Add (new Vector2 (0f, -diamondSize / transform.lossyScale.y));
			foreach (Vector2 point in points) {
				for (int i = 0; i < diamond.Count; i++) {
					Vector2 diamondOffset = point;
					if (i + 1 == diamond.Count) {
						Gizmos.DrawLine (diamond [i] + diamondOffset, diamond [0] + diamondOffset);
					} else {
						Gizmos.DrawLine (diamond [i] + diamondOffset, diamond [i + 1] + diamondOffset);
					}
				}
			}
		}
	}
#endif
}
