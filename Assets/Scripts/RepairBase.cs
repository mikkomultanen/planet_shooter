using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairBase : MonoBehaviour
{

    public TerrainMesh terrain;
    private Rigidbody2D rb;
    private float gravityForceMagnitude;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }

    private float targetMagnitude;
    void FixedUpdate()
    {
        var forceMagnitude = Mathf.Clamp((rb.position.magnitude - targetMagnitude), -3f, 1f) * gravityForceMagnitude;
        Vector2 force = rb.position.normalized * forceMagnitude;
        rb.AddForce(force);
        rb.rotation = -Mathf.Atan2(rb.position.x, rb.position.y) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        targetMagnitude = terrain.upperCaveCenterMagnitude(rb.position);
    }

    public void respawn(Vector2 position)
    {
        transform.position = position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.rotation = -Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg;
        targetMagnitude = terrain.upperCaveCenterMagnitude(position);
    }
}
