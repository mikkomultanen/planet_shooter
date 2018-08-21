using UnityEngine;
using System.Collections;

public class LightSource : MonoBehaviour
{
	//Resolution of shadowmap, can go higher but unless you've got a monster machine or maybe
	//deep blue I'd suggest against it. Feel free to add 8192 and experiment, just add it to the enum below
	public enum ShadowmapResolution
	{
		Res128 = 6,
		Res256 = 7,
		Res512 = 8,
		Res1024 = 9,
		Res2048 = 10,
		Res4096 = 11
	}

	//Angle of the cone of light
	[Range(0, 360)]
	public float angle = 360f;
	//Intensity (hardness) of the light
	[Range(0, 100)]
	public float intensity = 1;
	
	//Diameter of the light in unity units (the bigger the light, the higher imprecision)
	[Range(0, 100)]
	public float size = 1;
	//Color of the light
	public Color lightColor;
	//How blurry the light is (values > 2 might cause artifacts)
    public float blurAmount = 1;
	
	//The resolution of the shadowmap for each light, defaults at 256x256
	public ShadowmapResolution MapResolution = ShadowmapResolution.Res256;
	//Internal update flag
	[HideInInspector]
	public bool needsUpdate = true;
	//Wether or not the light emits any light (it's on or not)
	public bool on = true;
	
	//Reference matrices for shadow calculation
	[HideInInspector]
	public Matrix4x4 mvp;
	[HideInInspector]
	public Matrix4x4 lightMvp;
	[HideInInspector]
	public Matrix4x4 orthoProj;

	//Update flag variables
	float lastInt;
	Vector3 lastPos;
	Vector3 lastRot;
	float lastSize;
	Color lastCol;
	Vector3 initScale;
    float lastBlur;

	//GameObject lightQuad; <-- old code, unsure if this changed anything
	private void Start() {
		//In WebGL, too many iterations in the shader will cause it to fail, namely 1024 and more will crash the shader
		#if UNITY_WEBGL
		if(MapResolution == ShadowmapResolution.Res1024 || MapResolution == ShadowmapResolution.Res2048 || MapResolution == ShadowmapResolution.Res4096)
		{
			//Fallback to the next-highest quality
			MapResolution = ShadowmapResolution.Res512;
			Debug.LogWarning("WebGL does not support shadow quality of over 512");
		}
		#endif
	}
	
	//Used for updating the matrices used in shadow mapping
    public void RecalculateMatrices()
    {
        mvp = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse; //Get local inverse MVP matrix
        mvp.m23 = -10; //Make sure it's far away from the z-plane
        
		//Calculate the light MVP
		lightMvp = Matrix4x4.identity;
        lightMvp = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(size, size, 1.0f));
        
		//Create local orthographic projection matrix for this light
		orthoProj = Matrix4x4.Ortho(-(float)size / 2, (float)size / 2, -(float)size / 2, (float)size / 2, .3f, 1000f);
    }

	void Update()
	{
		//Check if update is needed, and if so update all values that need updating
		if (lastBlur != blurAmount || lastPos != transform.position || lastRot != transform.rotation.eulerAngles || lastSize != size || lastCol != lightColor || lastInt != intensity)
		{
			needsUpdate = true;
			lastSize = size;
			lastInt = intensity;
			lastCol = lightColor;
			lastPos = transform.position;
			lastRot = transform.rotation.eulerAngles;
            lastBlur = blurAmount;

			RecalculateMatrices();
		}
	}

	//Helper function
	public Vector3 DirFromAngle(float angleInDegrees, bool globalAngle)
	{
		if (!globalAngle)
			angleInDegrees += transform.eulerAngles.z;
		return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), transform.position.z);
	}
}
