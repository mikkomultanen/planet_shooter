using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HomingMissileMover : Explosive
{
    public float thrustAcceleration = 10f;
    public float maxSpeed = 10f;
    [Range(0.0f, 10f)]
    public float orthogonalDrag = 2f;
    public float homingRadius = 10f;
    public LayerMask homingLayerMask = 1;

    private Rigidbody2D rb;
    private float thurstForceMagnitude;
    private Transform target = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        thurstForceMagnitude = rb.mass * thrustAcceleration;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == 4) // Water
        {
            explode();
        }
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            var targetDirection = target.position - transform.position;
            var angle = Vector2.SignedAngle(transform.up, targetDirection);
            rb.angularVelocity = Mathf.Clamp(angle / 15f, -1, 1) * 300f;
        }

        var forwardSpeed = Vector2.Dot(rb.velocity, transform.up);
        var forwardVelocity = forwardSpeed * (Vector2)transform.up;
        var orthogonalVelocity = rb.velocity - forwardVelocity;
        rb.AddForce(-orthogonalVelocity * orthogonalVelocity.magnitude * orthogonalDrag);

        if (forwardSpeed < maxSpeed)
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
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        var radarCenter = transform.position + transform.up * homingRadius;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(radarCenter, homingRadius, homingLayerMask);
        target = colliders.Select(c => c.transform).Aggregate(target, (memo, t) =>
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
