using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class RadarEffect : MonoBehaviour {

    public Color color = Color.green;
    public float radarDistance = 2;
    public float radarSpeed = 1;
    private Material mat;
    private Camera _camera;
    private Vector3 radarOrigin;
    private float radarCurrentDistance = 1;
    private int colorNameID;
    private int radarNameID;
    private int aspectRatioNameID;

    void Start () {
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode = DepthTextureMode.Depth;
        colorNameID = Shader.PropertyToID("_EdgeColor");
        radarNameID = Shader.PropertyToID("_Radar");
        aspectRatioNameID = Shader.PropertyToID("_AspectRatio");
        mat = new Material(Shader.Find("Hidden/PlanetShooter/Radar"));
        mat.SetFloat("_Threshold", 0.1f);
    }

    private void Update() {
        radarCurrentDistance = radarCurrentDistance + Time.deltaTime * radarSpeed;
        if (radarCurrentDistance > radarDistance) {
            radarCurrentDistance = radarCurrentDistance % radarDistance;
            radarOrigin = transform.position;
        }
    }

    void OnRenderImage (RenderTexture source, RenderTexture destination) {
        var uv = _camera.WorldToViewportPoint(radarOrigin);
        mat.SetColor(colorNameID, color);
        mat.SetVector(radarNameID, new Vector4(uv.x, uv.y, radarCurrentDistance, 1 - radarCurrentDistance / radarDistance));
        mat.SetFloat(aspectRatioNameID, _camera.aspect);
        Graphics.Blit(source,destination,mat);
    }
}
