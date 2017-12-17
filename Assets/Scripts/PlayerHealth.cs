using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, Damageable
{

	public float health = 100;
	public ParticleSystem smoke;
	public ParticleSystem explosion;

	private float originalHealth;
	private PlayerController pc;

	// Use this for initialization
	void Start ()
	{
		originalHealth = health;
		pc = GetComponent<PlayerController> ();
	}

	void OnCollisionEnter2D (Collision2D collision)
	{
		doDamage (collision.relativeVelocity.sqrMagnitude / 100);
	}

	// Update is called once per frame
	void Update ()
	{
		bool lowHealth = (this.health <= 20);
		if (lowHealth && smoke.isStopped) {
			smoke.Play ();
		}
		if (lowHealth != smoke.isPlaying) {
			if (lowHealth)
				smoke.Play ();
			else
				smoke.Stop ();
		}
	}

	public void doDamage (float damage)
	{
		health -= damage;
		if (health < 0f) {
			Instantiate (explosion, transform.position, transform.rotation);
			health = originalHealth;
			gameObject.SetActive (false);
			Respawner ef = GameObject.FindObjectOfType<Respawner>();
			ef.respawn(gameObject, 5);
		}
	}
}
