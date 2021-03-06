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
    public ParticleSystem deathray;
    public GameObject deathrayField;
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

    protected virtual void Awake() {
        originalHealth = health;
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
        var afterBurnerOff = !devices.Any(d => d is AfterBurnerDevice);
        var deathrayOff = !devices.Any(d => d is DeathrayDevice);
        var shieldOff = !devices.Any(d => d is ShieldDevice);
        if (afterBurnerOff)
        {
            afterBurner.Stop();
            spike.SetActive(false);
        }
        if (deathrayOff) 
        {
            deathray.Stop();
            deathrayField.SetActive(false);
        }
        if (shieldOff) shield.SetActive(false);
    }

    public void repair(float amount)
    {
        health = Mathf.Min(health + amount, originalHealth);
    }

    public virtual string Name { get; }
}
