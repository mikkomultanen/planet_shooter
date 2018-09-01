using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RocketController : Explosive, Repairable
{
    public MeshRenderer rocket;
    public Transform gunPoint;
    public GameObject shield;
    public float maxThrustPower = 300f;
    public float maxSpeed = 10f;
    [Range(0.0f, 10f)]
    public float airDrag = 0.5f;
    [Range(0.0f, 10f)]
    public float waterDrag = 2.5f;
    [Range(0.0f, 10f)]
    public float orthogonalDrag = 2f;

    public ParticleSystem thruster;
    public ParticleSystem afterBurner;
    public ParticleSystem flamer;
    public ParticleSystem smoke;
    public LineRenderer laserRay;
    public ParticleSystem laserSparkles;
    public ParticleSystem deathrayLoading;
    public GameObject spike;

    public float afterBurnerMaxSpeed = 30f;
    public float afterBurnerThrustPower = 900f;

    [HideInInspector]
    public PlayerController playerController;
    private string turnAxis;
    private string thrustAxis;

    private Color _color;

    public Color color
    {
        get
        {
            return _color;
        }
        set
        {
            rocket.material.color = value;
            _color = value;
        }
    }

    private float originalHealth;
    private Rigidbody2D rb;
    private float gravityForceMagnitude;
    private bool isInWater = false;

    private void Awake() {
        originalHealth = health;
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
        previousPosition = rb.position;
    }

    void Start()
    {
        turnAxis = playerController.controls.ToString() + " Turn";
        thrustAxis = playerController.controls.ToString() + " Thrust";
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.otherCollider.gameObject != shield.gameObject)
        {
            var damage = collision.relativeVelocity.sqrMagnitude / 100;
            if (collision.otherCollider.gameObject == gameObject || !spike.activeSelf)
            {
                base.doDamage(damage);
            }
            else
            {
                // Spike visible and collided with the bumper
                Damageable damageable = collision.collider.GetComponent<Damageable>();
                if (damageable != null)
                {
                    damageable.doDamage(damage);
                }
            }
        }
    }

    public override void doDamage(float damage)
    {
        if (!shield.activeSelf)
        {
            base.doDamage(damage);
        }
    }

    protected override void afterExposion() 
    {
        base.afterExposion();
        playerController.gameController.playerDied();
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
        float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
        float thursterForceMagnitude = 0f;
        var afterBurnerOn = afterBurner.isEmitting;
        var forwardSpeed = Vector2.Dot(rb.velocity, transform.up);
        if (forwardSpeed < (afterBurnerOn ? afterBurnerMaxSpeed : maxSpeed))
        {
            float thrust = Mathf.Max(Input.GetAxis(thrustAxis), 0f);
            float athmosphereCoefficient = Mathf.Clamp((120f - h) / 20f, 0f, 1f);
            thursterForceMagnitude = athmosphereCoefficient * (afterBurnerOn ? afterBurnerThrustPower : thrust * maxThrustPower);
            if (isInWater)
            {
                thursterForceMagnitude = Mathf.Min(thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
            }
        }

        Vector2 gravity = positionNormalized * floatingAndGravityForceMagnitude;
        Vector2 thrusters = transform.up * thursterForceMagnitude;

        var drag = isInWater ? waterDrag : airDrag;
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

    public void resetDeviceEffects(IDevice[] devices)
    {
        var flamerOff = !devices.Any(d => d is FlamerDevice);
        var afterBurnerOff = !devices.Any(d => d is AfterBurnerDevice);
        var laserOff = !devices.Any(d => d is LaserDevice);
        var deathrayOff = !devices.Any(d => d is DeathrayDevice);
        var shieldOff = !devices.Any(d => d is ShieldDevice);
        if (flamerOff) flamer.Stop();
        if (afterBurnerOff)
        {
            afterBurner.Stop();
            spike.SetActive(false);
        }
        if (laserOff)
        {
            laserSparkles.Stop();
            laserRay.enabled = false;
        }
        if (deathrayOff) deathrayLoading.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (shieldOff) shield.SetActive(false);
    }

    public void repair(float amount)
    {
        health = Mathf.Min(health + amount, originalHealth);
    }
}
