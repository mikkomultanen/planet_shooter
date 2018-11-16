using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class WaterEffect : MonoBehaviour
{
	public Material mat;
	public Camera waterCamera;
	private Camera _camera;
	private RenderTexture waterTexture;

	private void Start() {
		_camera = GetComponent<Camera>();
		waterTexture = new RenderTexture(_camera.scaledPixelWidth, _camera.scaledPixelHeight, 24);
		waterCamera.targetTexture = waterTexture;
	}
	private void OnRenderImage(RenderTexture src, RenderTexture dest) {
		Graphics.Blit(src, dest);
		Graphics.Blit(waterTexture, dest, mat);
	}
}
