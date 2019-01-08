using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCrate : Explosive
{

    static T RandomEnumValue<T> ()
    {
        var v = System.Enum.GetValues (typeof (T));
        return (T) v.GetValue (new System.Random ().Next(v.Length));
    }

    public enum Weapon
    {
        Flamer,
        Laser,
        Missiles,
        HomingMissiles,
        Bombs,
        Deathray,
        Shield,
        AfterBurner,
        Drone
    }
    private Rigidbody2D rb;
    private float gravityForceMagnitude;
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            ShipController ship = other.rigidbody.GetComponent<ShipController>();
            if (ship != null)
            {
                var player = ship.playerController;
                var weapon = Weapon.Flamer;//RandomEnumValue<Weapon>();
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
                    case Weapon.Drone:
                        player.spawnDrone(transform.position);
                        break;
                }
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        Vector2 gravity = rb.position.normalized * gravityForceMagnitude;
        rb.AddForce(gravity);
    }
}
