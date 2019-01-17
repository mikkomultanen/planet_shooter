using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileMover : Explosive
{
    public float thrustAcceleration = 10f;
    public float maxSpeed = 10f;
    [Range(0.0f, 10f)]
    public float orthogonalDrag = 2f;

    protected Rigidbody2D rb;
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

    protected void FixedUpdate()
    {
        var forwardSpeed = Vector2.Dot(rb.velocity, transform.up);
        var forwardVelocity = forwardSpeed * (Vector2)transform.up;
        var orthogonalVelocity = rb.velocity - forwardVelocity;
        rb.AddForce(-orthogonalVelocity * orthogonalVelocity.magnitude * orthogonalDrag);

        if (forwardSpeed < maxSpeed)
        {
            rb.AddForce(transform.up * thurstForceMagnitude);
        }
    }

    protected void LateUpdate()
    {
        if (rb.position.magnitude > 300f)
        {
            Destroy(gameObject);
        }
    }
}
