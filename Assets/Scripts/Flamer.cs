using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flamer : MonoBehaviour {

	public float damagePerSecond = 20f;

	private ParticleSystem part;
	private List<ParticleCollisionEvent> collisionEvents;

	void Start()
	{
		part = GetComponent<ParticleSystem>();
		collisionEvents = new List<ParticleCollisionEvent>();
	}

	void OnParticleCollision(GameObject other)
	{
		Damageable damageable = other.GetComponent<Damageable> ();
		if (damageable != null) {
			damageable.doDamage (damagePerSecond * Time.smoothDeltaTime);
		}
	}
}
