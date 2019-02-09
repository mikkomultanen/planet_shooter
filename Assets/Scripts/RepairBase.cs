using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairBase : MonoBehaviour
{

    public float maxHorizontalSpeed = 2f;
    public float maxHorizontalThrustPower = 100f;
    public float maxVerticalSpeed = 5f;
    public float maxVerticalThrustPower = 300f;
    public ParticleSystem thruster1;
    public ParticleSystem thruster2;

    private Rigidbody2D rb;
    private float gravityForceMagnitude;
    private Vector2 targetPosition;
    private bool thrustersOn;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }

    void FixedUpdate()
    {
        var h = rb.position.magnitude;
        var positionNormalized = rb.position.normalized;
        Vector2 gravity = positionNormalized * gravityForceMagnitude;

        var direction = targetPosition - rb.position;

        Vector2 thrusters = Vector2.zero;
        var verticalDistance = Vector2.Dot(direction, positionNormalized);
        thrustersOn = verticalDistance > 0;
        if (thrustersOn && Vector2.Dot(rb.velocity, positionNormalized) < maxVerticalSpeed)
        {
            thrusters += positionNormalized * maxVerticalThrustPower * Mathf.Clamp01(verticalDistance);
        }

        var tangent = new Vector2(positionNormalized.y, -positionNormalized.x);
        float horizontalSpeed = Mathf.Abs(Vector2.Dot(rb.velocity, tangent));
        if (horizontalSpeed < maxHorizontalSpeed)
        {
            float horizontalDistance = Vector2.Dot(direction, tangent);
            thrusters += tangent * Mathf.Clamp(horizontalDistance, -1, 1) * maxHorizontalThrustPower;
        }

        rb.AddForce(thrusters + gravity);

        var angle = Vector2.SignedAngle(transform.up, rb.position);
        rb.angularVelocity = Mathf.Clamp(angle / 15f, -1, 1) * 180f;
    }

    private void Update() {
        if (thrustersOn != thruster1.isEmitting)
        {
            if (thrustersOn) {
                thruster1.Play();
                thruster2.Play();
            }
            else {
                thruster1.Stop();
                thruster2.Stop();
            }
        }
    }
    public void respawn(Vector2 position)
    {
        transform.position = position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.rotation = -Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg;
        targetPosition = position;
    }
}
