using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class WaterEffect : MonoBehaviour
{
	public FluidSystem fluidSystem;
	public RenderTexture renderTexture;

	private void Start() {
		Camera camera = GetComponent<Camera>();

		renderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
		RenderTargetIdentifier rtID = new RenderTargetIdentifier(renderTexture);

		CommandBuffer commandBuffer = new CommandBuffer();
		commandBuffer.name = "Draw Renderer";

		commandBuffer.SetRenderTarget(rtID);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);
		fluidSystem.Render(commandBuffer);
		commandBuffer.SetGlobalTexture(Shader.PropertyToID("Watermap_RT"), renderTexture);

		camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
	}
}
