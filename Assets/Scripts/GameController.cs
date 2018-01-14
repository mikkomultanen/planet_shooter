using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

	public GameObject shipTemplate;
	public List<Camera> cameras;
	public List<Color> colors;
	public List<Transform> startPositions;
	public Collider2D water;
	public Text roundText;
	public List<Text> scoreTexts;
	public Canvas canvas;

	private int playerCount;
	private EventSystem eventSystem;
	private List<PlayerController> playerControllers;

	private void Awake() {
		playerCount = players.Count;
		playerControllers = new List<PlayerController>();
		for (int i = 0; i < cameras.Count; i++) {
			if (i < playerCount) {
				GameObject ship = Instantiate (shipTemplate, startPositions[i].position, startPositions[i].rotation) as GameObject;
				var playerController = ship.GetComponent<PlayerController> (); 
				playerController.playerCamera = cameras[i];
				playerController.controls = players[i].controls;
				playerController.gameController = this;
				var flamerTrigger = playerController.flamer.trigger;
				flamerTrigger.SetCollider(0, water);
				ship.GetComponent<SpriteRenderer>().color = colors[i];
				playerControllers.Add(playerController);
			} else {
				cameras[i].gameObject.SetActive(false);
			}
		}
		if (playerCount == 2) {
			playerControllers[0].playerCamera.rect = new Rect(0.25f, 0.5f, 0.5f, 0.5f);
			playerControllers[1].playerCamera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
		}
		eventSystem = GetComponent<EventSystem>();
		eventSystem.enabled = false;
		canvas.enabled = false;
		updateScoreboard();
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
			updateScoreboard();
		}
		endingRound = false;
		startNewRound();
	}

	private void startNewRound() {
		Debug.Log ("Start new round");
		round++;
		roundText.text = "Round " + round;
		for (int i = 0; i < playerCount; i++) {
			playerControllers[i].gameObject.SetActive (true);
			playerControllers[i].respawn(startPositions[i].position);
		}
	}

	private void updateScoreboard() {
		for (int i = 0; i < playerCount; i++) {
			scoreTexts[i].text = players[i].wins.ToString();
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

	private static int round = 0;
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
