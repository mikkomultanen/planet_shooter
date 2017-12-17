﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ExplosionForce : MonoBehaviour
{
	public void doExplosion (Vector3 position, float force, float radius)
	{
		StartCoroutine (waitAndExplode (position, force, radius));
	}

	private IEnumerator waitAndExplode (Vector3 position, float force, float radius)
	{
		yield return new WaitForFixedUpdate ();
		
		Collider2D[] colliders = Physics2D.OverlapCircleAll (position, radius);

		Vector2 dir;
		float wearoff;
		Rigidbody2D rb;
		foreach (Collider2D coll in colliders) {
			rb = coll.GetComponent<Rigidbody2D> ();
			if (rb != null) {
				dir = (coll.transform.position - position);
				wearoff = 1 - (dir.magnitude / radius);
				Vector3 baseForce = dir.normalized * force * wearoff;
				baseForce.z = 0;
				rb.AddForce (baseForce);
			}
		}
	}
}
