﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCrate : Explosive, Damageable
{

    private Rigidbody2D rb;
    private float originalDrag;
    private bool isInWater = false;
    private float gravityForceMagnitude;
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        originalDrag = rb.drag;
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = true;
            rb.drag = 5;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = false;
            rb.drag = originalDrag;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerController player = other.rigidbody.GetComponent<PlayerController>();
            if (player != null)
            {
                player.removeSecondaryWeapon();
                switch (Random.Range(0, 4))
                {
                    case 0:
                        player.secondaryWeapon = SecondaryWeapon.Missiles;
                        player.secondaryAmmunition = 5;
                        break;
					case 1:
                        player.secondaryWeapon = SecondaryWeapon.Bombs;
                        player.secondaryAmmunition = 5;
						break;
                    case 2:
                        player.secondaryWeapon = SecondaryWeapon.Flamer;
                        player.secondaryEnergy = 10;
                        break;
                    case 3:
                        player.secondaryWeapon = SecondaryWeapon.Laser;
                        player.secondaryEnergy = 10;
                        break;
                }
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
        Vector2 gravity = rb.position.normalized * floatingAndGravityForceMagnitude;
        rb.AddForce(gravity);
    }

    public void doDamage(float damage)
    {
		explode();
    }
}