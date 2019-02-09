﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Vector2EqualComparer : IEqualityComparer<Vector2>
{
    public static Vector2EqualComparer Instance = new Vector2EqualComparer();

    public bool Equals(Vector2 x, Vector2 y)
    {
        return (x - y).sqrMagnitude < 0.01f;
    }
    public int GetHashCode(Vector2 obj)
    {
        return new Vector2Int(Mathf.RoundToInt(obj.x), Mathf.RoundToInt(obj.y)).GetHashCode();
    }
}

[System.Serializable]
public struct TerrainMaterials {
    public Material background;
    public Material pieceBackground;
    public Material piece;
    public Gradient tintColorU;
    public Gradient tintColorV;
}

public class TerrainMesh : MonoBehaviour
{
    public MeshFilter caveBackground;
    public MeshFilter background;
    public TerrainPiece terrainPieceTemplate;
    public List<TerrainMaterials> terrainMaterials;
    public float outerRadius = 125;
    public float innerRadius = 35;
    [Range(0,10)]
    public float insideOffset = 3;
    public float insideInnerRadius = 70;
    private Gradient tintColorU;
    private Gradient tintColorV;
    private const float textureScaleU = 6;
    private const float textureScaleV = 1;
    private Material material;
    private Texture2D mainTex;
    private Vector2 mainTexOffset;
    private Vector2 mainTexScale;
    private Texture2D overlayTex;
    private Vector2 overlayTexOffset;
    private Vector2 overlayTexScale;
    //private Color tintColor;
    private float brightness;

    public List<GameObject> fragments = new List<GameObject>();

    private ICaveSystem caveSystem;
    private List<PSEdge> caveCeilingEdges = new List<PSEdge>();
    private List<PSEdge> caveFloorEdges = new List<PSEdge>();
    private List<List<Vector2>> caveInsidePoints = new List<List<Vector2>>();
    private bool _ready = false;
    public bool Ready { get { return _ready; }}
#if UNITY_EDITOR
    private List<PSPolygon> editorPolygons = new List<PSPolygon>();
    private List<PSEdge> editorEdges = new List<PSEdge>();
    private List<Vector2> editorPoints = new List<Vector2>();
#endif

    private void Awake() {
        caveSystem = new SimplexCaveSystem(outerRadius, innerRadius, Random.Range(0, 0x7fffffff));
    }

    private void Start()
    {
        InitMaterial();
        if (fragments.Count == 0)
        {
            GenerateTerrain();
        }
    }

    public PSEdge randomCaveCeiling()
    {
        if(caveCeilingEdges.Count > 0) {
            return caveCeilingEdges[Random.Range(0, caveCeilingEdges.Count)];
        }
        return null;
    }

    public Vector2 RandomPositionInsideCave()
    {
        if (caveInsidePoints.Count > 0) {
            var list = caveInsidePoints[Random.Range(0, caveInsidePoints.Count)];
            return list[Random.Range(0, list.Count)];
        }
        return Vector2.zero;
    }

    public Vector2[] startPositions(int playerCount)
    {
        Vector2[] result = new Vector2[playerCount];
        var columnCount = caveInsidePoints.Count;
        if (columnCount > 0) {
            int step = columnCount / playerCount;
            int phase = Random.Range(0, columnCount);
            for (int i = 0; i < playerCount; i++)
            {
                var index = (i * step + phase) % columnCount;
                var list = caveInsidePoints[index];
                result[i] = list[Random.Range(0, list.Count)];
            }
        }
        return result;
    }

    private void InitMaterial()
    {
        if (material == null)
        {
            var terrainMaterial = terrainMaterials[Random.Range(0, terrainMaterials.Count)];
            terrainPieceTemplate.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial.piece;
            caveBackground.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial.background;
            background.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial.pieceBackground;
            tintColorU = terrainMaterial.tintColorU;
            tintColorV = terrainMaterial.tintColorV;
            material = terrainMaterial.piece;
            mainTex = material.GetTexture("_MainTex") as Texture2D;
            mainTexOffset = material.GetTextureOffset("_MainTex");
            mainTexScale = material.GetTextureScale("_MainTex");
            overlayTex = material.GetTexture("_OverlayTex") as Texture2D;
            overlayTexOffset = material.GetTextureOffset("_OverlayTex");
            overlayTexScale = material.GetTextureScale("_OverlayTex");
            //tintColor = material.GetColor("_Color");
            brightness = material.GetFloat("_Brightness");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate terrain")]
#endif
    private void GenerateTerrain()
    {
        DeleteTerrain();
        int r = Mathf.RoundToInt(outerRadius + 1);

        Debug.Log("GenerateTerrain start");
        var points = GeneratePoints(r);
        Debug.Log("GenerateTerrain points done");

        List<Vector2> vertices;
        List<int> triangles;
        GenerateTerrainTriangles(points, out vertices, out triangles);
        Debug.Log("GenerateTerrain triangles done");

        var polygons = GeneratePolygons(vertices, triangles, r);
        Debug.Log("GenerateTerrain polygons done");
        caveCeilingEdges = polygons.SelectMany(p => getCeilingEdges(p, innerRadius)).ToList();
        Debug.Log("GenerateTerrain caveCeilingEdges done");
        caveFloorEdges = polygons.SelectMany(getFloorEdges).ToList();
        Debug.Log("GenerateTerrain caveFloorEdges done");
        caveInsidePoints = GenerateInsidePoints(polygons, r);
        Debug.Log("GenerateTerrain caveInsidePoints done");

#if UNITY_EDITOR
        editorPoints = caveInsidePoints.SelectMany(l => l).ToList();
        //editorPolygons = polygons;
        //editorEdges = ;
#endif

        GenerateFragments(polygons);
        Debug.Log("GenerateTerrain fragments done");
        UpdateBackground();
        Debug.Log("GenerateTerrain background done");
        UpdateCaveBackground(polygons);
        Debug.Log("GenerateTerrain cave background done");
        _ready = true;
    }

    private Vector2 findBorder(Vector2 v0, bool b0, Vector2 v1, bool b1)
    {
        Vector2 inV = b0 ? v0 : v1;
        Vector2 outV = b0 ? v1 : v0;
        Vector2 middleV;
        int iterations = 0;
        do
        {
            middleV = (inV + outV) * 0.5f;
            if (caveSystem.insideCave(middleV))
            {
                inV = middleV;
            }
            else
            {
                outV = middleV;
            }
            iterations++;
        } while ((inV - outV).sqrMagnitude > 0.0001f && iterations < 10);
        return (inV + outV) * 0.5f;
    }


    public static Contour createContour(IEnumerable<Vector2> points)
    {
        return new Contour(points.Select(toVertex));
    }

    private static void addBorderPoint(List<Vector2> coords, Vector2 coord)
    {
        var oldIndex = indexOf(coords, coord);
        if (oldIndex < 0)
        {
            coords.Add(coord);
        }
        else if (coord.x == 0 || coord.y == 0)
        {
            coords[oldIndex] = coord;
        }
    }

    public static int indexOf(List<Vector2> coords, Vector2 coord)
    {
        for (int i = 0; i < coords.Count; i++)
        {
            if ((coords[i] - coord).sqrMagnitude < 0.01)
            {
                return i;
            }
        }
        return -1;
    }

    public static Vertex toVertex(Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }
    public static Vector2 toVector2(Vertex vertex)
    {
        return new Vector2((float)vertex.X, (float)vertex.Y);
    }

    private List<Vector2> GeneratePoints(int r)
    {
        var allPoints = new ConcurrentBag<Vector2>();
        Parallel.For(-r, r, x => {
            Vector2 v00, v10, v01, v11;
            bool b00, b10, b01, b11;
            for (int y = -r; y < r; y++)
            {
                v00 = new Vector2(x, y);
                v10 = new Vector2(x + 1, y);
                v01 = new Vector2(x, y + 1);
                v11 = new Vector2(x + 1, y + 1);
                b00 = caveSystem.insideCave(v00);
                b10 = caveSystem.insideCave(v10);
                b01 = caveSystem.insideCave(v01);
                b11 = caveSystem.insideCave(v11);

                if (b00 != b10)
                {
                    allPoints.Add(findBorder(v00, b00, v10, b10));
                }
                if (b00 != b01)
                {
                    allPoints.Add(findBorder(v00, b00, v01, b01));
                }
                if (b11 != b10)
                {
                    allPoints.Add(findBorder(v11, b11, v10, b10));
                }
                if (b11 != b01)
                {
                    allPoints.Add(findBorder(v11, b11, v01, b01));
                }
            }
        });

        return allPoints.Distinct(Vector2EqualComparer.Instance).ToList();
    }
    private void GenerateTerrainTriangles(List<Vector2> points, out List<Vector2> vertices, out List<int> triangles)
    {
        var imesh = (new GenericMesher().Triangulate(points.Select(toVertex).ToList()));
        vertices = new List<Vector2>();
        triangles = new List<int>();
        foreach (var triangle in imesh.Triangles)
        {
            var list = triangle.vertices.Select(toVector2).Reverse().ToList();
            if (!caveSystem.insideCave(PSPolygon.GetCenter(list)))
            {
                foreach (var v in list)
                {
                    var index = indexOf(vertices, v);
                    if (index < 0)
                    {
                        index = vertices.Count;
                        vertices.Add(v);
                    }
                    triangles.Add(index);
                }
            }
        }
    }

    private List<PSPolygon> GeneratePolygons(List<Vector2> vertices, List<int> triangles, int r)
    {
        var contours = MeshToPolygonConverter.ContourPolygons(vertices, triangles).ToList();
        var segments = new Vector2[][]{
            new Vector2[]{new Vector2(0,0), new Vector2(-r,0), new Vector2(-r,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(-r,r), new Vector2(0,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(0,r), new Vector2(r,r)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,r), new Vector2(r,0)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,0), new Vector2(r,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(r,-r), new Vector2(0,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(0,-r), new Vector2(-r,-r)},
            new Vector2[]{new Vector2(0,0), new Vector2(-r,-r), new Vector2(-r,0)}
        };
        return segments.AsParallel().SelectMany(segment => MeshToPolygonConverter.FragmentPolygons(contours, segment)).ToList();
    }

    private List<List<Vector2>> GenerateInsidePoints(IEnumerable<PSPolygon> polygons, int r)
    {
        var area = new Vector2[]{
            new Vector2(r,r), new Vector2(r,-r), new Vector2(-r,-r), new Vector2(-r,r)
        };
        var insidePolygons = MeshToPolygonConverter.InsidePolygons(area, polygons, -insideOffset).ToList();
        insidePolygons.ForEach(p => p.Precalc());
        var insideLookup = insidePolygons.ToLookup(p => p.IsHole);
        var magnitudeStep = 1f;
        var magnitudeCount = Mathf.CeilToInt((outerRadius - insideInnerRadius) / magnitudeStep);
        return Enumerable.Range(0, 360).Select(i => {
            var angle = i * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            return Enumerable.Range(0, magnitudeCount).Select(j => direction * (insideInnerRadius + j * magnitudeStep)).Where(p => {
                return insideLookup[false].Any(c => c.PointInPolygon(p)) && insideLookup[true].All(c => !c.PointInPolygon(p));
            }).ToList();
        }).Where(l => l.Count > 0).ToList();
    }

    private static List<PSEdge> getFloorEdges(PSPolygon polygon)
    {
        var floorEdges = new List<PSEdge>();
        Vector2 previous = polygon.points.Last();
        bool previousIsFloor = isFloorPoint(previous, polygon);
        bool currentIsFloor;
        foreach (var p in polygon.points)
        {
            currentIsFloor = isFloorPoint(p, polygon);
            if ((currentIsFloor || previousIsFloor) && Vector3.Cross(previous, p).sqrMagnitude > 0)
            {
                floorEdges.Add(new PSEdge(previous, p));
            }
            previous = p;
            previousIsFloor = currentIsFloor;
        }
        return floorEdges;
    }

    private static List<PSEdge> getCeilingEdges(PSPolygon polygon, float innerRadius)
    {
        var ceilingEdges = new List<PSEdge>();
        Vector2 previous = polygon.points.Last();
        bool previousIsCeiling = isCeilingPoint(previous, polygon, innerRadius);
        bool currentIsCeiling;
        foreach (var p in polygon.points)
        {
            currentIsCeiling = isCeilingPoint(p, polygon, innerRadius);
            if ((currentIsCeiling || previousIsCeiling) && Vector3.Cross(previous, p).sqrMagnitude > 0)
            {
                ceilingEdges.Add(new PSEdge(previous, p));
            }
            previous = p;
            previousIsCeiling = currentIsCeiling;
        }
        return ceilingEdges;
    }

    private static bool isFloorPoint(Vector2 point, PSPolygon polygon)
    {
        return polygon.PointInPolygon(point + (point.normalized * -0.005f));
    }

    private static bool isCeilingPoint(Vector2 point, PSPolygon polygon, float innerRadius)
    {
        return point.magnitude > innerRadius + 0.005f && polygon.PointInPolygon(point + (point.normalized * 0.005f));
    }

    private float tangent(Vector2 point)
    {
        var magnitude = point.magnitude;
        var direction = point.normalized;
        var d = outerRadius - innerRadius;
        var distanceToFloor = caveFloorEdges.Select(e => magnitude - e.IntersectMagnitude(direction)).Where(m => m > -0.1f).DefaultIfEmpty(d).Min();
        var distanceToCeiling = caveCeilingEdges.Select(e => e.IntersectMagnitude(direction) - magnitude).Where(m => m > -0.1f).DefaultIfEmpty(d).Min();
        return (1 - Mathf.Clamp01(distanceToFloor / 3f)) - (1 - Mathf.Clamp01(distanceToCeiling / 3f));
    }

    public Color terrainTintColor(Vector2 point, bool doNotWrap)
    {
        var angle = Mathf.Atan2(point.x, point.y);
        if (doNotWrap && angle > 0)
        {
            angle = angle - 2 * Mathf.PI;
        }
        var magnitude = point.magnitude;
        float u = angle / (2 * Mathf.PI);
        if (u < 0f) u += 1f;
        float v = Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius));
        if (tintColorU != null && tintColorV != null)
        {
            return Color.Lerp(tintColorU.Evaluate(u), tintColorV.Evaluate(v), 0.5f);
        }
        if (tintColorU != null)
        {
            return tintColorU.Evaluate(u);
        }
        if (tintColorV != null)
        {
            return tintColorV.Evaluate(v);
        }
        return Color.white;
    }

    private Color backgroundTintColor(Vector2 point, bool doNotWrap)
    {
        var color = terrainTintColor(point, doNotWrap) * 0.5f;
        color.a = 1f;
        return color;
    }

    private Color caveBackgroundTintColor(Vector2 point, bool doNotWrap)
    {
        var value = 1f - 0.5f * Mathf.Clamp01(0.5f - 0.5f * tangent(point));
        var color = terrainTintColor(point, doNotWrap) * value;
        color.a = 1f;
        return color;
    }

    public Vector2 getUV(Vector2 coord, bool doNotWrap)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        if (doNotWrap && angle > 0)
        {
            angle = angle - 2 * Mathf.PI;
        }
        var magnitude = coord.magnitude;
        float u = angle / (2 * Mathf.PI) * textureScaleU;
        float v = Mathf.Clamp01((magnitude - innerRadius) / (outerRadius - innerRadius)) * textureScaleV;
        return new Vector2(u, v);
    }

    public Vector2 getUV2(Vector2 coord, bool doNotWrap, List<PSEdge> floorEdges)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        if (doNotWrap && angle > 0)
        {
            angle = angle - 2 * Mathf.PI;
        }
        var magnitude = coord.magnitude;
        float u = angle / (2 * Mathf.PI) * textureScaleU * 10;
        var direction = coord.normalized;
        var floorMagnitude = floorEdges.Select(e => e.IntersectMagnitude(direction) - magnitude).Where(m => m > -0.1f).DefaultIfEmpty(0f).Min();
        var d = outerRadius - innerRadius;
        var v = Mathf.Clamp(floorMagnitude, 0, d) / d * textureScaleV * 10;
        return new Vector2(u, v);
    }

    public Vector2 getMainTexUV(Vector2 coord, bool doNotWrap)
    {
        var uv = getUV(coord, doNotWrap);
        return new Vector2(uv.x * mainTexScale.x, uv.y * mainTexScale.y) + mainTexOffset;
    }

    public Vector2 getOverlayTexUV(Vector2 coord, bool doNotWrap, List<PSEdge> floorEdges)
    {
        var uv = getUV2(coord, doNotWrap, floorEdges);
        return new Vector2(uv.x * overlayTexScale.x, uv.y * overlayTexScale.y) + overlayTexOffset;
    }

    public Color32 getColor(Vector2 mainTexUV, Vector2 overlayTexUV, Color tintColor)
    {
        var color = mainTex.GetPixelBilinear(mainTexUV.x, mainTexUV.y).linear;
        color = color * tintColor * brightness;
        color.a = 1f;
        var overlayColor = overlayTex.GetPixelBilinear(overlayTexUV.x, overlayTexUV.y).linear;
        var a = overlayColor.a;
        overlayColor.a = 1f;
        return Color.Lerp(color, overlayColor, a);
    }

#if UNITY_EDITOR
    [ContextMenu("Delete terrain")]
#endif
    private void DeleteTerrain()
    {
        _ready = false;
        foreach (GameObject frag in fragments)
        {
#if UNITY_EDITOR
            DestroyImmediate(frag);
#else
			Destroy (frag);
#endif
        }
        fragments.Clear();
        caveCeilingEdges.Clear();
        UpdateBackground();
        UpdateCaveBackground(new PSPolygon[0]);
#if UNITY_EDITOR
        editorPolygons.Clear();
        editorEdges.Clear();
        editorPoints.Clear();
#endif
    }
    private void GenerateFragments(List<PSPolygon> polygons)
    {
        fragments.AddRange(polygons.Select(p => GenerateFragment(p)));
        Resources.UnloadUnusedAssets();
    }

    private GameObject GenerateFragment(PSPolygon polygon)
    {
        TerrainPiece piece = Instantiate(terrainPieceTemplate, gameObject.transform.position, gameObject.transform.rotation);

        MeshFilter meshFilter = piece.GetComponent<MeshFilter>();

        Mesh uMesh = meshFilter.sharedMesh;
        if (uMesh == null)
        {
            meshFilter.mesh = new Mesh();
            uMesh = meshFilter.sharedMesh;
        }

        var poly = new Polygon();
        poly.Add(TerrainMesh.createContour(polygon.points));
        var quality = new QualityOptions();
        quality.MinimumAngle = 36;
        quality.MaximumAngle = 91;
        var imesh = poly.Triangulate(quality);
        var meshVectices = imesh.Triangles.SelectMany(t => t.vertices.Select(TerrainMesh.toVector2).Reverse());

        var vertices = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var v in meshVectices)
        {
            var index = TerrainMesh.indexOf(vertices, v);
            if (index < 0)
            {
                index = vertices.Count;
                vertices.Add(v);
            }
            triangles.Add(index);
        }

        var polygonBoundsCenter = polygon.Bounds.center;
        var doNotWrapUV = polygonBoundsCenter.x < 0 && polygonBoundsCenter.y < 0;
        var floorEdges = getFloorEdges(polygon);

        uMesh.vertices = vertices.Select(p => new Vector3(p.x, p.y, 0)).ToArray();
        uMesh.colors = vertices.Select(p => terrainTintColor(p, doNotWrapUV)).ToArray();
        uMesh.uv = vertices.Select(v => getUV(v, doNotWrapUV)).ToArray();
        uMesh.uv2 = vertices.Select(v => getUV2(v, doNotWrapUV, floorEdges)).ToArray();
        uMesh.triangles = triangles.ToArray();
        uMesh.RecalculateNormals();
        uMesh.RecalculateBounds();

        meshFilter.mesh = uMesh;

        PolygonCollider2D collider = piece.gameObject.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, polygon.points);

        piece.terrainMesh = this;
        piece.doNotWrapUV = doNotWrapUV;
        piece.floorEdges = floorEdges;

        piece.gameObject.SetActive(true);
        return piece.gameObject;
    }

    private void UpdateBackground()
    {
        var meshes = fragments.Select(piece => piece.GetComponent<MeshFilter>().sharedMesh);
        var verticesCount = meshes.Aggregate(0, (total, mesh) => total + mesh.vertices.Length);
        var trianglesCount = meshes.Aggregate(0, (total, mesh) => total + mesh.triangles.Length);
        var vertices = new List<Vector3>(verticesCount);
        var uv = new List<Vector2>(verticesCount);
        var triangles = new List<int>(trianglesCount);
        int currentIndex;
        foreach (var mesh in meshes)
        {
            currentIndex = vertices.Count;
            vertices.AddRange(mesh.vertices);
            uv.AddRange(mesh.uv);
            triangles.AddRange(mesh.triangles.Select(i => currentIndex + i));
        }
        UpdateMesh(background, vertices, uv, vertices.Select(v => (Color32)backgroundTintColor(v, false)), triangles);
    }

    private void UpdateCaveBackground(IEnumerable<PSPolygon> allPolygons)
    {
        var polygonSets = new List<List<PSPolygon>>();
        polygonSets.Add(allPolygons.Where(p => p.Bounds.center.x >= 0 && p.Bounds.center.y >= 0).ToList());
        polygonSets.Add(allPolygons.Where(p => p.Bounds.center.x >= 0 && p.Bounds.center.y <= 0).ToList());
        polygonSets.Add(allPolygons.Where(p => p.Bounds.center.x <= 0 && p.Bounds.center.y >= 0).ToList());
        polygonSets.Add(allPolygons.Where(p => p.Bounds.center.x <= 0 && p.Bounds.center.y <= 0).ToList());

        var vertices = new List<Vector2>();
        var uv = new List<Vector2>();
        var colors = new List<Color32>();
        var triangles = new List<int>();

        int currentIndex;
        var tempVertices = new List<Vector2>();
        foreach (var polygons in polygonSets)
        {
            if (polygons.Count == 0)
            {
                continue;
            }
            currentIndex = vertices.Count;
            var polygonBoundsCenter = polygons.First().Bounds.center;
            var doNotWrapUV = polygonBoundsCenter.x < 0 && polygonBoundsCenter.y < 0;

            var poly = new Polygon();
            foreach (var polygon in polygons)
            {
                poly.Add(createContour(polygon.points), true);
            }
            poly.Add(new Vertex());
            if (polygonBoundsCenter.x < 0) {
                poly.Add(new Vertex(-outerRadius, 0));
                if (polygonBoundsCenter.y < 0) {
                    poly.Add(new Vertex(-outerRadius, -outerRadius));
                    poly.Add(new Vertex(0, -outerRadius));
                } else {
                    poly.Add(new Vertex(-outerRadius, outerRadius));
                    poly.Add(new Vertex(0, outerRadius));
                }
            } else {
                poly.Add(new Vertex(outerRadius, 0));
                if (polygonBoundsCenter.y < 0) {
                    poly.Add(new Vertex(outerRadius, -outerRadius));
                    poly.Add(new Vertex(0, -outerRadius));
                } else {
                    poly.Add(new Vertex(outerRadius, outerRadius));
                    poly.Add(new Vertex(0, outerRadius));
                }
            }
            var constraint = new ConstraintOptions();
            constraint.Convex = true;
            var quality = new QualityOptions();
            quality.MinimumAngle = 36;
            quality.MaximumAngle = 91;
            var imesh = poly.Triangulate(constraint, quality);
            foreach (var triangle in imesh.Triangles)
            {
                var list = triangle.vertices.Select(toVector2).Reverse().ToList();
                if (list.Any(v => v.magnitude > innerRadius))
                {
                    foreach (var v in list)
                    {
                        var index = indexOf(tempVertices, v);
                        if (index < 0)
                        {
                            index = tempVertices.Count;
                            tempVertices.Add(v);
                            colors.Add(caveBackgroundTintColor(v, doNotWrapUV));
                            uv.Add(getUV(v, doNotWrapUV));
                        }
                        triangles.Add(currentIndex + index);
                    }
                }
            }
            vertices.AddRange(tempVertices);
            tempVertices.Clear();
        }

        UpdateMesh(caveBackground, vertices.Select(p => new Vector3(p.x, p.y, 0)), uv, colors, triangles);
    }

    private static void UpdateMesh(MeshFilter meshFilter, IEnumerable<Vector3> vertices, IEnumerable<Vector2> uv, IEnumerable<Color32> colors, IEnumerable<int> triangles)
    {
        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            meshFilter.mesh = new Mesh();
            mesh = meshFilter.sharedMesh;
        }
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.colors32 = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2 offset = (Vector2)transform.position * 0;

            editorPolygons.ForEach(p => {
                Gizmos.color = p.IsHole ? Color.red : Color.white;
                DrawPolygon(p, offset);
            });

            Gizmos.color = Color.cyan;
            editorEdges.ForEach(e => DrawEdge(e, offset));

            Gizmos.color = Color.white;
            DrawDiamonds(editorPoints, offset);

            Gizmos.color = Color.green;
            caveCeilingEdges.ForEach(e => DrawEdge(e, offset));

            Gizmos.color = Color.yellow;
            caveFloorEdges.ForEach(e => DrawEdge(e, offset));
        }
    }

    private void DrawPolygon(PSPolygon polygon, Vector2 offset)
    {
        for (int i = 0; i < polygon.points.Length; i++)
        {
            if (i + 1 == polygon.points.Length)
            {
                Gizmos.DrawLine(polygon.points[i] + offset, polygon.points[0] + offset);
            }
            else
            {
                Gizmos.DrawLine(polygon.points[i] + offset, polygon.points[i + 1] + offset);
            }
        }
    }

    private void DrawEdge(PSEdge edge, Vector2 offset)
    {
        var v0 = edge.v0 + offset;
        var v1 = edge.v1 + offset;
        var arrow = 0.9f * v1 + 0.1f * v0;
        var perpendicular = 0.1f * Vector2.Perpendicular(v1 - v0);
        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(arrow + perpendicular, v1);
        Gizmos.DrawLine(arrow - perpendicular, v1);
    }

    private void DrawDiamonds(IEnumerable<Vector2> points, Vector2 offset)
    {
        float diamondSize = 0.1f;
        List<Vector2> diamond = new List<Vector2>();
        diamond.Add(new Vector2(-diamondSize / transform.lossyScale.x, 0f));
        diamond.Add(new Vector2(0f, diamondSize / transform.lossyScale.y));
        diamond.Add(new Vector2(diamondSize / transform.lossyScale.x, 0f));
        diamond.Add(new Vector2(0f, -diamondSize / transform.lossyScale.y));
        foreach (Vector2 point in points)
        {
            for (int i = 0; i < diamond.Count; i++)
            {
                Vector2 diamondOffset = point + offset;
                if (i + 1 == diamond.Count)
                {
                    Gizmos.DrawLine(diamond[i] + diamondOffset, diamond[0] + diamondOffset);
                }
                else
                {
                    Gizmos.DrawLine(diamond[i] + diamondOffset, diamond[i + 1] + diamondOffset);
                }
            }
        }
    }
#endif
}
