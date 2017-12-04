using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour {

	public float speed;

	private Rigidbody rb;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		rb.velocity += transform.forward * speed;
	}

	void OnTriggerEnter(Collider other) {
		Destroy(gameObject);
	}

	void FixedUpdate () {
		Vector3 gravity = rb.position;
		gravity.Normalize ();
		gravity *= -1f;

		rb.AddForce (gravity);

	}

	// LateUpdate is called after Update each frame
	void LateUpdate () {
		transform.LookAt (rb.position + rb.velocity);
		if (rb.position.magnitude > 300f) {
			Destroy(gameObject);
		}
	}
}
