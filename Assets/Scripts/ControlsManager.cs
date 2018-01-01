using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControlsManager : SceneLoader {

	public List<PlayerWizard> playerWizards;
	public string gameScene;
	public string previousScene;
	private List<Controls> playerControls = new List<Controls>();
	private List<Controls> availableControls;
	
	private void Awake() {
		availableControls = new List<Controls>((Controls[])System.Enum.GetValues(typeof(Controls)));
	}

	void Update () {
		if (Input.GetButton("Menu Cancel")) {
			loadScene(previousScene);
		}
		var index = availableControls.FindIndex(x => Input.GetButton(x.ToString() + " Fire1") ||
				Input.GetButton(x.ToString() + " Fire2") ||
				Input.GetButton(x.ToString() + " Fire3") ||
				Input.GetButton(x.ToString() + " Fire4"));
		if (index > -1) {
			var wizardIndex = playerControls.Count;
			var wizard = playerWizards.Count > wizardIndex ? playerWizards[wizardIndex] : null;
			if (wizard != null) {
				var controls = availableControls[index];
				playerControls.Add(controls);
				availableControls.Remove(controls);
				wizard.setControls(controls);
			}
		}
	}

	public void startGameSceneIfAllReady() {
		if (playerWizards.TrueForAll(x => x.isReady())) {
			controls = playerControls;
			loadSceneAsync(gameScene);
		}
	}

	private static List<Controls> controls;

	public static Controls getControls(int index) {
		return controls[index];
	}
}
