using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControlsManager : SceneLoader {

	public List<PlayerWizard> playerWizards;
	public Text startGameCountdown;
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
		int readyWizardsCount = playerWizards.FindAll(x => x.isReady()).Count;
		if (readyWizardsCount > 1 && readyWizardsCount == playerControls.Count && currentCountdownValue == 0) {
			StartCoroutine(startCountdown());
		}
	}

	private float currentCountdownValue = 0;
	public IEnumerator startCountdown(float countdownValue = 5)
	{
		currentCountdownValue = countdownValue;
		while (currentCountdownValue > 0)
		{
			startGameCountdown.text = "Starting in " + currentCountdownValue + "s";
			yield return new WaitForSeconds(1.0f);
			currentCountdownValue--;
		}
		controls = playerControls;
		loadSceneAsync(gameScene);
	}

	private static List<Controls> controls = new List<Controls> {Controls.Keyboard, Controls.Joystick1};

	public static int getPlayerCount() {
		return controls.Count;
	}
	public static Controls getControls(int index) {
		return controls[index];
	}
}
