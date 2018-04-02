using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{

    public float speed;
    public float damage = 1f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Vector2 initialVelocity = transform.up * speed;
        rb.velocity += initialVelocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Damageable damageable = other.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.doDamage(damage);
        }
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        transform.up = rb.velocity.normalized;
    }
}
