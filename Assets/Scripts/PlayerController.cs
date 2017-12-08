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
	private bool isInWater = false;

	// Use this for initialization
	void Start ()
	{
		rb = gameObject.GetComponent<Rigidbody2D> ();
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
	}

	void OnTriggerEnter2D(Collider2D other) {
		if (other.tag == "Water") {
			isInWater = true;
			rb.drag = 5;
		}
	}

	void OnTriggerExit2D(Collider2D other) {
		if (other.tag == "Water") {
			isInWater = false;
			rb.drag = 1;
		}
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

		float positionMagnitude = rb.position.magnitude;
		float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
		float thursterForceMagnitude = thrust * Mathf.Clamp ((120f - positionMagnitude) / 20f, 0f, 1f) * maxThrustPower;
		if (isInWater) {
			thursterForceMagnitude = Mathf.Min (thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
		}
			
		Vector2 gravity = rb.position.normalized * floatingAndGravityForceMagnitude;
		Vector2 thrusters = transform.up * thursterForceMagnitude;

		rb.AddForce (thrusters + gravity);
		rb.angularVelocity = -turn * 90f;
	}
}
