using UnityEngine;
using System;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class BloomEffect : MonoBehaviour {

	[Range(0, 10)]
	public float threshold = 1;
	public Shader bloomShader;
	public bool debug;

	[NonSerialized]
	Material bloom;
	RenderTexture[] textures = new RenderTexture[16];
	int currentWidth = 0;
	int iterations = 1;

	const int BoxDownPrefilterPass = 0;
	const int BoxDownPass = 1;
	const int BoxUpPass = 2;
	const int ApplyBloomPass = 3;
	const int DebugBloomPass = 4;

	void OnRenderImage (RenderTexture source, RenderTexture destination) {
		if (bloom == null) {
			bloom = new Material(bloomShader);
			bloom.hideFlags = HideFlags.HideAndDontSave;
		}
		bloom.SetFloat("_Threshold", threshold);

		if (currentWidth != source.width) {
			currentWidth = source.width;
			iterations = Mathf.RoundToInt(Mathf.Clamp(Mathf.Log(currentWidth)/Mathf.Log(2) - 5, 1f, 16f));
			Debug.Log("Iterations " + iterations);
		}
		int width = source.width / 2;
		int height = source.height / 2;
		RenderTextureFormat format = source.format;
		RenderTexture currentDestination = textures[0] =
			RenderTexture.GetTemporary(width, height, 0, format);
			
		Graphics.Blit(source, currentDestination, bloom, BoxDownPrefilterPass);
		RenderTexture currentSource = currentDestination;
		
		int i = 1;
		for (; i < iterations; i++) {
			width /= 2;
			height /= 2;
			if (height < 2 || width < 2) {
				break;
			}
			currentDestination = textures[i] =
				RenderTexture.GetTemporary(width, height, 0, format);
			Graphics.Blit(currentSource, currentDestination, bloom, BoxDownPass);
			currentSource = currentDestination;
		}

		for (i -= 2; i >= 0; i--) {
			currentDestination = textures[i];
			textures[i] = null;
			Graphics.Blit(currentSource, currentDestination, bloom, BoxUpPass);
			RenderTexture.ReleaseTemporary(currentSource);
			currentSource = currentDestination;
		}

		if (debug) {
			Graphics.Blit(currentSource, destination, bloom, DebugBloomPass);
		}
		else {
			bloom.SetTexture("_SourceTex", source);
			Graphics.Blit(currentSource, destination, bloom, ApplyBloomPass);
		}
		RenderTexture.ReleaseTemporary(currentSource);
	}
}