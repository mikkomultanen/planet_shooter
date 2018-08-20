using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class ShadowEffect : MonoBehaviour
{
	//Filtering mode used, use bilinear for smooth lighting, try point for pixel-artsy style
	//Is only visibile in the shadows, not the colour gradient, might change that
	public FilterMode textureFilterMode = FilterMode.Bilinear;
	
	//Alpha cuttoff threshhold, how transparent does an object have to be until it no longer
	//casts a shadow?
	[Range(0, 1)]
	public float alphaThreshold = 0.3f;
	//Shadow color
	public Color shadowColor = Color.black;
	
	//List of all lights, this is READ ONLY
	public List<LightSource> lights;
	
	//Lightmesh used to render the lightmap
	[HideInInspector]
	public Mesh lightMesh;

	//Materials for all shaders used
	Material occlusion;
	Material lookupMat;
	Material lightMat;
	Material lightAccumulation;
	
	List<ShadowCaster2D> occluders;

	//The command buffer to use
	CommandBuffer buffer;
	
	//RTI's cause I'm lazy
	RenderTargetIdentifier lightmap;
	RenderTargetIdentifier occlusionMap;
	RenderTargetIdentifier lookupMap;
	RenderTargetIdentifier lightAreaMap;

	Camera renderingCamera;
	
	void Start()
	{
		//If no lightmesh has been created, one has to be created and set up
		if (!lightMesh)
		{
			//I'm lazy, ok? I didn't feel like creating the coordinates and UV's and that, sue me
			GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
			lightMesh = o.GetComponent<MeshFilter>().sharedMesh;
			GameObject.Destroy(o);
		}
		buffer = new CommandBuffer();

		occluders = new List<ShadowCaster2D>();

		//Populate materials with the appropriate shaders
		occlusion = new Material(Shader.Find("Hidden/2DLighting/Occlusion"));
		lookupMat = new Material(Shader.Find("Hidden/2DLighting/Distance"));
		lightMat = new Material(Shader.Find("Hidden/2DLighting/Light"));
		lightAccumulation = new Material(Shader.Find("Hidden/2DLighting/Accumulation"));

		//Create RTI's for easy reference
		lightmap = new RenderTargetIdentifier(Shader.PropertyToID("Lightmap_RT"));
		occlusionMap = new RenderTargetIdentifier(Shader.PropertyToID("Occluders_RT"));
		lookupMap = new RenderTargetIdentifier(Shader.PropertyToID("Lookup_RT"));
		lightAreaMap = new RenderTargetIdentifier(Shader.PropertyToID("LightArea_RT"));
		//Attach buffer to camera. Haven't experimented much with render order, but AfterForwardOpaque works
        renderingCamera = this.GetComponent<Camera>();
		renderingCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, buffer);
	}

	void OnDestroy()
	{
		Cleanup();
	}

	void Cleanup()
	{
		//I know, I KNOW, there's probably something to clean up here, but, whatever, it's C#
	}
	
	//We use LateUpdate to make sure all transforms and everything has been properly applied, otherwise the shadows will drag
	void LateUpdate()
	{
		//Clear all lists and repopulate them. There's performance gains to be made here.
		//For example we could make each object's transform and scripts send a callback to update their listing 
		//here, or something similar. This will be changed in the future
		occluders.Clear();
		lights.Clear();
        foreach (ShadowCaster2D obj in GameObject.FindObjectsOfType<ShadowCaster2D>())
            occluders.Add(obj);
		foreach (LightSource light in GameObject.FindObjectsOfType<LightSource>())
			lights.Add(light);

		buffer.Clear(); //Clear the buffer
		buffer.GetTemporaryRT(Shader.PropertyToID("Lightmap_RT"), renderingCamera.pixelWidth, renderingCamera.pixelHeight, 0, textureFilterMode); //Set the first shader to be used
		buffer.SetRenderTarget(lightmap); //Set the rendertarget to our lightmap texture
		buffer.ClearRenderTarget(false, true, shadowColor); //Clear the lightmap with the shadow color
		foreach (LightSource light in lights) //Loop through each light and render out their small lightmap
		{
			if (!light.on)
				continue; //Skip this light if it's not on, duh
			
            //Reset the switches for the shader
            Shader.DisableKeyword("QUALITY_CRAP");
            Shader.DisableKeyword("QUALITY_LOW");
            Shader.DisableKeyword("QUALITY_NORMAL");
            Shader.DisableKeyword("QUALITY_HIGH");
            Shader.DisableKeyword("QUALITY_HIGHER");
            Shader.DisableKeyword("QUALITY_BEST");
            
            //Set the current switch
            if(light.MapResolution == LightSource.ShadowmapResolution.Res128)
                Shader.EnableKeyword("QUALITY_CRAP");
            else if(light.MapResolution == LightSource.ShadowmapResolution.Res256)
                Shader.EnableKeyword("QUALITY_LOW");
            else if(light.MapResolution == LightSource.ShadowmapResolution.Res512)
                Shader.EnableKeyword("QUALITY_NORMAL");
            else if(light.MapResolution == LightSource.ShadowmapResolution.Res1024)
                Shader.EnableKeyword("QUALITY_HIGH");
            else if(light.MapResolution == LightSource.ShadowmapResolution.Res2048)
                Shader.EnableKeyword("QUALITY_HIGHER");
            else if(light.MapResolution == LightSource.ShadowmapResolution.Res4096)
                Shader.EnableKeyword("QUALITY_BEST");

            int ligthResolution = 2 << (int)light.MapResolution;

			//Set up the occluders map
			//NOTE: instead of using big values in the enum, we simply bit shift our 2 to get the resolution we want (basically raising 2 by the value set in the enum)
			buffer.GetTemporaryRT(Shader.PropertyToID("Occluders_RT"), ligthResolution, ligthResolution, 0, textureFilterMode);
			buffer.SetRenderTarget(occlusionMap);
			buffer.ClearRenderTarget(false, true, Color.clear);
			
			//Render occluders to occlusion map
			light.RecalculateMatrices();
            buffer.SetGlobalFloat(Shader.PropertyToID("_Alpha"), alphaThreshold);
			buffer.SetGlobalMatrix(Shader.PropertyToID("LIGHT_MVP"), light.mvp);
            buffer.SetGlobalMatrix(Shader.PropertyToID("LIGHT_PROJ"), GL.GetGPUProjectionMatrix(light.orthoProj, true));
            foreach (ShadowCaster2D occluder in occluders)
            {
				//If it's a spriterenderer, get the color assigned in the spriterenderer (alpha value is important here)
				//otherwise use the material color (again, alpha is what we are pulling this for)
                Renderer r = occluder.GetComponent<Renderer>();
                if (r as SpriteRenderer != null)
                {
                    buffer.SetGlobalColor(Shader.PropertyToID("_SpriteColor"), (r as SpriteRenderer).color);
                }
                else
                {
                    buffer.SetGlobalColor(Shader.PropertyToID("_SpriteColor"), r.material.color);
                }
				//Render to occlusion map
                buffer.DrawRenderer(r, occlusion);
			}
					
			//Render lookup map
			buffer.GetTemporaryRT(Shader.PropertyToID("Lookup_RT"), ligthResolution, 1, 0, textureFilterMode);
			buffer.SetGlobalVector(Shader.PropertyToID("_Resolution"), new Vector4(ligthResolution, ligthResolution));
			buffer.SetGlobalFloat(Shader.PropertyToID("_SampleRange"), light.angle / 360f);
			buffer.SetGlobalTexture(Shader.PropertyToID("_OccTex"), occlusionMap);
			buffer.Blit(occlusionMap, lookupMap, lookupMat, 0); //Blitting causes the occlusion map to be set as the texture input in the shader, neat
			buffer.ReleaseTemporaryRT(Shader.PropertyToID("Occluders_RT"));
			
			//Render light area
			buffer.GetTemporaryRT(Shader.PropertyToID("LightArea_RT"), ligthResolution, ligthResolution, 0, textureFilterMode);
			buffer.SetGlobalFloat(Shader.PropertyToID("_Intensity"), light.intensity);
            buffer.SetGlobalFloat(Shader.PropertyToID("_Blurring"), light.blurAmount);
			buffer.Blit(lookupMap, lightAreaMap, lightMat);
			buffer.ReleaseTemporaryRT(Shader.PropertyToID("Lookup_RT"));
			
			//Render into composite lightmap
			buffer.SetRenderTarget(lightmap);
			buffer.SetGlobalTexture(Shader.PropertyToID("LightArea_RT"), lightAreaMap);
			buffer.SetGlobalVector(Shader.PropertyToID("LightColor"), light.lightColor);
			buffer.DrawMesh(lightMesh, light.lightMvp, lightAccumulation);
			buffer.ReleaseTemporaryRT(Shader.PropertyToID("LightArea_RT"));
		}
		buffer.SetGlobalTexture(Shader.PropertyToID("Lightmap_RT"), lightmap); //Set the composite lightmap as a global texture for easy shader access
	}
}
