using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WaterColors {
    public Color outerColor;
    public Color innerColor;
}

[RequireComponent(typeof(MeshFilter))]
public class Water : MonoBehaviour
{

    public int steps = 128;
    public float outerRadius = 1;
    public float innerRadius = 0.1f;
    public List<WaterColors> waterColors;
    public ParticleSystem splashTemplate;
    public float splashThreshold = 1f;

    private float textureScaleU = 6;
    private float textureScaleV = 1;
    private Color outerColor = Color.white;
    private Color innerColor = Color.white;
    private float waterSurfaceMagnitude;

    private void Awake()
    {
        var water = GetComponent<CircleCollider2D>();
        waterSurfaceMagnitude = water.radius;
        GenerateMesh();
    }

#if UNITY_EDITOR
    [ContextMenu("Generate mesh")]
#endif
    private void GenerateMesh()
    {
        var waterColor = waterColors[Random.Range(0, waterColors.Count)];
        innerColor = waterColor.innerColor;
        outerColor = waterColor.outerColor;

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
        var colors32 = new Color32[verticesCount];

        var direction = new Vector2(1, 0);
        for (int i = 0; i < steps; i++)
        {
            var j = 2 * i;
            vertices[j] = new Vector3(direction.x, direction.y, 0) * outerRadius;
            normals[j] = vertices[2 * i].normalized;
            uv[j] = new Vector2(textureScaleU * i / steps, textureScaleV);
            colors32[j] = outerColor;
            direction = Quaternion.Euler(0, 0, -180.0f / steps) * direction;
            vertices[j + 1] = new Vector3(direction.x, direction.y, 0) * innerRadius;
            normals[j + 1] = new Vector3(0, 0, -1);
            uv[j + 1] = new Vector2(textureScaleU * (i + 0.5f) / steps, 0);
            colors32[j + 1] = innerColor;
            direction = Quaternion.Euler(0, 0, -180.0f / steps) * direction;
        }
        vertices[2 * steps] = vertices[0];
        normals[2 * steps] = normals[0];
        uv[2 * steps] = new Vector2(textureScaleU, textureScaleV);
        colors32[2 * steps] = colors32[0];
        vertices[2 * steps + 1] = vertices[1];
        normals[2 * steps + 1] = normals[1];
        uv[2 * steps + 1] = new Vector2(textureScaleU * (steps + 0.5f) / steps, 0);
        colors32[2 * steps + 1] = colors32[1];
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.colors32 = colors32;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var positionNormalized = rb.position.normalized;
            var massScale = Mathf.Clamp(rb.mass / 10f, 0.1f, 1f);
            var ySpeed = Mathf.Abs(Vector2.Dot(rb.velocity, positionNormalized)) * massScale;
            if (ySpeed > splashThreshold)
            {
                var splash = Instantiate(splashTemplate, positionNormalized * waterSurfaceMagnitude, Quaternion.Euler(0, 0, -Mathf.Atan2(rb.position.x, rb.position.y) * Mathf.Rad2Deg));
                var emission = splash.emission;
                emission.rateOverTimeMultiplier *= massScale;
                var main = splash.main;
                var startSpeed = Mathf.Clamp(ySpeed, 0, 10);
                main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.5f, startSpeed);
                splash.gameObject.SetActive(true);
            }
        }
    }

    public void explosionSplash(Vector2 position, float radius)
    {
        var magnitude = Mathf.Clamp(position.magnitude - waterSurfaceMagnitude, 0, radius);
        var scale = 1 - (magnitude / radius);
        if (scale > 0.8f)
        {
            var splash = Instantiate(splashTemplate, position.normalized * waterSurfaceMagnitude, Quaternion.Euler(0, 0, -Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg));
            var emission = splash.emission;
            emission.rateOverTimeMultiplier *= 10 * scale;
            var main = splash.main;
            var startSpeed = 10 * scale;
            main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.5f, startSpeed);
            splash.gameObject.SetActive(true);
        }
    }
}
