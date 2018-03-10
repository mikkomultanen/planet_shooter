﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{

    public ParticleSystem splashTemplate;
    public float splashThreshold = 1f;

    private CircleCollider2D water;
    private float waterSurfaceMagnitude;

    private void Awake()
    {
        water = GetComponent<CircleCollider2D>();
        waterSurfaceMagnitude = water.radius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var positionNormalized = rb.position.normalized;
            var massScale = Mathf.Clamp(rb.mass / 10f, 0.1f, 1f);
            var ySpeed = Mathf.Abs(Vector2.Dot(rb.velocity, positionNormalized)) * massScale;
            if (ySpeed > splashThreshold)
            {
                var splash = Instantiate(splashTemplate, positionNormalized * waterSurfaceMagnitude, Quaternion.Euler(0, 0, -Mathf.Atan2(rb.position.x, rb.position.y) * Mathf.Rad2Deg));
                splash.trigger.SetCollider(0, water);
                var emission = splash.emission;
                emission.rateOverTimeMultiplier *= massScale;
                var main = splash.main;
                var startSpeed = Mathf.Clamp(ySpeed, 0, 10);
                main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.5f, startSpeed);
            }
        }
    }
}