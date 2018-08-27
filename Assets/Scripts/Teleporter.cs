using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Teleporter : MonoBehaviour {

	private float disolve = 1;
	private Renderer _renderer;
	private MaterialPropertyBlock propBlock;
	private int disolveNameId;

	void Awake () {
		_renderer = GetComponent<Renderer>();
		propBlock = new MaterialPropertyBlock();
		disolveNameId = Shader.PropertyToID("_Disolve");
	}

	private void Start() {
		UpdatePropBlock();
	}
	
	void Update () {
		UpdatePropBlock();
		disolve = Mathf.Max(0, disolve - Time.deltaTime);
		if (Input.GetKeyDown("t"))
        {
            Restart();
        }
	}

	private void UpdatePropBlock() {
		_renderer.SetPropertyBlock(propBlock);
		propBlock.SetFloat(disolveNameId, Mathf.Sqrt(disolve));
		_renderer.SetPropertyBlock(propBlock);
	}

	public void Restart() {
		disolve = 1;
	}
}
