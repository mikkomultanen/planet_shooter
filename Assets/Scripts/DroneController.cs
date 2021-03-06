﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneController : Explosive
{

    public Rigidbody2D ship;
    public float maxHorizontalSpeed = 5f;
    public float maxHorizontalThrustPower = 10f;
    public float maxVerticalSpeed = 10f;
    public float maxVerticalThrustPower = 30f;
    public float minDistance = 4f;
    public float targetRadius = 10f;
    public LayerMask targetLayerMask = 0;
	public float gunPointRadius = 1f;

	public MeshRenderer body;
    public ParticleSystem thruster;
    public LineRenderer laserRay;
    public ParticleSystem laserSparkles;
    public LayerMask laserLayerMask = ~(1 << 1);

    private Color _color;

    public Color color
    {
        get
        {
            return _color;
        }
        set
        {
            body.material.color = value;
            _color = value;
        }
    }

    private Rigidbody2D rb;
    private float gravityForceMagnitude;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }

    private void Update()
    {
        var direction = Direction();
        var thrustersOn = Vector2.Dot(direction, rb.position) > 0;
        if (thrustersOn != thruster.isEmitting)
        {
            if (thrustersOn)
                thruster.Play();
            else
                thruster.Stop();
        }

        if (ship) {
            UpdateLaser(GetTarget());
        } else {
            explode();
        }
    }
    void FixedUpdate()
    {
        var h = rb.position.magnitude;
        var positionNormalized = rb.position.normalized;
        float athmosphereCoefficient = Mathf.Clamp((120f - h) / 20f, 0f, 1f);
        Vector2 gravity = positionNormalized * gravityForceMagnitude;

        var direction = Direction();

        Vector2 thrusters = Vector2.zero;
        if (Vector2.Dot(direction, positionNormalized) > 0 && Vector2.Dot(rb.velocity, positionNormalized) < maxVerticalSpeed)
        {
            var verticalForceMagnitude = athmosphereCoefficient * maxVerticalThrustPower;
            thrusters += positionNormalized * verticalForceMagnitude;
        }

        var tangent = new Vector2(positionNormalized.y, -positionNormalized.x);
        float horizontalSpeed = Mathf.Abs(Vector2.Dot(rb.velocity, tangent));
        if (horizontalSpeed < maxHorizontalSpeed)
        {
            var horizontalForceMagnitude = athmosphereCoefficient * maxHorizontalThrustPower;
            thrusters += tangent * Mathf.Sign(Vector2.Dot(direction, tangent)) * horizontalForceMagnitude;
        }

        rb.AddForce(thrusters + gravity);
        rb.rotation = -Mathf.Atan2(rb.position.x, rb.position.y) * Mathf.Rad2Deg;
    }

    private Vector2 Direction()
    {
        if (ship == null) return Vector2.zero;

        var direction = ship.position - rb.position;
        if (direction.magnitude < minDistance)
        {
            direction = -direction;
        }
        return direction;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.otherCollider.gameObject == gameObject)
        {
            doDamage(collision.relativeVelocity.sqrMagnitude / 100);
        }
    }

    private Transform GetTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(rb.position, targetRadius, targetLayerMask);
        return colliders.Aggregate((Transform)null, (memo, c) =>
        {
            if (!IsEnemy(c.gameObject))
            {
                return memo;
            }
            else if (memo == null)
            {
                return c.transform;
            }
            else
            {
                var memoSqrD = sqrDistance(memo);
                var cSqrD = sqrDistance(c.transform);
                return memoSqrD < cSqrD ? memo : c.transform;
            }
        });
    }

    private bool IsEnemy(GameObject other)
    {
        if (other == ship.gameObject || other == gameObject) return false;
        var drone = other.GetComponent<DroneController>();
        if (drone) {
            return drone.ship != ship;
        }
        return true;
    }

    private float laserDamagePerSecond = 20f;
    private float energy = 0f;
    private bool loadingLaser = true;
    private void UpdateLaser(Transform target)
    {
        bool laserOn = false;
        if (target != null && !loadingLaser && energy > 0f)
        {
            if (raycastLaserBeam(target))
            {
                laserOn = true;
                energy -= Time.deltaTime;
                if (energy < 0f)
                {
                    loadingLaser = true;
                    energy = 0f;
                }
            }
        }
        else if (energy < 0.5f)
        {
            energy += Time.deltaTime;
        }
        else
        {
            loadingLaser = false;
            energy = 0.5f;
        }
        if (laserOn != laserRay.enabled)
        {
            laserRay.enabled = laserOn;
        }
        if (laserOn != laserSparkles.isEmitting)
        {
            if (laserOn)
                laserSparkles.Play();
            else
                laserSparkles.Stop();
        }
    }

    private float sqrDistance(Transform t)
    {
        Vector2 p = t.position;
        return (rb.position - p).sqrMagnitude;
    }

    private bool raycastLaserBeam(Transform target)
    {
        Vector2 position = transform.position;
        Vector2 direction = ((Vector2)target.position - position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(position + (direction * gunPointRadius), direction, 100, laserLayerMask);
        if (hit.collider != null)
        {
            laserRay.SetPosition(1, laserRay.transform.InverseTransformPoint(hit.point));
            laserSparkles.transform.position = new Vector3(hit.point.x, hit.point.y, laserSparkles.transform.position.z);
            Damageable damageable = hit.collider.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.doDamage(laserDamagePerSecond * Time.smoothDeltaTime);
            }
			return true;
        }
        return false;
    }
}
