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
        Deathray,
        Shield,
        AfterBurner
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
                    case Weapon.Flamer:
                        player.setPrimaryWeapon(new FlamerDevice());
                        break;
                    case Weapon.Laser:
                        player.setPrimaryWeapon(new LaserDevice());
                        break;
                    case Weapon.AfterBurner:
                        player.setPrimaryWeapon(new AfterBurnerDevice());
                        break;
                    case Weapon.Missiles:
                        player.setSecondaryWeapon(new MissileDevice());
                        break;
                    case Weapon.HomingMissiles:
                        player.setSecondaryWeapon(new HomingMissileDevice());
                        break;
                    case Weapon.Bombs:
                        player.setSecondaryWeapon(new BombDevice());
                        break;
                    case Weapon.Deathray:
                        player.setSecondaryWeapon(new DeathrayDevice());
                        break;
                    case Weapon.Shield:
                        player.setSecondaryWeapon(new ShieldDevice());
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
