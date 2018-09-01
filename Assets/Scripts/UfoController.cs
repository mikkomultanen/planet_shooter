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
    [Range(0.0f, 10f)]
    public float waterDrag = 2.5f;

    public Transform targeting;
    public ParticleSystem thruster;
    public ParticleSystem smoke;

    public float afterBurnerMaxSpeed = 25f;
    public float afterBurnerThrustPower = 900f;

    private string xAxis;
    private string yAxis;

    private Rigidbody2D rb;
    private float gravityForceMagnitude;

    protected override void Awake() {
        base.Awake();
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
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
        var localDirection = Vector2.ClampMagnitude(new Vector2(x, y), 1);
        Vector2 direction = transform.TransformDirection(localDirection);
        if (direction.sqrMagnitude > 0.01f) {
            targeting.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
        }

        var smokeEmission = smoke.emission;
        bool thrustersOn = Input.GetAxis(yAxis) > 0f;
        if (thrustersOn != thruster.isEmitting)
        {
            if (thrustersOn)
                thruster.Play();
            else
                thruster.Stop();
        }

        bool movingHorizontally = Input.GetAxis(xAxis) != 0f;
        if (thrustersOn || movingHorizontally)
            smokeEmission.rateOverDistanceMultiplier = 5.0f;
        else
            smokeEmission.rateOverDistanceMultiplier = 0.0f;

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

    void FixedUpdate()
    {
        float x = Input.GetAxis(xAxis);
        float y = Input.GetAxis(yAxis);
        var localDirection = Vector2.ClampMagnitude(new Vector2(x, y), 1);
        Vector2 direction = transform.TransformDirection(localDirection);
        
        var h = rb.position.magnitude;
        var positionNormalized = rb.position.normalized;
        float athmosphereCoefficient = Mathf.Clamp((120f - h) / 20f, 0f, 1f);
        float floatingAndGravityForceMagnitude = (IsInWater ? -1.2f : (1f - athmosphereCoefficient)) * gravityForceMagnitude;
        float thursterForceMagnitude = 0f;
        var afterBurnerOn = afterBurner.isEmitting;
        var forwardSpeed = Vector2.Dot(rb.velocity, direction.normalized);
        if (forwardSpeed < (afterBurnerOn ? afterBurnerMaxSpeed : maxSpeed))
        {
            thursterForceMagnitude = athmosphereCoefficient * (afterBurnerOn ? afterBurnerThrustPower : maxThrustPower);
            if (IsInWater)
            {
                thursterForceMagnitude = Mathf.Min(thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
            }
        }

        Vector2 gravity = positionNormalized * floatingAndGravityForceMagnitude;
        Vector2 thrusters = direction * thursterForceMagnitude;

        var drag = IsInWater ? waterDrag : airDrag;
        rb.AddForce(-rb.velocity * rb.velocity.magnitude * drag);

        rb.AddForce(thrusters + gravity);
        rb.rotation = -Mathf.Atan2(rb.position.x, rb.position.y) * Mathf.Rad2Deg;
    }
}
