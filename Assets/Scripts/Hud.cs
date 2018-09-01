using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class Hud : MonoBehaviour
{

    public Text health;
    public Text weapons;
    public Image enemyIndicatorTemplate;

    private Canvas canvas;
    private Camera _camera;
    private List<PlayerController> players = new List<PlayerController>();
    private List<Image> enemyIndicators = new List<Image>();
    public void UpdateHealth(int health)
    {
        this.health.text = "Health: " + health;
    }

    public void UpdateWeapons(IDevice primary, IDevice secondary)
    {
        var text = new List<string>();
        text.Add(primary.HudRow());
        if (secondary != null)
        {
            text.Add(secondary.HudRow());
        }
        weapons.text = string.Join("\n", text.ToArray());
    }

    public static string energyToString(float energy)
    {
        return Mathf.Max(0, energy).ToString("f1");
    }

    public void InitializeEnemyIndicators(List<PlayerController> players)
    {
        this.players = players;
        enemyIndicators = players.Select(player =>
        {
            var indicator = Instantiate(enemyIndicatorTemplate);
            indicator.rectTransform.parent = transform;
            indicator.rectTransform.localScale = Vector3.one;
            indicator.transform.localPosition = Vector3.zero;
            indicator.rectTransform.anchoredPosition = Vector2.zero;
            indicator.gameObject.SetActive(true);
            indicator.color = player.color;
            return indicator;
        }).ToList();
    }

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        _camera = canvas.worldCamera;
    }
    private void LateUpdate()
    {
        PlayerController player;
        Image indicator;
        Vector3 screenPos;
        Vector2 onScreenPos;
        float angle;
        float max;
        for (int i = 0; i < players.Count; i++)
        {
            player = players[i];
            indicator = enemyIndicators[i];

            if (player.ship == null) {
                indicator.gameObject.SetActive(false);
                return;
            }

            screenPos = _camera.WorldToViewportPoint(player.transform.position); //get viewport positions

            if (screenPos.x >= 0 && screenPos.x <= 1 && screenPos.y >= 0 && screenPos.y <= 1)
            {
                indicator.gameObject.SetActive(false);
            }
            else
            {
                onScreenPos = new Vector2(screenPos.x - 0.5f, screenPos.y - 0.5f) * 2; //2D version, new mapping
                max = Mathf.Max(Mathf.Abs(onScreenPos.x), Mathf.Abs(onScreenPos.y)); //get largest offset
                onScreenPos = (onScreenPos / (max * 2)) + new Vector2(0.5f, 0.5f); //undo mapping
                indicator.rectTransform.anchorMin = onScreenPos;
                indicator.rectTransform.anchorMax = onScreenPos;
                angle = -Mathf.Atan2(onScreenPos.x - 0.5f, onScreenPos.y - 0.5f) * Mathf.Rad2Deg;
                indicator.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                indicator.gameObject.SetActive(true);
            }
        }
    }
}
