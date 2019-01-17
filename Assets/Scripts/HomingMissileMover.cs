using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HomingMissileMover : MissileMover
{
    public float homingRadius = 10f;
    public LayerMask homingLayerMask = 1;

    private Transform target = null;

    new void FixedUpdate()
    {
        if (target != null)
        {
            var targetDirection = target.position - transform.position;
            var angle = Vector2.SignedAngle(transform.up, targetDirection);
            rb.angularVelocity = Mathf.Clamp(angle / 15f, -1, 1) * 300f;
        }

        base.FixedUpdate();
    }

    new void LateUpdate()
    {
        UpdateTarget();
        base.LateUpdate();
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
