using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Delaunay;
using Delaunay.Geo;

class Noise {
	private float aMin;
	private float aMax;
	private float[] a;
	private int[] f;
	private float[] p;

	public Noise(float aMin, float aMax, int minF, int maxF, int n) {
		this.aMin = aMin;
		this.aMax = aMax;
		this.a = new float[n];
		this.f = new int[n];
		this.p = new float[n];
		float sumA = 0;
		for (int i = 0; i < n; i++)
		{
			this.a[i] = Random.Range(n - i - 0.9f, n - i);
			sumA += this.a[i];
			this.f[i] = Mathf.RoundToInt(Random.Range((i + 1) * minF, (i + 1) * maxF));
			this.p[i] = Random.Range(0, 2 * Mathf.PI);
		}
		float scale = 1.0f / sumA;
		for (int i = 0; i < n; i++)
		{
			this.a[i] *= scale;
		}
	}

	public float value(float angle) {
		float sum = 0;
		for (int i = 0; i < a.Length; i++)
		{
			sum += a[i] * Mathf.Sin(f[i] * angle + p[i]);
		}
		return Mathf.Lerp(aMin, aMax, 0.5f * sum + 0.5f);
	}
}
class Cave {
	public float r = 1;
	private Noise wave;
	private Noise thickness;
	public Cave(float r, float aMin, float aMax, float tMin, float tMax) {
		this.r = r;
		this.wave = new Noise(aMin, aMax, 1, 5, 5);
		this.thickness = new Noise(tMin, tMax, 11, 17, 5);
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

	public bool isFloor(Vector2 coord) {
		var angle = Mathf.Atan2(coord.x, coord.y);
		var magnitude = coord.magnitude;
		return Mathf.Abs(magnitude - floorMagnitude(angle)) < 0.1f;
	}

	public bool isCeiling(Vector2 coord) {
		var angle = Mathf.Atan2(coord.x, coord.y);
		var magnitude = coord.magnitude;
		return Mathf.Abs(magnitude - ceilingMagnitude(angle)) < 0.1f;
	}

	public float ceilingMagnitude(float angle) {
		return waveValue(angle) + thicknessValue(angle) / 2;
	}

	public float floorMagnitude(float angle) {
		return waveValue(angle) - thicknessValue(angle) / 2;
	}

	public float waveValue(float angle) {
		return wave.value(angle) + r;
	}

	public float thicknessValue(float angle) {
		return thickness.value(angle);
	}
}

class Shafts {
	private float radius;
	private float w;
	private Noise thicknessX = new Noise(1, 2, 5, 23, 3);
	private Noise thicknessY = new Noise(1, 2, 5, 23, 3);

	public Shafts(float radius) {
		this.radius = radius;
		w = 1.0f / radius * Mathf.PI;
	}
	public List<Vector2> coords(int steps) {
		var result = new List<Vector2>();
		var d = radius / steps;
		var dir1 = new Vector2(1, 0);
		var dir2 = new Vector2(0, 1);
		var dir3 = new Vector2(-1, 0);
		var dir4 = new Vector2(0, -1);
		float p;
		float angle;
		for (int i = 1; i <= steps; i++)
		{
			p = i * d;
			angle = p * w;
			result.Add(dir1 * p + dir2 * thicknessX.value(angle));
			result.Add(dir1 * p + dir4 * thicknessX.value(angle));
			result.Add(dir3 * p + dir2 * thicknessX.value(-angle));
			result.Add(dir3 * p + dir4 * thicknessX.value(-angle));
			result.Add(dir2 * p + dir1 * thicknessY.value(angle));
			result.Add(dir2 * p + dir3 * thicknessY.value(angle));
			result.Add(dir4 * p + dir1 * thicknessY.value(-angle));
			result.Add(dir4 * p + dir3 * thicknessY.value(-angle));
		}
		return result;
	}
	public bool inside(Vector2 coord) {
		return Mathf.Abs(coord.x) < thicknessY.value(coord.y * w) || Mathf.Abs(coord.y) < thicknessX.value(coord.x * w);
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
	private Shafts shafts;
    private Mesh mesh;
	private List<Vector2> points = new List<Vector2> ();
	private List<List<Vector2>> polygons = new List<List<Vector2>> ();

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
		shafts = new Shafts(outerRadius);
		float r = (innerRadius + outerRadius) / 2;
		float t = (outerRadius - innerRadius);
		caves.Clear();
		int n = Random.Range(2,4);
		for (int i = 0; i < n; i++) {
			caves.Add(new Cave(r - t/4 * i/(n - 1), -t/4, t/4 + t/8 , t/8, t / 4));
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
		var innerStepsMod = Mathf.CeilToInt(outerRadius / innerRadius);
        for (int i = 0; i < steps; i++)
        {
			if (i % innerStepsMod == 0) {
				points.Add(direction * innerRadius);
			}
			var angle = Mathf.Atan2(direction.x, direction.y);
			foreach (Cave cave in caves) {
				var ceiling = cave.ceilingMagnitude(angle);
				var floor = cave.floorMagnitude(angle);
				points.Add(direction * (ceiling + 1));
				points.Add(direction * ceiling);
				points.Add(direction * floor);
				points.Add(direction * (floor - 1));
			}
			points.Add(direction * outerRadius);
			direction = Quaternion.Euler(0, 0, -360.0f / steps) * direction;
        }
		var shaftSteps = Mathf.RoundToInt(steps / (2 * Mathf.PI));
		points.AddRange(shafts.coords(shaftSteps));
		points = points.Where(p => shouldAdd(p)).ToList();

		Rect rect = new Rect(-outerRadius, -outerRadius, 2*outerRadius, 2*outerRadius);
		Voronoi voronoi = new Delaunay.Voronoi (points, null, rect);
		var coords = voronoi.SiteCoords();
		mesh.vertices = coords.Select(c => new Vector3(c.x, c.y, 0)).ToArray();
		mesh.normals = coords.Select(caveNormal).ToArray();
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
		GenerateColliders();
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
		return new Vector2(angle / (2 * Mathf.PI) * textureScaleU, Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV);
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

	private Vector3 caveNormal(Vector2 coord) {
		foreach (Cave cave in caves) {
			if (cave.isFloor(coord)) {
				return new Vector3(coord.x, coord.y, 0).normalized;
			}
		}
		foreach (Cave cave in caves) {
			if (cave.isCeiling(coord)) {
				return new Vector3(-coord.x, -coord.y, 0).normalized;
			}
		}
		return new Vector3(0, 0, 0);
	}

	private void GenerateColliders() {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;

		// Get triangles and vertices from mesh
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
        Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
        for (int i = 0; i < triangles.Length; i += 3) {
            for (int e = 0; e < 3; e++) {
                int vert1 = triangles[i + e];
                int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                if (edges.ContainsKey(edge)) {
                    edges.Remove(edge);
                } else {
                    edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                }
            }
        }

        // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
		HashSet<int> validVertices = new HashSet<int>();
        Dictionary<int, int> lookup = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> edge in edges.Values) {
            if (lookup.ContainsKey(edge.Key) == false) {
				validVertices.Add(edge.Key);
                lookup.Add(edge.Key, edge.Value);
            }
        }

		polygons.Clear();
		#if UNITY_EDITOR
		DestroyImmediate(GetComponent<PolygonCollider2D>());
		#else
		Destroy(GetComponent<PolygonCollider2D>());
		#endif
        // Create empty polygon collider
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.pathCount = 0;

        // Loop through edge vertices in order
        int startVert = 0;
        int nextVert = startVert;
        List<Vector2> colliderPath = new List<Vector2>();
        while (true) {

            // Add vertex to collider path
            colliderPath.Add(vertices[nextVert]);
			var removed = validVertices.Remove(nextVert);
			if (!removed) {
				// Edges share a vertex
				colliderPath.Clear();
				Debug.Log("ColliderPath invalid validVertices " + validVertices.Count);

				// Go to next shape if one exists
                if (validVertices.Count > 0) {
                    // Set starting and next vertices
                    startVert = validVertices.First();
                    nextVert = startVert;

                    // Continue to next loop
                    continue;
                }

                // No more verts
                break;
			}

            // Get next vertex
            nextVert = lookup[nextVert];

            // Shape complete
            if (nextVert == startVert) {

				if (colliderPath.Count > 5) {
					float area = PolygonArea(colliderPath);
					if (area > 20) {
						polygons.Add(colliderPath.ToList());
						// Add path to polygon collider
						polygonCollider.pathCount++;
						polygonCollider.SetPath(polygonCollider.pathCount - 1, colliderPath.ToArray());
					}
				}
                colliderPath.Clear();

                // Go to next shape if one exists
                if (validVertices.Count > 0) {
                    // Set starting and next vertices
                    startVert = validVertices.First();
                    nextVert = startVert;

                    // Continue to next loop
                    continue;
                }

                // No more verts
                break;
            }
        }		
	}

	private float PolygonArea(List<Vector2> p)
	{
		int n = p.Count;
		float sum = p[0].x * ( p[1].y - p[n - 1].y );
		for( int i = 1; i < n - 1; ++i )
		{
			sum += p[i].x * ( p[i + 1].y - p[i - 1].y );
		}
		sum += p[n - 1].x * ( p[0].y - p[n - 2].y );
		return Mathf.Abs(0.5f * sum);
	}

#if UNITY_EDITOR
	void OnDrawGizmos ()
	{
		if (Application.isEditor) {
			Gizmos.color = Color.yellow;
			Gizmos.matrix = transform.localToWorldMatrix;
			Vector2 offset = (Vector2)transform.position * 0;
			foreach (List<Vector2> polygon in polygons) {
				for (int i = 0; i < polygon.Count; i++) {
					if (i + 1 == polygon.Count) {
						Gizmos.DrawLine (polygon [i] + offset, polygon [0] + offset);
					} else {
						Gizmos.DrawLine (polygon [i] + offset, polygon [i + 1] + offset);
					}
				}
			}

			Gizmos.color = Color.red;
			float diamondSize = 0.2f;
			List<Vector2> diamond = new List<Vector2> ();
			diamond.Add (new Vector2 (-diamondSize / transform.lossyScale.x, 0f));
			diamond.Add (new Vector2 (0f, diamondSize / transform.lossyScale.y));
			diamond.Add (new Vector2 (diamondSize / transform.lossyScale.x, 0f));
			diamond.Add (new Vector2 (0f, -diamondSize / transform.lossyScale.y));
			foreach (Vector2 point in points) {
				for (int i = 0; i < diamond.Count; i++) {
					Vector2 diamondOffset = point + offset;
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
