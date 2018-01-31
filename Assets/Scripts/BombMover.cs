using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombMover : Explosive
{
    public float speed;

    private Rigidbody2D rb;

    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Vector2 initialVelocity = transform.up * speed;
        rb.velocity += initialVelocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        explode();
    }
}
