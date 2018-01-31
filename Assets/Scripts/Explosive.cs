using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    public float explosionDamage = 100f;
    public float explosionForce = 20000f;
    public float explosionRadius = 5f;
    public ParticleSystem explosion;

    private bool alive = true;
    protected void explode()
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
