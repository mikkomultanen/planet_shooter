using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class Hud : MonoBehaviour {

	public Text health;
	public void UpdateHealth (int health) {
		this.health.text = "Health: " + health;
	}
}
