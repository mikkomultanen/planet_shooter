using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{

	public float speed;

	private Rigidbody2D rb;
	private float gravityForceMagnitude;

	// Use this for initialization
	void Start ()
	{
		rb = GetComponent<Rigidbody2D> ();
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
		Vector2 initialVelocity = transform.up * speed;
		rb.velocity += initialVelocity;
	}

	void OnTriggerEnter2D(Collider2D other) {
		EarthBlock earthBlock = other.GetComponent<EarthBlock> ();
		if (earthBlock != null) {
			earthBlock.doDamage (100f);
		}
		ExplosionForce ef = GameObject.FindObjectOfType<ExplosionForce>();
		ef.doExplosion(transform.position);
		Destroy (gameObject);
	}

	void FixedUpdate ()
	{
		rb.AddForce (rb.position.normalized * gravityForceMagnitude);
	}

	// LateUpdate is called after Update each frame
	void LateUpdate ()
	{
		if (rb.position.magnitude > 300f) {
			Destroy (gameObject);
		} else {
			transform.up = rb.velocity.normalized;
		}
	}
}
