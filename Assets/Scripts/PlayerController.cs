﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerNumber
{
	P1,
	P2,
	P3,
	P4
}

public class PlayerController : MonoBehaviour
{
	public PlayerNumber playerNumber;
	public GameObject projectile;
	public GameObject missile;
	public Transform gunPoint;
	public float maxThrustPower = 2000f;
	public float maxSpeed = 10f;
	public float fireRate = 0.2f;
	public float missileFireRate = 1f;
	public ParticleSystem thruster;

	private Rigidbody2D rb;
	private float originalDrag;
	private float nextFire = 0.0f;
	private float nextMissileFire = 0.0f;
	private float gravityForceMagnitude;
	private bool isInWater = false;
	private string xAxis;
	private string yAxis;
	private string fire1Button;
	private string fire2Button;

	// Use this for initialization
	void Start ()
	{
		rb = gameObject.GetComponent<Rigidbody2D> ();
		transform.up = rb.position.normalized;
		originalDrag = rb.drag;
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
		xAxis = playerNumber.ToString () + " Horizontal";
		yAxis = playerNumber.ToString () + " Vertical";
		fire1Button = playerNumber.ToString () + " Fire1";
		fire2Button = playerNumber.ToString () + " Fire2";
	}

	void OnTriggerEnter2D (Collider2D other)
	{
		if (other.tag == "Water") {
			isInWater = true;
			rb.drag = 5;
		}
	}

	void OnTriggerExit2D (Collider2D other)
	{
		if (other.tag == "Water") {
			isInWater = false;
			rb.drag = originalDrag;
		}
	}

	// Update is called once per frame
	void Update ()
	{
		if (Input.GetButton (fire1Button) && Time.time > nextFire) {
			nextFire = Time.time + fireRate;
			GameObject clone = Instantiate (projectile, gunPoint.position, gunPoint.rotation) as GameObject;
			clone.GetComponent<Rigidbody2D> ().velocity = rb.velocity;
		}
		if (Input.GetButton (fire2Button) && Time.time > nextMissileFire) {
			nextMissileFire = Time.time + missileFireRate;
			GameObject clone = Instantiate (missile, gunPoint.position, gunPoint.rotation) as GameObject;
			clone.GetComponent<Rigidbody2D> ().velocity = rb.velocity;
		}
		bool thrustersOn = Input.GetAxis (yAxis) > 0f;
		if (thrustersOn != thruster.isPlaying) {
			if (thrustersOn)
				thruster.Play ();
			else
				thruster.Stop ();
		}
	}

	void FixedUpdate ()
	{
		float turn = Input.GetAxis (xAxis);

		float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
		float thursterForceMagnitude = 0f;
		if (Vector2.Dot (rb.velocity, transform.up) < maxSpeed) {
			float thrust = Mathf.Max (Input.GetAxis (yAxis), 0f);
			float athmosphereCoefficient = Mathf.Clamp ((120f - rb.position.magnitude) / 20f, 0f, 1f);
			thursterForceMagnitude = thrust * athmosphereCoefficient * maxThrustPower;
			if (isInWater) {
				thursterForceMagnitude = Mathf.Min (thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
			}
		}
			
		Vector2 gravity = rb.position.normalized * floatingAndGravityForceMagnitude;
		Vector2 thrusters = transform.up * thursterForceMagnitude;

		rb.AddForce (thrusters + gravity);
		// TODO use rb.AddTorque and angularDrag to turn the ship
		rb.angularVelocity = -turn * 90f;
	}

	public void respawn ()
	{
		Debug.Log ("respawn " + transform.parent.position);
		transform.localPosition = new Vector3 (0, 0, 0);
		transform.up = rb.position.normalized;
	}
}
