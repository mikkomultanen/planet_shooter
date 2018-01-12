using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

	public List<PlayerController> playerControllers;
	public Canvas canvas;

	private int playerCount;
	private EventSystem eventSystem;

	private void Awake() {
		playerCount = players.Count;
		for (int i = 0; i < playerControllers.Count; i++) {
			if (i < playerCount) {
				playerControllers[i].controls = players[i].controls;
				playerControllers[i].gameController = this;
			} else {
				playerControllers[i].camera.gameObject.SetActive(false);
				playerControllers[i].gameObject.SetActive(false);
			}
		}
		if (playerCount == 2) {
			playerControllers[0].camera.rect = new Rect(0.25f, 0.5f, 0.5f, 0.5f);
			playerControllers[1].camera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
		}
		eventSystem = GetComponent<EventSystem>();
		eventSystem.enabled = false;
		canvas.enabled = false;
		startNewRound();
	}

	private void Update() {
		if (Input.GetButtonDown("Menu Cancel")) {
			if (canvas.enabled) {
				resume();
			} else {
				pause();
			}
		}
	}

	public void playerDied() {
		Debug.Log ("playerDied");
		int numberOfAlivePlayers = 0;
		for (int i = 0; i < playerCount; i++) {
			if (playerControllers[i].isAlive()) {
				numberOfAlivePlayers++;
			}
		}
		if (numberOfAlivePlayers <= 1) {
			if (!endingRound) {
				Debug.Log ("playerDied endRoundAfter 2");
				StartCoroutine (endRoundAfter (2));
			}
		}
	}

	private bool endingRound = false;

	private IEnumerator endRoundAfter (float seconds)
	{
		yield return new WaitForSeconds (seconds);

		int numberOfAlivePlayers = 0;
		Player lastAlivePlayer = null;
		for (int i = 0; i < playerCount; i++) {
			if (playerControllers[i].isAlive()) {
				numberOfAlivePlayers++;
				lastAlivePlayer = players[i];
			}
		}
		if (numberOfAlivePlayers == 1 && lastAlivePlayer != null) {
			lastAlivePlayer.wins++;
		}
		endingRound = false;
		startNewRound();
	}

	private void startNewRound() {
		Debug.Log ("Start new round");
		for (int i = 0; i < playerCount; i++) {
			playerControllers[i].gameObject.SetActive (true);
			playerControllers[i].respawn(playerControllers[i].transform.parent.position);
		}
	}

	public void pause() {
		eventSystem.enabled = true;
		canvas.enabled = true;
		Time.timeScale = 0;
		playerControllers.ForEach(c => c.enabled = false);
	}

	public void resume() {
		eventSystem.enabled = false;
		canvas.enabled = false;
		Time.timeScale = 1;
		playerControllers.ForEach(c => c.enabled = true);
	}

	private static List<Player> players = new List<Player> {new Player(Controls.Keyboard), new Player(Controls.Joystick1)};

	public static void setPlayers(List<Player> players) {
		GameController.players = players;
	}

	public class Player {
		public Controls controls;
		public int wins = 0;

		public Player(Controls controls) {
			this.controls = controls;
		}
	}
}
