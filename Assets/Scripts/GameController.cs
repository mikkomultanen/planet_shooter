using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{

    public RepairBase repairBaseTemplate;
    public List<GameObject> weaponCrateTemplates;
    public PlayerController cameraTemplate;
    public Hud hudTemplate;
    public List<Color> colors;
    public TerrainMesh terrain;
    public FluidSystem fluidSystem;
    public Text roundText;
    public List<Text> scoreTexts;
    public Canvas canvas;

    private int playerCount;
    private EventSystem eventSystem;
    private List<PlayerController> playerControllers;

    private void Awake()
    {
        playerCount = players.Count;
        playerControllers = new List<PlayerController>();
        var startPositions = terrain.startPositions(playerCount);
        for (int i = 0; i < playerCount; i++)
        {
            PlayerController playerController = Instantiate(cameraTemplate, startPositions[i], Quaternion.identity) as PlayerController;
            playerController.gameObject.SetActive(true);
            Camera camera = playerController._camera;
            switch (i)
            {
                case 0:
                    camera.rect = new Rect(0.0f, 0.5f, 0.5f, 0.5f);
                    break;
                case 1:
                    camera.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    break;
                case 2:
                    camera.rect = new Rect(0.0f, 0.0f, 0.5f, 0.5f);
                    break;
                case 3:
                    camera.rect = new Rect(0.5f, 0.0f, 0.5f, 0.5f);
                    break;
            }

            playerController.gameController = this;

            Hud hud = Instantiate(hudTemplate, startPositions[i], Quaternion.identity) as Hud;
            hud.gameObject.SetActive(true);
            hud.GetComponent<Canvas>().worldCamera = camera;
            playerController.hud = hud;

            var repairBase = Instantiate(repairBaseTemplate, startPositions[i], Quaternion.identity) as RepairBase;
            repairBase.gameObject.SetActive(true);
            playerController.repairBase = repairBase;

            playerController.controls = players[i].controls;
            playerController.color = colors[i];
            playerControllers.Add(playerController);
            
        }
        playerControllers.ForEach(c => c.hud.InitializeEnemyIndicators(playerControllers));
        if (playerCount == 2)
        {
            playerControllers[0]._camera.rect = new Rect(0.25f, 0.5f, 0.5f, 0.5f);
            playerControllers[1]._camera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
        }
        eventSystem = GetComponent<EventSystem>();
        eventSystem.enabled = false;
        canvas.enabled = false;
        updateScoreboard();
        startNewRound();
        StartCoroutine(spawnWeaponCrateWithInterval(10));

    }

    private void Update()
    {
        if (Input.GetButtonDown("Menu Cancel"))
        {
            if (canvas.enabled)
            {
                resume();
            }
            else
            {
                pause();
            }
        }
    }

    public void playerDied()
    {
        Debug.Log("playerDied");
        int numberOfAlivePlayers = 0;
        for (int i = 0; i < playerCount; i++)
        {
            if (playerControllers[i].isAlive())
            {
                numberOfAlivePlayers++;
            }
        }
        if (numberOfAlivePlayers <= 1)
        {
            if (!endingRound)
            {
                Debug.Log("playerDied endRoundAfter 2");
                StartCoroutine(endRoundAfter(2));
            }
        }
    }

    private bool endingRound = false;

    private IEnumerator endRoundAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        int numberOfAlivePlayers = 0;
        Player lastAlivePlayer = null;
        for (int i = 0; i < playerCount; i++)
        {
            if (playerControllers[i].isAlive())
            {
                numberOfAlivePlayers++;
                lastAlivePlayer = players[i];
            }
        }
        if (numberOfAlivePlayers == 1 && lastAlivePlayer != null)
        {
            lastAlivePlayer.wins++;
            updateScoreboard();
        }
        endingRound = false;
        startNewRound();
    }

    private void startNewRound()
    {
        Debug.Log("Start new round");
        round++;
        roundText.text = "Round " + round;
        var startPositions = terrain.startPositions(playerCount);
        for (int i = 0; i < playerCount; i++)
        {
            playerControllers[i].respawn(startPositions[i]);
        }
        foreach (var crate in GameObject.FindObjectsOfType<WeaponCrate>())
        {
            Destroy(crate.gameObject);
        }
        foreach (var drone in GameObject.FindObjectsOfType<DroneController>())
        {
            Destroy(drone.gameObject);
        }
    }

    private IEnumerator spawnWeaponCrateWithInterval(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            GameObject weaponCrateTemplate = weaponCrateTemplates[Random.Range(0, weaponCrateTemplates.Count)];
            Vector2 position = terrain.randomUpperCaveCenter();
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0, 2 * Mathf.PI));
            GameObject newCrate = Instantiate(weaponCrateTemplate, position, rotation);
            newCrate.SetActive(true);
        }
    }

    private void updateScoreboard()
    {
        for (int i = 0; i < playerCount; i++)
        {
            scoreTexts[i].text = players[i].wins.ToString();
        }
    }

    public void pause()
    {
        eventSystem.enabled = true;
        canvas.enabled = true;
        Time.timeScale = 0;
        playerControllers.ForEach(c => c.enabled = false);
    }

    public void resume()
    {
        eventSystem.enabled = false;
        canvas.enabled = false;
        Time.timeScale = 1;
        playerControllers.ForEach(c => c.enabled = true);
    }

    private static int round = 0;
    private static List<Player> players = new List<Player> { new Player(Controls.Keyboard), new Player(Controls.Joystick1) };

    public static void setPlayers(List<Player> players)
    {
        GameController.players = players;
    }

    public class Player
    {
        public Controls controls;
        public int wins = 0;

        public Player(Controls controls)
        {
            this.controls = controls;
        }
    }
}
