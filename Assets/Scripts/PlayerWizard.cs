using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWizard : MonoBehaviour {

	public Text controlsText;
	public GameObject pressStartPage;
	public GameObject readyPage;

	public ControlsManager controlsManager;

	void Start () {
		pressStartPage.SetActive(true);
		readyPage.SetActive(false);
	}
	
	public void setControls(Controls controls) {
		controlsText.text = controls.ToString();
		pressStartPage.SetActive(false);
		readyPage.SetActive(true);
		controlsManager.startGameSceneIfAllReady();
	}

	public bool isReady() {
		return readyPage.active;
	}
}
