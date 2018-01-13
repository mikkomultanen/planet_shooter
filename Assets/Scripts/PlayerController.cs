﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Controls
{
	Keyboard,
	Joystick1,
	Joystick2,
	Joystick3,
	Joystick4
}

public class PlayerController : MonoBehaviour, Damageable
{
	public Camera playerCamera;
	public float cameraMinDistance = 55f;
	public float cameraMaxDistance = 105f;
	public GameController gameController;
	public Controls controls;
	public GameObject projectile;
	public Transform gunPoint;
	public GameObject missile;
	public Transform missilePoint;
	public float maxHealth = 100;
	public float maxThrustPower = 2000f;
	public float maxSpeed = 10f;
	public float fireRate = 0.2f;
	public float missileFireRate = 1f;
	public ParticleSystem thruster;
	public ParticleSystem flamer;
	public ParticleSystem smoke;
	public ParticleSystem explosion;

	private float health;
	private Vector3 cameraOffset;
	private Rigidbody2D rb;
	private float originalDrag;
	private float nextFire = 0.0f;
	private float nextMissileFire = 0.0f;
	private float gravityForceMagnitude;
	private bool isInWater = false;
	private string turnAxis;
	private string thrustAxis;
	private string fire1Button;
	private string fire2Button;
	private string fire3Button;
	private string fire4Button;

	// Use this for initialization
	void Start ()
	{
		health = maxHealth;
		cameraOffset = new Vector3(0, 0, -10);
		rb = gameObject.GetComponent<Rigidbody2D> ();
		transform.up = rb.position.normalized;
		originalDrag = rb.drag;
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
		turnAxis = controls.ToString () + " Turn";
		thrustAxis = controls.ToString () + " Thrust";
		fire1Button = controls.ToString () + " Fire1";
		fire2Button = controls.ToString () + " Fire2";
		fire3Button = controls.ToString () + " Fire3";
		fire4Button = controls.ToString () + " Fire4";
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

	void OnCollisionEnter2D (Collision2D collision)
	{
		doDamage (collision.relativeVelocity.sqrMagnitude / 100);
	}

	public void doDamage (float damage)
	{
		float oldHealt = health;
		health -= damage;
		if (health < 0 && oldHealt >= 0) {
			Instantiate (explosion, transform.position, transform.rotation);
			gameObject.SetActive (false);
			gameController.playerDied();
		}
	}

	void Update ()
	{
		if (Input.GetButton (fire1Button) && Time.time > nextFire) {
			nextFire = Time.time + fireRate;
			GameObject clone = Instantiate (projectile, gunPoint.position, gunPoint.rotation) as GameObject;
			clone.GetComponent<Rigidbody2D> ().velocity = rb.velocity;
		}
		if (Input.GetButton (fire2Button) && Time.time > nextMissileFire) {
			nextMissileFire = Time.time + missileFireRate;
			GameObject clone = Instantiate (missile, missilePoint.position, missilePoint.rotation) as GameObject;
			clone.GetComponent<Rigidbody2D> ().velocity = rb.velocity;
		}
		bool flamerOn = Input.GetButton (fire3Button);
		if (flamerOn != flamer.isEmitting) {
			if (flamerOn)
				flamer.Play ();
			else
				flamer.Stop ();
		}
		bool thrustersOn = Input.GetAxis (thrustAxis) > 0f;
		if (thrustersOn != thruster.isEmitting) {
			if (thrustersOn)
				thruster.Play ();
			else
				thruster.Stop ();
		}
		bool lowHealth = this.health < 20;
		if (lowHealth != smoke.isEmitting) {
			if (lowHealth)
				smoke.Play ();
			else
				smoke.Stop ();
		}
	}

	void FixedUpdate ()
	{
		float turn = Input.GetAxis (turnAxis);

		float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
		float thursterForceMagnitude = 0f;
		if (Vector2.Dot (rb.velocity, transform.up) < maxSpeed) {
			float thrust = Mathf.Max (Input.GetAxis (thrustAxis), 0f);
			float athmosphereCoefficient = Mathf.Clamp ((120f - rb.position.magnitude) / 20f, 0f, 1f);
			thursterForceMagnitude = thrust * athmosphereCoefficient * maxThrustPower;
			if (isInWater) {
				thursterForceMagnitude = Mathf.Min (thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
			}
		}
			
		Vector2 gravity = rb.position.normalized * floatingAndGravityForceMagnitude;
		Vector2 thrusters = transform.up * thursterForceMagnitude;

		rb.AddForce (thrusters + gravity);
		rb.angularVelocity = -turn * 300f;
	}

	void LateUpdate () 
	{
		Vector3 up = transform.position.normalized;
		Vector3 lookAt = up * Mathf.Clamp(transform.position.magnitude, cameraMinDistance, cameraMaxDistance);
		playerCamera.transform.position = lookAt + cameraOffset;
		playerCamera.transform.LookAt (lookAt, up);
	}

	public bool isAlive() {
		return health > 0;
	}

	public void respawn (Vector3 position)
	{
		health = maxHealth;
		transform.position = position;
		transform.up = position.normalized;
	}
}
