using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent (typeof(Explodable))]
public class CaveGenerator : ExplodableAddon
{
	void Awake ()
	{
		if (!Application.isEditor || EditorApplication.isPlaying) {
			GetComponent<Explodable> ().explode ();
		}
	}

	public override void OnFragmentsGenerated (List<GameObject> fragments)
	{
		foreach (GameObject fragment in fragments) {
			Rigidbody2D fragRb = fragment.GetComponent<Rigidbody2D> ();
			fragRb.bodyType = RigidbodyType2D.Static;
			fragment.AddComponent<EarthBlock> ();
		}
	}
}

