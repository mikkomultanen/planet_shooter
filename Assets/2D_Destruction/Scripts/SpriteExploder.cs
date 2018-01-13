using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public static class SpriteExploder
{
	public static List<GameObject> GenerateTriangularPieces (GameObject source, int extraPoints = 0, int subshatterSteps = 0, Material mat = null)
	{
		List<GameObject> pieces = new List<GameObject> ();

		if (mat == null) {
			mat = createFragmentMaterial (source);
		}

		//get transform information
		Vector3 origScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion origRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;

		//get rigidbody information
		Vector2 origVelocity = source.GetComponent<Rigidbody2D> ().velocity;

		//get collider information
		PolygonCollider2D sourcePolyCollider = source.GetComponent<PolygonCollider2D> ();
		BoxCollider2D sourceBoxCollider = source.GetComponent<BoxCollider2D> ();
		List<Vector2> points = new List<Vector2> ();
		List<Vector2> borderPoints = new List<Vector2> ();

		//add points from the present collider
		if (sourcePolyCollider != null) {
			points = getPoints (sourcePolyCollider);
			borderPoints = getPoints (sourcePolyCollider);
		} else if (sourceBoxCollider != null) {
			points = getPoints (sourceBoxCollider);
			borderPoints = getPoints (sourceBoxCollider);
		}

		//create a bounding rectangle based on the polygon points
		Rect rect = getRect (source);

		//if the target polygon is a triangle, generate a point in the middle to allow for fracture
		if (points.Count == 3) {
			points.Add ((points [0] + points [1] + points [2]) / 3);
		}

		for (int i = 0; i < extraPoints; i++) {
			points.Add (new Vector2 (Random.Range (rect.width / -2, rect.width / 2), Random.Range (rect.height / -2 + rect.center.y, rect.height / 2 + rect.center.y)));
		}


		Voronoi voronoi = new Delaunay.Voronoi (points, null, rect);
        
		List<List<Vector2>> clippedTriangles = new List<List<Vector2>> ();
		foreach (Triangle tri in voronoi.Triangles()) {
			clippedTriangles = ClipperHelper.clip (borderPoints, tri);
			foreach (List<Vector2> triangle in clippedTriangles) {
				pieces.Add (generateTriangularPiece (source, triangle, origVelocity, origScale, origRotation, mat));
			}
		}
		List<GameObject> morePieces = new List<GameObject> ();
		if (subshatterSteps > 0) {
			subshatterSteps--;
			foreach (GameObject piece in pieces) {
				morePieces.AddRange (SpriteExploder.GenerateTriangularPieces (piece, extraPoints, subshatterSteps, mat));
				GameObject.DestroyImmediate (piece);
			}
		} else {
			morePieces = pieces;
		}

		//reset transform information
		source.transform.localScale = origScale;
		source.transform.localRotation = origRotation;

		Resources.UnloadUnusedAssets ();

		return morePieces;
	}

	private static GameObject generateTriangularPiece (GameObject source, List<Vector2> tri, Vector2 origVelocity, Vector3 origScale, Quaternion origRotation, Material mat)
	{
		//Create Game Object and set transform settings properly
		GameObject piece = new GameObject (source.name + " piece");
		piece.transform.position = source.transform.position;
		piece.transform.rotation = source.transform.rotation;
		piece.transform.localScale = source.transform.localScale;

		//Create and Add Mesh Components
		MeshFilter meshFilter = (MeshFilter)piece.AddComponent (typeof(MeshFilter));
		piece.AddComponent (typeof(MeshRenderer));

		Mesh uMesh = piece.GetComponent<MeshFilter> ().sharedMesh;
		if (uMesh == null) {
			meshFilter.mesh = new Mesh ();
			uMesh = meshFilter.sharedMesh;
		}
		Vector3[] vertices = new Vector3[3];
		int[] triangles = new int[3];

		vertices [0] = new Vector3 (tri [0].x, tri [0].y, 0);
		vertices [1] = new Vector3 (tri [1].x, tri [1].y, 0);
		vertices [2] = new Vector3 (tri [2].x, tri [2].y, 0);
		//triangles[0] = 0;
		//triangles[1] = 1;
		//triangles[2] = 2;
		triangles [0] = 2;
		triangles [1] = 1;
		triangles [2] = 0;

		uMesh.vertices = vertices;
		uMesh.triangles = triangles;
		if (source.GetComponent<SpriteRenderer> () != null) {
			uMesh.uv = calcUV (vertices, source.GetComponent<SpriteRenderer> (), source.transform);
		} else {
			uMesh.uv = calcUV (vertices, source.GetComponent<MeshRenderer> (), source.transform);
		}

		//set transform properties before fixing the pivot for easier rotation
		piece.transform.localScale = origScale;
		piece.transform.localRotation = origRotation;

		Vector3 diff = calcPivotCenterDiff (piece);
		centerMeshPivot (piece, diff);
		uMesh.RecalculateBounds ();
		uMesh.RecalculateNormals ();

		//setFragmentMaterial(piece, source);
		piece.GetComponent<MeshRenderer> ().sharedMaterial = mat;

		//assign mesh
		meshFilter.mesh = uMesh;

		//Create and Add Polygon Collider
		PolygonCollider2D collider = piece.AddComponent<PolygonCollider2D> ();
		collider.SetPath (0, new Vector2[]{ uMesh.vertices [0], uMesh.vertices [1], uMesh.vertices [2] });

		//Create and Add Rigidbody
		Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D> ();
		rigidbody.velocity = origVelocity;

		return piece;
	}

	public static List<GameObject> GenerateVoronoiPieces (GameObject source, int extraPoints = 0, int subshatterSteps = 0, Material mat = null)
	{
		List<GameObject> pieces = new List<GameObject> ();

		if (mat == null) {
			mat = createFragmentMaterial (source);
		}

		//get transform information
		Vector3 origScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion origRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;

		//get rigidbody information
		Vector2 origVelocity = source.GetComponent<Rigidbody2D> ().velocity;

		//get collider information
		PolygonCollider2D sourcePolyCollider = source.GetComponent<PolygonCollider2D> ();
		BoxCollider2D sourceBoxCollider = source.GetComponent<BoxCollider2D> ();
		List<Vector2> points = new List<Vector2> ();
		List<Vector2> borderPoints = new List<Vector2> ();
		if (sourcePolyCollider != null) {
			points = getPoints (sourcePolyCollider);
			borderPoints = getPoints (sourcePolyCollider);
		} else if (sourceBoxCollider != null) {
			points = getPoints (sourceBoxCollider);
			borderPoints = getPoints (sourceBoxCollider);
		}

		Rect rect = getRect (source);

		for (int i = 0; i < extraPoints; i++) {
			points.Add (new Vector2 (Random.Range (
				rect.width / -2 + rect.center.x, rect.width / 2 + rect.center.x), 
				Random.Range (rect.height / -2 + rect.center.y, rect.height / 2 + rect.center.y)
			));
		}


		Voronoi voronoi = new Delaunay.Voronoi (points, null, rect);
		List<List<Vector2>> clippedRegions = new List<List<Vector2>> ();
		foreach (List<Vector2> region in voronoi.Regions()) {
			clippedRegions = ClipperHelper.clip (borderPoints, region);
			foreach (List<Vector2> clippedRegion in clippedRegions) {
				pieces.Add (generateVoronoiPiece (source, clippedRegion, origVelocity, origScale, origRotation, mat));
			}
		}

		List<GameObject> morePieces = new List<GameObject> ();
		if (subshatterSteps > 0) {
			subshatterSteps--;
			foreach (GameObject piece in pieces) {
				morePieces.AddRange (SpriteExploder.GenerateVoronoiPieces (piece, extraPoints, subshatterSteps));
				GameObject.DestroyImmediate (piece);
			}
		} else {
			morePieces = pieces;
		}

		//reset transform information
		source.transform.localScale = origScale;
		source.transform.localRotation = origRotation;

		Resources.UnloadUnusedAssets ();

		return morePieces;
	}

	public static List<GameObject> GenerateCavePieces (GameObject source, PointsResult pointsResult, int subshatterSteps = 0, Material mat = null)
	{
		List<GameObject> pieces = new List<GameObject> ();

		if (mat == null) {
			mat = createFragmentMaterial (source);
		}

		//get transform information
		Vector3 origScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion origRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;

		//get rigidbody information
		Vector2 origVelocity = source.GetComponent<Rigidbody2D> ().velocity;

		Rect rect = getRect (source);

		List<Vector2> points = new List<Vector2> ();
		foreach (Point point in pointsResult.points) {
			points.Add (point.point);
		} 
		List<Vector2> borderPoints = pointsResult.borderPoints;

		Voronoi voronoi = new Delaunay.Voronoi (points, null, rect);
		List<List<Vector2>> clippedRegions = new List<List<Vector2>> ();
		foreach (Point point in pointsResult.points) {
			bool isWall = pointsResult.isWall (point);
			float magnitude = point.point.magnitude;
			bool isCell = magnitude > 100;
			bool isInStartingArea = Mathf.Abs (Mathf.Abs (point.point.x) - Mathf.Abs (point.point.y)) < 10;
			if (!isInStartingArea && (isWall || isCell)) {
				List<Vector2> region = voronoi.Region (point.point);
				clippedRegions = ClipperHelper.clip (borderPoints, region);
				foreach (List<Vector2> clippedRegion in clippedRegions) {
					pieces.Add (generateVoronoiPiece (source, clippedRegion, origVelocity, origScale, origRotation, mat));
				}
			}
		}

		List<GameObject> morePieces = new List<GameObject> ();
		if (subshatterSteps > 0) {
			subshatterSteps--;
			foreach (GameObject piece in pieces) {
				morePieces.AddRange (SpriteExploder.GenerateVoronoiPieces (piece, 3, subshatterSteps));
				GameObject.DestroyImmediate (piece);
			}
		} else {
			morePieces = pieces;
		}

		//reset transform information
		source.transform.localScale = origScale;
		source.transform.localRotation = origRotation;

		Resources.UnloadUnusedAssets ();

		return morePieces;
	}

	private static GameObject generateVoronoiPiece (GameObject source, List<Vector2> region, Vector2 origVelocity, Vector3 origScale, Quaternion origRotation, Material mat)
	{
		//Create Game Object and set transform settings properly
		GameObject piece = new GameObject (source.name + " piece");
		piece.transform.position = source.transform.position;
		piece.transform.rotation = source.transform.rotation;
		piece.transform.localScale = source.transform.localScale;

		//Create and Add Mesh Components
		MeshFilter meshFilter = (MeshFilter)piece.AddComponent (typeof(MeshFilter));
		piece.AddComponent (typeof(MeshRenderer));

		Mesh uMesh = piece.GetComponent<MeshFilter> ().sharedMesh;
		if (uMesh == null) {
			meshFilter.mesh = new Mesh ();
			uMesh = meshFilter.sharedMesh;
		}

		Voronoi voronoi = new Voronoi (region, null, getRect (region));

		Vector3[] vertices = calcVerts (voronoi);
		int[] triangles = calcTriangles (voronoi);

		uMesh.vertices = vertices;
		uMesh.triangles = triangles;
		if (source.GetComponent<SpriteRenderer> () != null) {
			uMesh.uv = calcUV (vertices, source.GetComponent<SpriteRenderer> (), source.transform);
		} else {
			uMesh.uv = calcUV (vertices, source.GetComponent<MeshRenderer> (), source.transform);
		}

		//set transform properties before fixing the pivot for easier rotation
		piece.transform.localScale = origScale;
		piece.transform.localRotation = origRotation;

		Vector3 diff = calcPivotCenterDiff (piece);
		centerMeshPivot (piece, diff);
		uMesh.RecalculateBounds ();
		calcNormals (piece);
		calcOutlineDirToColor (piece);

		//setFragmentMaterial(piece, source);
		piece.GetComponent<MeshRenderer> ().sharedMaterial = mat;

		//assign mesh
		meshFilter.mesh = uMesh;

		//Create and Add Polygon Collider
		PolygonCollider2D collider = piece.AddComponent<PolygonCollider2D> ();
		collider.SetPath (0, calcPolyColliderPoints (region, diff));
			
		//Create and Add Rigidbody
		Rigidbody2D rigidbody = piece.AddComponent<Rigidbody2D> ();
		rigidbody.velocity = origVelocity;

		return piece;
	}

	private static Vector2 getCenter (List<Vector2> points)
	{
		Vector2 result = new Vector2 (0f, 0f);
		foreach (Vector2 point in points) {
			result += point;
		}
		return result / points.Count;
	}

	/// <summary>
	/// generates a list of points from a box collider
	/// </summary>
	/// <param name="collider">source box collider</param>
	/// <returns>list of points</returns>
	private static List<Vector2> getPoints (BoxCollider2D collider)
	{
		List<Vector2> points = new List<Vector2> ();

		Vector2 center = collider.offset;
		Vector2 size = collider.size;
		//bottom left
		points.Add (new Vector2 ((center.x - size.x / 2), (center.y - size.y / 2)));
		//top left
		points.Add (new Vector2 ((center.x - size.x / 2), (center.y + size.y / 2)));
		//top right
		points.Add (new Vector2 ((center.x + size.x / 2), (center.y + size.y / 2)));
		//bottom right
		points.Add (new Vector2 ((center.x + size.x / 2), (center.y - size.y / 2)));

		return points;
	}

	/// <summary>
	/// generates a list of points from a polygon collider
	/// </summary>
	/// <param name="collider">source polygon collider</param>
	/// <returns>list of points</returns>
	private static List<Vector2> getPoints (PolygonCollider2D collider)
	{
		List<Vector2> points = new List<Vector2> ();

		foreach (Vector2 point in collider.GetPath(0)) {
			points.Add (point);
		}

		return points;
	}

	private static List<Vector2> getRendererPoints (GameObject source)
	{
		List<Vector2> points = new List<Vector2> ();
		Bounds bounds = source.GetComponent<Renderer> ().bounds;
		points.Add (new Vector2 (bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y) - (Vector2)source.transform.position);
		points.Add (new Vector2 (bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y) - (Vector2)source.transform.position);
		points.Add (new Vector2 (bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y) - (Vector2)source.transform.position);
		points.Add (new Vector2 (bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y) - (Vector2)source.transform.position);
		return points;
	}

	/// <summary>
	/// generates a rectangle based on the rendering bounds of the object
	/// </summary>
	/// <param name="source">gameobject to get the rectangle from</param>
	/// <returns>a Rectangle representing the rendering bounds of the object</returns>
	private static Rect getRect (GameObject source)
	{
		Bounds bounds = source.GetComponent<Renderer> ().bounds;
		Rect rect = new Rect (bounds.extents.x * -1, bounds.extents.y * -1, bounds.size.x, bounds.size.y);
		rect.center = new Vector2 (rect.center.x + bounds.center.x - source.transform.position.x, rect.center.y + bounds.center.y - source.transform.position.y);
		return rect;
	}

	private static Rect getRect (List<Vector2> region)
	{
		Vector2 center = new Vector2 ();
		float minX = region [0].x;
		float maxX = minX;
		float minY = region [0].y;
		float maxY = minY;
		foreach (Vector2 v in region) {
			center += v;
			if (v.x < minX) {
				minX = v.x;
			}
			if (v.x > maxX) {
				maxX = v.x;
			}
			if (v.y < minY) {
				minY = v.y;
			}
			if (v.y > maxY) {
				maxY = v.y;
			}
		}
		center /= region.Count;
		Vector2 size = new Vector2 (maxX - minX, maxY - minY);
		return new Rect (center, size);
	}

	/// <summary>
	/// calculates the UV coordinates for the given vertices based on the provided Sprite
	/// </summary>
	/// <param name="vertices">vertices to generate the UV coordinates for</param>
	/// <param name="sRend">Sprite Renderer of original object</param>
	/// <param name="sTransform">Transform of the original object</param>
	/// <returns>array of UV coordinates for the mesh</returns>
	private static Vector2[] calcUV (Vector3[] vertices, SpriteRenderer sRend, Transform sTransform)
	{
		float texHeight = (sRend.bounds.extents.y * 2) / sTransform.localScale.y;
		float texWidth = (sRend.bounds.extents.x * 2) / sTransform.localScale.x;
		Vector3 botLeft = sTransform.InverseTransformPoint (new Vector3 (sRend.bounds.center.x - sRend.bounds.extents.x, sRend.bounds.center.y - sRend.bounds.extents.y, 0));
		Vector2[] uv = new Vector2[vertices.Length];

		Vector2[] sourceUV = sRend.sprite.uv;
		Vector2 uvMin;
		Vector2 uvMax;
		getUVRange (out uvMin, out uvMax, sourceUV);

		for (int i = 0; i < vertices.Length; i++) {

			float x = (vertices [i].x - botLeft.x) / texWidth;
			x = scaleRange (x, 0, 1, uvMin.x, uvMax.x);
			float y = (vertices [i].y - botLeft.y) / texHeight;
			y = scaleRange (y, 0, 1, uvMin.y, uvMax.y);

			uv [i] = new Vector2 (x, y);
		}
		return uv;
	}

	private static Vector2[] calcUV (Vector3[] vertices, MeshRenderer mRend, Transform sTransform)
	{
		float texHeight = (mRend.bounds.extents.y * 2) / sTransform.localScale.y;
		float texWidth = (mRend.bounds.extents.x * 2) / sTransform.localScale.x;
		Vector3 botLeft = sTransform.InverseTransformPoint (new Vector3 (mRend.bounds.center.x - mRend.bounds.extents.x, mRend.bounds.center.y - mRend.bounds.extents.y, 0));
		Vector2[] uv = new Vector2[vertices.Length];

		Vector2[] sourceUV = sTransform.GetComponent<MeshFilter> ().sharedMesh.uv;
		Vector2 uvMin;
		Vector2 uvMax;
		getUVRange (out uvMin, out uvMax, sourceUV);

		for (int i = 0; i < vertices.Length; i++) {
			float x = (vertices [i].x - botLeft.x) / texWidth;
			x = scaleRange (x, 0, 1, uvMin.x, uvMax.x);
			float y = (vertices [i].y - botLeft.y) / texHeight;
			y = scaleRange (y, 0, 1, uvMin.y, uvMax.y);

			uv [i] = new Vector2 (x, y);
		}
		return uv;
	}

	private static void getUVRange (out Vector2 min, out Vector2 max, Vector2[]uv)
	{
		min = uv [0];
		max = uv [0];

		foreach (Vector2 p in uv) {
			if (p.x < min.x) {
				min.x = p.x;
			}
			if (p.x > max.x) {
				max.x = p.x;
			}
			if (p.y < min.y) {
				min.y = p.y;
			}
			if (p.y > max.y) {
				max.y = p.y;
			}
		}
	}

	private static float scaleRange (float target, float oldMin, float oldMax, float newMin, float newMax)
	{
		return (target / ((oldMax - oldMin) / (newMax - newMin))) + newMin;
	}

	private static Vector3[] calcVerts (Voronoi region)
	{
		List<Site> sites = region.Sites ()._sites;
		Vector3[] vertices = new Vector3[sites.Count];
		int idx = 0;
		foreach (Site s in sites) {
			vertices [idx++] = new Vector3 (s.x, s.y, 0);
		}
		return vertices;
	}

	private static int[] calcTriangles (Voronoi region)
	{
		//calculate unity triangles
		int[] triangles = new int[region.Triangles ().Count * 3];

		List<Site> sites = region.Sites ()._sites;
		int idx = 0;
		foreach (Triangle t in region.Triangles()) {
			triangles [idx++] = sites.IndexOf (t.sites [0]);
			triangles [idx++] = sites.IndexOf (t.sites [1]);
			triangles [idx++] = sites.IndexOf (t.sites [2]);
		}
		return triangles;
	}

	private static Vector2[] calcPolyColliderPoints (List<Vector2> points, Vector2 offset)
	{
		Vector2[] result = new Vector2[points.Count];
		for (int i = 0; i < points.Count; i++) {
			result [i] = points [i] + offset;
		}
		return result;
	}

	/// <summary>
	/// calculates the distance between the targets pivot and it's actual center
	/// </summary>
	/// <param name="target">target gameobject to do the calculation on</param>
	/// <returns>distance between center and pivot</returns>
	private static Vector3 calcPivotCenterDiff (GameObject target)
	{
		Mesh uMesh = target.GetComponent<MeshFilter> ().sharedMesh;
		Vector3[] vertices = uMesh.vertices;

		Vector3 sum = new Vector3 ();

		for (int i = 0; i < vertices.Length; i++) {
			sum += vertices [i];
		}
		Vector3 triCenter = sum / vertices.Length;
		Vector3 pivot = target.transform.InverseTransformPoint (target.transform.position);
		return pivot - triCenter;
	}

	/// <summary>
	/// Sets the pivot of the target object to it's center
	/// </summary>
	/// <param name="target">Target Gameobject</param>
	/// <param name="diff">the distance from pivot to center</param>
	private static void centerMeshPivot (GameObject target, Vector3 diff)
	{
		//initialize mesh and vertices variables from source
		Mesh uMesh = target.GetComponent<MeshFilter> ().sharedMesh;
		Vector3[] vertices = uMesh.vertices;

		//calculate adjusted vertices
		for (int i = 0; i < vertices.Length; i++) {
			vertices [i] += diff;
		}
		//set adjusted vertices
		uMesh.vertices = vertices;

		//calculate and assign adjusted trasnsform position
		Vector3 pivot = target.transform.InverseTransformPoint (target.transform.position);
		target.transform.localPosition = target.transform.TransformPoint (pivot - diff);
        
	}

	public static void calcNormals(GameObject target) {
		//initialize mesh and vertices variables from source
		Mesh uMesh = target.GetComponent<MeshFilter> ().sharedMesh;
		Vector3[] vertices = uMesh.vertices;
		Vector3[] normals = new Vector3[vertices.Length];

		//calculate adjusted vertices
		for (int i = 0; i < vertices.Length; i++) {
			Vector2 dir = vertices[i];
			dir.Normalize();
			var normal = new Vector3(dir.x, dir.y, -0.1f);
			normal.Normalize();
			normals [i] = normal;
		}
		//set adjusted vertices
		uMesh.normals = normals;

	}

	public static void calcOutlineDirToColor(GameObject target) {
				//initialize mesh and vertices variables from source
		Mesh uMesh = target.GetComponent<MeshFilter> ().sharedMesh;
		Vector3[] vertices = uMesh.vertices;
		Color[] colors = new Color[vertices.Length];

		//calculate adjusted vertices
		for (int i = 0; i < vertices.Length; i++) {
			Vector2 dir = vertices[i];
			dir.Normalize();
			colors [i] = new Vector4(dir.x, dir.y, 0, 0);
		}
		//set adjusted vertices
		uMesh.colors = colors;

	}

	/// <summary>
	/// assigns a new material for a fragment
	/// </summary>
	/// <param name="newSprite">sprite of the fragment</param>
	/// <param name="source">original gameobject that was shattered</param>
	private static void setFragmentMaterial (GameObject newSprite, GameObject source)
	{
        
		//Material mat = new Material(Shader.Find("Sprites/Default"));
		Material mat = new Material (source.GetComponent<Renderer> ().sharedMaterial);
        
		SpriteRenderer sRend = source.GetComponent<SpriteRenderer> ();
		if (sRend != null) {
			mat.SetTexture ("_MainTex", sRend.sprite.texture);
			mat.color = sRend.color;
		} else {
			mat = source.GetComponent<MeshRenderer> ().sharedMaterial;
		}
		newSprite.GetComponent<MeshRenderer> ().sharedMaterial = mat;
	}

	private static Material createFragmentMaterial (GameObject source)
	{
		SpriteRenderer sRend = source.GetComponent<SpriteRenderer> ();
		if (sRend != null) {
			Material mat = new Material (sRend.sharedMaterial);
			mat.SetTexture ("_MainTex", sRend.sprite.texture);
			mat.color = sRend.color;
			return mat;
		} else {
			return source.GetComponent<MeshRenderer> ().sharedMaterial;
		}

	}

	public static PointsResult getPoints (GameObject source, int extraPoints = 0)
	{
		//get transform information
		Vector3 origScale = source.transform.localScale;
		source.transform.localScale = Vector3.one;
		Quaternion origRotation = source.transform.localRotation;
		source.transform.localRotation = Quaternion.identity;

		//get collider information
		PolygonCollider2D sourcePolyCollider = source.GetComponent<PolygonCollider2D> ();
		BoxCollider2D sourceBoxCollider = source.GetComponent<BoxCollider2D> ();
		Collider2D sourceCollider = null;
		List<Point> points = new List<Point> ();
		List<Vector2> borderPoints = new List<Vector2> ();
		if (sourcePolyCollider != null) {
			borderPoints = getPoints (sourcePolyCollider);
			sourceCollider = sourcePolyCollider;
		} else if (sourceBoxCollider != null) {
			borderPoints = getPoints (sourceBoxCollider);
			sourceCollider = sourceBoxCollider;
		}

		Rect rect = getRect (source);

		float ratio = Mathf.Sqrt (3) / 2;
		float n = Mathf.Sqrt (Mathf.Max (extraPoints / ratio, 100));
		int rows = Mathf.RoundToInt (n);
		float rowHeight = rect.height / rows;
		int columns = Mathf.RoundToInt (n * ratio);
		float columnWidth = rect.width / columns;

		float x0 = rect.width / -2 + rect.center.x + columnWidth / 2;
		float y0 = rect.height / -2 + rect.center.y + rowHeight / 2;

		float x, y, xOffset;
		float randomVariation = 0.4f;
		Vector2 point;
		for (int i = 0; i < rows; i++) {
			y = y0 + i * rowHeight;
			xOffset = i % 2 == 0 ? -columnWidth / 4 : columnWidth / 4;
			for (int j = 0; j < columns; j++) {
				x = x0 + j * columnWidth + xOffset;
				point = new Vector2 (
					Random.Range (randomVariation * columnWidth / -2 + x, randomVariation * columnWidth / 2 + x), 
					Random.Range (randomVariation * rowHeight / -2 + y, randomVariation * rowHeight / 2 + y)
				);
				if (sourceCollider != null && sourceCollider.OverlapPoint (point)) {
					points.Add (new Point (point, i, j));
				}
			}
		}

		//reset transform information
		source.transform.localScale = origScale;
		source.transform.localRotation = origRotation;

		PointsResult result = new PointsResult (points, borderPoints, rows, columns);
		result.GenerateMap (rect);
		return result;
	}

	public class Point
	{
		public Vector2 point;
		public int row;
		public int column;

		public Point (Vector2 point, int row, int column)
		{
			this.point = point;
			this.row = row;
			this.column = column;
		}
	}

	public class PointsResult
	{
		public List<Point> points;
		public List<Vector2> borderPoints;
		public int rows;
		public int columns;
		public int[,] map;
		public string seed = "";
		public bool useRandomSeed = true;
		public int randomFillPercent = 50;

		public PointsResult (List<Point> points, List<Vector2> borderPoints, int rows, int columns)
		{
			this.points = points;
			this.borderPoints = borderPoints;
			this.rows = rows;
			this.columns = columns;
		}

		public bool isWall (Point point)
		{
			return map != null && point.column < columns && point.row < rows && map [point.column, point.row] > 0;
		}

		public void GenerateMap (Rect rect)
		{
			map = new int[columns, rows];
			RandomFillMap (rect);

			for (int i = 0; i < 1; i++) {
				//SmoothMap ();
			}
		}


		void RandomFillMap (Rect rect)
		{
			if (useRandomSeed) {
				seed = Time.time.ToString ();
			}

			System.Random pseudoRandom = new System.Random (seed.GetHashCode ());

			float rowHeight = rect.height / rows;
			float columnWidth = rect.width / columns;

			float x0 = rect.width / -2 + rect.center.x + columnWidth / 2;
			float y0 = rect.height / -2 + rect.center.y + rowHeight / 2;

			Vector2 wave1Vector = Random.insideUnitCircle.normalized;
			Vector2 wave2Vector = Random.insideUnitCircle.normalized;

			float x, y, xOffset;
			for (int i = 0; i < rows; i++) {
				y = y0 + i * rowHeight;
				xOffset = i % 2 == 0 ? -columnWidth / 4 : columnWidth / 4;
				for (int j = 0; j < columns; j++) {
					x = x0 + j * columnWidth + xOffset;
					Vector2 point = new Vector2 (x, y);
					float magnitude = point.magnitude;
					float wave1 = 80 + Mathf.Sin (4 * Mathf.Deg2Rad * Vector2.Angle (wave1Vector, point)) * 20f;
					float wave2 = 80 + Mathf.Sin (2 * Mathf.Deg2Rad * Vector2.Angle (wave2Vector, point)) * 30f;
					bool isCell = magnitude > 130;
					bool isMiddle = Mathf.Abs(magnitude - wave1) < 7 || Mathf.Abs(magnitude - wave2) < 5;
					bool isUnderWater = magnitude < 30;
					if (isCell || isUnderWater) {
						map [j, i] = 1;
					} else if (isMiddle) {
						map [j, i] = 0;
					} else {
						map[j,i] = (pseudoRandom.Next (0, 100) < 65) ? 1 : 0;
					}
				}
			}
		}

		void SmoothMap ()
		{
			for (int x = 0; x < columns; x++) {
				for (int y = 0; y < rows; y++) {
					int neighbourWallTiles = GetSurroundingWallCount (x, y);

					if (neighbourWallTiles > 5)
						map [x, y] = 1;
					else if (neighbourWallTiles < 3)
						map [x, y] = 0;

				}
			}
		}

		int GetSurroundingWallCount (int gridX, int gridY)
		{
			int wallCount = 0;
			for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
				for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {
					if (neighbourX >= 0 && neighbourX < columns && neighbourY >= 0 && neighbourY < rows) {
						if (neighbourX != gridX || neighbourY != gridY) {
							wallCount += map [neighbourX, neighbourY];
						}
					} else {
						wallCount++;
					}
				}
			}

			return wallCount;
		}
	}
}
