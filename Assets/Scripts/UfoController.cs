using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UfoController : ShipController
{
    public float maxThrustPower = 300f;
    public float maxSpeed = 8f;
    [Range(0.0f, 10f)]
    public float airDrag = 0.5f;

    public Transform disc;
    public Transform targeting;
    public ParticleSystem thruster;
    public ParticleSystem smoke;

    public float afterBurnerMaxSpeed = 25f;
    public float afterBurnerThrustPower = 900f;

    private string xAxis;
    private string yAxis;

    private Rigidbody2D rb;

    protected override void Awake() {
        base.Awake();
        rb = gameObject.GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        xAxis = playerController.controls.ToString() + " Turn";
        yAxis = playerController.controls.ToString() + " Thrust";
    }

    void Update()
    {
        float x = Input.GetAxis(xAxis);
        float y = Input.GetAxis(yAxis);
        bool thrustersOn = Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f;
        var smokeEmission = smoke.emission;
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

        disc.Rotate(0, x * 360 * Time.deltaTime, 0);
    }

    void FixedUpdate()
    {
        float x = Input.GetAxis(xAxis);
        float y = Input.GetAxis(yAxis);
        var h = rb.position.magnitude;
        var positionNormalized = rb.position.normalized;

        Vector2 direction = Vector2.ClampMagnitude(positionNormalized * y - Vector2.Perpendicular(positionNormalized) * x, 1);
        if (direction.sqrMagnitude > 0.01f) {
            var targetAngleDiff = Vector2.SignedAngle(targeting.up, direction);
            var maxRotation = 300f * Time.deltaTime;
            targeting.Rotate(0, 0 , Mathf.Clamp(targetAngleDiff, -maxRotation, maxRotation));
        }

        float athmosphereCoefficient = Mathf.Clamp((120f - h) / 20f, 0f, 1f);
        float thursterForceMagnitude = 0f;
        var afterBurnerOn = afterBurner.isEmitting;
        var forwardSpeed = Vector2.Dot(rb.velocity, direction.normalized);
        if (forwardSpeed < (afterBurnerOn ? afterBurnerMaxSpeed : maxSpeed))
        {
            thursterForceMagnitude = athmosphereCoefficient * (afterBurnerOn ? afterBurnerThrustPower : maxThrustPower);
        }

        Vector2 thrusters = direction * thursterForceMagnitude;

        rb.AddForce(-rb.velocity * rb.velocity.magnitude * airDrag);

        rb.AddForce(thrusters);

        var angle = Vector2.SignedAngle(transform.up, rb.position) - x * 15f;
        rb.angularVelocity = Mathf.Clamp(angle / 15f, -1, 1) * 180f;
    }

    public override string Name {
        get {
            return "Ufo";
        }
    }
}
