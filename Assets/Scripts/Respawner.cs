using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{
	public void respawn (GameObject ship, float afterSeconds)
	{
		StartCoroutine (waitAndRespawn (ship, afterSeconds));
	}

	private IEnumerator waitAndRespawn (GameObject ship, float afterSeconds)
	{
		yield return new WaitForSeconds (afterSeconds);

		ship.SetActive (true);
		ship.GetComponent<PlayerController> ().respawn ();
	}
}
