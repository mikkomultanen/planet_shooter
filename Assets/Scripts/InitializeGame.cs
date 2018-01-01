using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeGame : MonoBehaviour {

	public List<PlayerController> playerControllers;

	private void Awake() {
		for (int i = 0; i < playerControllers.Count; i++) {
			playerControllers[i].controls = ControlsManager.getControls(i);
		}
	}
}
