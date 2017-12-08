using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	public GameObject projectile;
	public Transform gunPoint;
	public float maxThrustPower = 20f;
	public float fireRate = 0.2f;

	private Rigidbody2D rb;
	private float nextFire = 0.0f;
	private float gravityForceMagnitude;

	// Use this for initialization
	void Start ()
	{
		rb = gameObject.GetComponent<Rigidbody2D> ();
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		string jumpButton = tag == "Player" ? "Jump" : "Jump_2";
		if (Input.GetButton (jumpButton) && Time.time > nextFire) {
			nextFire = Time.time + fireRate;
			GameObject clone = Instantiate (projectile, gunPoint.position, gunPoint.rotation) as GameObject;
			clone.GetComponent<Rigidbody2D> ().velocity = rb.velocity;
		}
	}

	void FixedUpdate ()
	{
		float turn = 0.0f;
		float thrust = 0.0f;
		if (tag == "Player") {
			turn = Input.GetAxis ("Horizontal");
			thrust = Input.GetButton ("Fire2") ? 1.0f : 0.0f;
		} else {
			turn = Input.GetAxis ("Horizontal_2");
			thrust = Input.GetButton ("Fire2_2") ? 1.0f : 0.0f;
		}


		Vector2 gravity = rb.position.normalized * gravityForceMagnitude;
		Vector2 thrusters = transform.up * thrust * Mathf.Clamp ((120f - rb.position.magnitude) / 20f, 0f, 1f) * maxThrustPower;
		rb.AddForce (thrusters + gravity);

		rb.angularVelocity = -turn * 90f;
	}
}
