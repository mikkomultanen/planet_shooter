using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathrayField : MonoBehaviour
{
    public float damagePerSecond = 100;
    public float maxDistance = 10;

    private void OnTriggerStay2D(Collider2D other) {
        var damageable = other.GetComponent<Damageable>();
        if (damageable != null) {
            Vector2 distance = transform.position - other.transform.position;
            damageable.doDamage(damagePerSecond * Time.deltaTime * (1 - Mathf.Clamp01(distance.magnitude / maxDistance)));
        }
    }
}
