using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{

    public float speed;
    public float damage = 1f;

    private Rigidbody2D rb;
    private float minSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Vector2 initialVelocity = transform.up * speed;
        rb.velocity += initialVelocity;
        minSpeed = 0.2f * speed;
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
        float currentSpeed = rb.velocity.magnitude;
        if (currentSpeed < minSpeed) {
            Destroy(gameObject);
        } else {
            transform.up = rb.velocity / currentSpeed;
        }
    }
}
