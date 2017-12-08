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

	void OnCollisionEnter2D (Collision2D coll)
	{
		//if (coll.gameObject.tag == "Enemy")
		//	coll.gameObject.SendMessage("ApplyDamage", 10);
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
