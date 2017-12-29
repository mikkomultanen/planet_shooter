using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flamer : MonoBehaviour {

	public float damage = 1f;

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
			int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
			for (int i = 0; i < numCollisionEvents; i++) {
				damageable.doDamage (damage);
			}
		}
	}
}
