using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileMover : Explosive
{
    public float thrustAcceleration = 10f;
    public float maxSpeed = 10f;

    private Rigidbody2D rb;
    private float thurstForceMagnitude;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        thurstForceMagnitude = rb.mass * thrustAcceleration;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        explode();
    }

    void FixedUpdate()
    {
        if (Vector2.Dot(rb.velocity, transform.up) < maxSpeed)
        {
            rb.AddForce(transform.up * thurstForceMagnitude);
        }
    }

    void LateUpdate()
    {
        if (rb.position.magnitude > 300f)
        {
            Destroy(gameObject);
        }
    }
}
