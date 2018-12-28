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

	private void Start() {
		Camera camera = GetComponent<Camera>();

		renderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
		RenderTargetIdentifier rtID = new RenderTargetIdentifier(renderTexture);

		steamRenderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
		RenderTargetIdentifier steamRtID = new RenderTargetIdentifier(steamRenderTexture);

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

		camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
	}
}
