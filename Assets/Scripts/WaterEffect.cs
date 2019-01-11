using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class WaterEffect : MonoBehaviour
{
	public FluidSystem fluidSystem;
	public RenderTexture renderTexture;
	public RenderTexture steamRenderTexture;
	public RenderTexture fireRenderTexture;

	private void Start() {
		Camera camera = GetComponent<Camera>();

		renderTexture = new RenderTexture(camera.pixelWidth / 2, camera.pixelHeight / 2, 0);
		RenderTargetIdentifier rtID = new RenderTargetIdentifier(renderTexture);

		steamRenderTexture = new RenderTexture(camera.pixelWidth / 2, camera.pixelHeight / 2, 0);
		RenderTargetIdentifier steamRtID = new RenderTargetIdentifier(steamRenderTexture);

		fireRenderTexture = new RenderTexture(camera.pixelWidth / 2, camera.pixelHeight / 2, 0, RenderTextureFormat.ARGBHalf);
		RenderTargetIdentifier fireRtID = new RenderTargetIdentifier(fireRenderTexture);

		CommandBuffer commandBuffer = new CommandBuffer();
		commandBuffer.name = "Draw Renderer";

		commandBuffer.SetRenderTarget(rtID);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);
		fluidSystem.Render(commandBuffer);
		commandBuffer.SetGlobalTexture(Shader.PropertyToID("Watermap_RT"), renderTexture);

		commandBuffer.SetRenderTarget(steamRtID);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);
		fluidSystem.RenderSteam(commandBuffer);
		commandBuffer.SetGlobalTexture(Shader.PropertyToID("Steammap_RT"), steamRenderTexture);

		commandBuffer.SetRenderTarget(fireRtID);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);
		fluidSystem.RenderFire(commandBuffer);
		commandBuffer.SetGlobalTexture(Shader.PropertyToID("Firemap_RT"), fireRenderTexture);

		camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
	}
}
