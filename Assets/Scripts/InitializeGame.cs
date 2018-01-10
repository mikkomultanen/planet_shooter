using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeGame : MonoBehaviour {

	public List<PlayerController> playerControllers;

	private void Awake() {
		int playerCount = ControlsManager.getPlayerCount();
		for (int i = 0; i < playerControllers.Count; i++) {
			if (i < playerCount) {
				playerControllers[i].controls = ControlsManager.getControls(i);
			} else {
				playerControllers[i].camera.gameObject.SetActive(false);
				playerControllers[i].gameObject.SetActive(false);
			}
		}
		if (playerCount == 2) {
			playerControllers[0].camera.rect = new Rect(0.25f, 0.5f, 0.5f, 0.5f);
			playerControllers[1].camera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
		}
	}
}
