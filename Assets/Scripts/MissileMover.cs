using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileMover : MonoBehaviour
{

	public float explosionDamage = 100f;
	public float explosionForce = 50000f;
	public float explosionRadius = 5f;
	public float thrustAcceleration = 10f;
	public float maxSpeed = 10f;
	public ParticleSystem explosion;

	private Rigidbody2D rb;
	private float thurstForceMagnitude;

	void Start ()
	{
		rb = GetComponent<Rigidbody2D> ();
		thurstForceMagnitude = rb.mass * thrustAcceleration;
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		Collider2D[] colliders = Physics2D.OverlapCircleAll (transform.position, explosionRadius);
		Vector2 dir;
		float wearoff;
		Damageable damageable;
		foreach (Collider2D coll in colliders) {
			damageable = coll.GetComponent<Damageable> ();
			if (damageable != null) {
				dir = (coll.transform.position - transform.position);
				wearoff = 1 - (dir.magnitude / explosionRadius);
				damageable.doDamage (explosionDamage * wearoff);
			}
		}

		Instantiate (explosion, transform.position, transform.rotation);
		ExplosionForce ef = GameObject.FindObjectOfType<ExplosionForce> ();
		ef.doExplosion (transform.position, explosionForce, explosionRadius);
		Destroy (gameObject);
	}

	void FixedUpdate ()
	{
		if (Vector2.Dot (rb.velocity, transform.up) < maxSpeed) {
			rb.AddForce (transform.up * thurstForceMagnitude);
		}
	}

	void LateUpdate ()
	{
		if (rb.position.magnitude > 300f) {
			Destroy (gameObject);
		}
	}
}
