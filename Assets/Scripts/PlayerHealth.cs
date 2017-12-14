using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour {

    public float health = 100;
    private float bulletDamage = 10;
    public ParticleSystem smoke;

    // Use this for initialization
    void Start () {
		
	}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Bullet") {
            this.health -= this.bulletDamage;
        }
            
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        this.health -= collision.relativeVelocity.sqrMagnitude / 100;
    }

    // Update is called once per frame
    void Update () {
        bool lowHealth = (this.health <= 20);
		if (lowHealth && smoke.isStopped)
        {
            smoke.Play();
        }
        if (lowHealth != smoke.isPlaying)
        {
            if (lowHealth) smoke.Play();
            else smoke.Stop();
        }
    }
}
