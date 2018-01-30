using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCrate : MonoBehaviour, Damageable
{

    public float explosionDamage = 100f;
    public float explosionForce = 50000f;
    public float explosionRadius = 5f;
    public ParticleSystem explosion;

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
                switch (Random.Range(0, 3))
                {
                    case 0:
                        player.secondaryWeapon = SecondaryWeapon.Missiles;
                        player.missiles = 5;
                        break;
                    case 1:
                        player.secondaryWeapon = SecondaryWeapon.Flamer;
                        player.flamerFuel = 10;
                        break;
                    case 2:
                        player.secondaryWeapon = SecondaryWeapon.Laser;
                        player.laserEnergy = 10;
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

    private bool alive = true;
    public void doDamage(float damage)
    {
        if (!alive)
        {
            return;
        }
        alive = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        Vector2 dir;
        float wearoff;
        Damageable damageable;
        foreach (Collider2D coll in colliders)
        {
            damageable = coll.GetComponent<Damageable>();
            if (damageable != null)
            {
                dir = (coll.transform.position - transform.position);
                wearoff = 1 - (dir.magnitude / explosionRadius);
                damageable.doDamage(explosionDamage * wearoff);
            }
        }

        Instantiate(explosion, transform.position, transform.rotation);
        ExplosionForce ef = GameObject.FindObjectOfType<ExplosionForce>();
        ef.doExplosion(transform.position, explosionForce, explosionRadius);
        Destroy(gameObject);
    }

}
