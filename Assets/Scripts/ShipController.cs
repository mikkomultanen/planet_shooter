using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipController : Explosive, Repairable
{
    public MeshRenderer shipBody;
    public Transform gunPoint;
    public GameObject shield;

    public ParticleSystem afterBurner;
    public ParticleSystem flamer;
    public LineRenderer laserRay;
    public ParticleSystem laserSparkles;
    public ParticleSystem deathrayLoading;
    public GameObject spike;

    [HideInInspector]
    public PlayerController playerController;

    private Color _color;

    public Color color
    {
        get
        {
            return _color;
        }
        set
        {
            shipBody.material.color = value;
            _color = value;
        }
    }

    private float originalHealth;
    private bool isInWater = false;

    public bool IsInWater 
    {
        get
        {
            return isInWater;
        }
    }

    protected virtual void Awake() {
        originalHealth = health;
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

    public virtual string Name { get; }
}
