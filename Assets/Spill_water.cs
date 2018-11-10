using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Camera))]
public class Spill_water : MonoBehaviour {

	public ParticleSystem waterSystem;
	
	private Camera _camera;
	private int count = 1;

	void Start () {
		_camera = GetComponent<Camera>();
	}
	
	void FixedUpdate () {
         
		if(Input.GetMouseButton(0)) {
			Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
			Vector3 wordPos = _camera.ScreenToWorldPoint(mousePos);
			wordPos.z = 0f;
			var emitParams = new ParticleSystem.EmitParams();
			emitParams.position = wordPos;
			waterSystem.Emit(emitParams, 1);
			Debug.Log ("count " + count++);
		}
	}
}
