﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HomingMissileMover : Explosive
{
    public float thrustAcceleration = 10f;
    public float maxSpeed = 10f;
    public float homingRadius = 10f;
    private int homingLayerMask = 1;

    private Rigidbody2D rb;
    private float thurstForceMagnitude;
    private Transform target = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        thurstForceMagnitude = rb.mass * thrustAcceleration;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        explode();
    }

    void FixedUpdate()
    {
        if (Vector2.Dot(rb.velocity, transform.up) < maxSpeed)
        {
            rb.AddForce(transform.up * thurstForceMagnitude);
        }
        if (target != null)
        {
            var turn = Mathf.Sign(Vector2.SignedAngle(transform.up, target.position - transform.position));
            rb.angularVelocity = turn * 300f;
        }
    }

    void LateUpdate()
    {
        if (rb.position.magnitude > 300f)
        {
            Destroy(gameObject);
        }
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        var radarCenter = transform.position + transform.up * homingRadius;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(radarCenter, homingRadius, homingLayerMask);
        target = colliders.Where(c => c.tag == "Player").Select(c => c.transform).Aggregate(target, (memo, t) =>
        {
            if (memo == null)
            {
                return t;
            }
            else
            {
                var memoSqrD = sqrDistance(memo);
                var tSqrD = sqrDistance(t);
                return memoSqrD < tSqrD ? memo : t;
            }
        });
    }

    private float sqrDistance(Transform t)
    {
        Vector2 p = t.position;
        return (rb.position - p).sqrMagnitude;
    }
}
