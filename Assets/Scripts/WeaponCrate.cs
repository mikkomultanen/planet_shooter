using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCrate : Explosive
{
    public enum Weapon
    {
        Flamer,
        Laser,
        Missiles,
        HomingMissiles,
        Bombs,
        Deathray
    }
    public Weapon weapon;
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
                switch (weapon)
                {
                    case Weapon.Missiles:
                        player.setSecondaryWeapon(SecondaryWeapon.Missiles, 10);
                        break;
                    case Weapon.HomingMissiles:
                        player.setSecondaryWeapon(SecondaryWeapon.HomingMissiles, 5);
                        break;
                    case Weapon.Bombs:
                        player.setSecondaryWeapon(SecondaryWeapon.Bombs, 5);
                        break;
                    case Weapon.Deathray:
                        player.setSecondaryWeapon(SecondaryWeapon.Deathray, 5);
                        break;
                    case Weapon.Flamer:
                        player.setPrimaryWeapon(PrimaryWeapon.Flamer, 10);
                        break;
                    case Weapon.Laser:
                        player.setPrimaryWeapon(PrimaryWeapon.Laser, 30);
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
}
