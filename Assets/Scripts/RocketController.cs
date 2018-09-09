using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RocketController : ShipController
{
    public float maxThrustPower = 300f;
    public float maxSpeed = 10f;
    [Range(0.0f, 10f)]
    public float airDrag = 0.5f;
    [Range(0.0f, 10f)]
    public float waterDrag = 2.5f;
    [Range(0.0f, 10f)]
    public float orthogonalDrag = 2f;

    public ParticleSystem thruster;
    public ParticleSystem smoke;

    public float afterBurnerMaxSpeed = 30f;
    public float afterBurnerThrustPower = 900f;

    private string turnAxis;
    private string thrustAxis;

    private Rigidbody2D rb;
    private float gravityForceMagnitude;

    protected override void Awake() {
        base.Awake();
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
        previousPosition = rb.position;
    }

    void Start()
    {
        turnAxis = playerController.controls.ToString() + " Turn";
        thrustAxis = playerController.controls.ToString() + " Thrust";
    }

    void Update()
    {
        var smokeEmission = smoke.emission;
        bool thrustersOn = Input.GetAxis(thrustAxis) > 0f && !afterBurner.isEmitting;
        if (thrustersOn != thruster.isEmitting)
        {
            if (thrustersOn)
            {
                thruster.Play();
                smokeEmission.rateOverDistanceMultiplier = 5.0f;
            }
            else
            {
                thruster.Stop();
                smokeEmission.rateOverDistanceMultiplier = 0.0f;
            }
        }
        bool lowHealth = this.health < 30;
        smokeEmission.rateOverTimeMultiplier = 10 * Mathf.Clamp01(1.0f - this.health / 30);
        if (lowHealth != smoke.isEmitting)
        {
            if (lowHealth)
                smoke.Play();
            else
                smoke.Stop();
        }
    }

    private Vector2 previousPosition;
    void FixedUpdate()
    {
        float turn = Input.GetAxis(turnAxis);

        var h = rb.position.magnitude;
        var positionNormalized = rb.position.normalized;
        float floatingAndGravityForceMagnitude = (IsInWater ? -1.2f : 1f) * gravityForceMagnitude;
        float thursterForceMagnitude = 0f;
        var afterBurnerOn = afterBurner.isEmitting;
        var forwardSpeed = Vector2.Dot(rb.velocity, transform.up);
        if (forwardSpeed < (afterBurnerOn ? afterBurnerMaxSpeed : maxSpeed))
        {
            float thrust = Mathf.Max(Input.GetAxis(thrustAxis), 0f);
            float athmosphereCoefficient = Mathf.Clamp((120f - h) / 20f, 0f, 1f);
            thursterForceMagnitude = athmosphereCoefficient * (afterBurnerOn ? afterBurnerThrustPower : thrust * maxThrustPower);
            if (IsInWater)
            {
                thursterForceMagnitude = Mathf.Min(thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
            }
        }

        Vector2 gravity = positionNormalized * floatingAndGravityForceMagnitude;
        Vector2 thrusters = transform.up * thursterForceMagnitude;

        var drag = IsInWater ? waterDrag : airDrag;
        var forwardVelocity = forwardSpeed * (Vector2)transform.up;
        var orthogonalVelocity = rb.velocity - forwardVelocity;
        rb.AddForce(-orthogonalVelocity * orthogonalVelocity.magnitude * (drag + orthogonalDrag));
        rb.AddForce(-forwardVelocity * forwardVelocity.magnitude * drag);

        rb.AddForce(thrusters + gravity);
        rb.angularVelocity = -turn * 300f;

        var positionDelta = rb.position - previousPosition;
        previousPosition = rb.position;
        var t = positionDelta - (positionNormalized * Vector2.Dot(positionDelta, positionNormalized));
        rb.rotation -= Mathf.Atan(t.magnitude / h) * Mathf.Sign(PSEdge.Cross(t, positionNormalized)) * Mathf.Rad2Deg;
    }

    public override string Name {
        get {
            return "Rocket";
        }
    }
}
