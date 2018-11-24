using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour, Damageable
{
    public float explosionDamage = 100f;
    public ParticleSystem explosion;
    public float health = 0.01f;

    private bool alive = true;
    protected void explode()
    {
        if (!alive)
        {
            return;
        }
        alive = false;
        float explosionForce = explosionDamage * 200f;
        float explosionRadius = Mathf.Sqrt(explosionDamage) * 0.7f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        Vector2 dir;
        float wearoff;
        Damageable damageable;
        TerrainPiece terrainPiece;
        foreach (Collider2D coll in colliders)
        {
            damageable = coll.GetComponent<Damageable>();
            if (damageable != null)
            {
                dir = (coll.transform.position - transform.position);
                wearoff = 1 - (dir.magnitude / explosionRadius);
                damageable.doDamage(explosionDamage * wearoff);
            }
            terrainPiece = coll.GetComponent<TerrainPiece>();
            if (terrainPiece != null)
            {
                terrainPiece.destroyTerrain(transform.position, explosionRadius * 0.6f);
            }
        }

        var position = transform.position;
        position.z = explosion.transform.position.z;
        Instantiate(explosion, position, transform.rotation);

        ExplosionForce ef = GameObject.FindObjectOfType<ExplosionForce>();
        ef.doExplosion(transform.position, explosionForce, explosionRadius);

        afterExposion();
    }

    public virtual void doDamage(float damage)
    {
        health -= damage;
        if (health < 0f) explode();
    }

    protected virtual void afterExposion()
    {
        Destroy(gameObject);
    }
}
