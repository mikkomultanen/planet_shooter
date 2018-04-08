using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Controls
{
    Keyboard,
    Joystick1,
    Joystick2,
    Joystick3,
    Joystick4
}

public class PlayerController : MonoBehaviour, Damageable
{
    public Camera playerCamera;
    public Hud hud;
    public float cameraMinDistance = 55f;
    public float cameraMaxDistance = 105f;
    public GameController gameController;
    public Controls controls;
    public MeshRenderer rocket;
    public GameObject projectile;
    public Transform gunPoint;
    public GameObject missile;
    public GameObject homingMissile;
    public GameObject bomb;
    public DeathrayBeamMover deathrayBeam;
    public GameObject shield;
    public DroneController droneTemplate;
    public float maxHealth = 100;
    public float maxThrustPower = 2000f;
    public float maxSpeed = 10f;
    public float afterBurnerMaxSpeed = 30f;
    public float afterBurnerThrustPower = 6000f;
    public LayerMask laserLayerMask = ~(1 << 1);
    public float deathrayDistance = 150f;
    public float deathrayWidth = 1f;

    public ParticleSystem thruster;
    public ParticleSystem afterBurner;
    public ParticleSystem flamer;
    public ParticleSystem smoke;
    public ParticleSystem explosion;
    public LineRenderer laserRay;
    public ParticleSystem laserSparkles;
    public ParticleSystem deathrayLoading;
    public GameObject spike;

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

    private float _health;

    private float health
    {
        get
        {
            return _health;
        }
        set
        {
            hud.UpdateHealth(Mathf.RoundToInt(value));
            _health = value;
        }
    }

    private static Vector3 cameraOffset = new Vector3(0, 0, -10);
    private Rigidbody2D rb;
    private float originalDrag;
    private float gravityForceMagnitude;
    private bool isInWater = false;
    private string turnAxis;
    private string thrustAxis;
    private string fire1Button;
    private string fire2Button;
    private string fire3Button;
    private string fire4Button;
    private IDevice primaryWeapon = new MachineGunDevice();
    private IDevice secondaryWeapon;
    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        originalDrag = rb.drag;
        gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
    }
    // Use this for initialization
    void Start()
    {
        health = maxHealth;
        turnAxis = controls.ToString() + " Turn";
        thrustAxis = controls.ToString() + " Thrust";
        fire1Button = controls.ToString() + " Fire1";
        fire2Button = controls.ToString() + " Fire2";
        fire3Button = controls.ToString() + " Fire3";
        fire4Button = controls.ToString() + " Fire4";
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = true;
            rb.drag = 5;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Water")
        {
            isInWater = false;
            rb.drag = originalDrag;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.otherCollider.gameObject != shield.gameObject)
        {
            var damage = collision.relativeVelocity.sqrMagnitude / 100;
            if (collision.otherCollider.gameObject == gameObject || !spike.activeSelf)
            {
                doInternalDamage(damage);
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

    public void doDamage(float damage)
    {
        if (!shield.activeSelf)
        {
            doInternalDamage(damage);
        }
    }

    public void doInternalDamage(float damage)
    {
        float oldHealt = health;
        health -= damage;
        if (health < 0 && oldHealt >= 0)
        {
            Instantiate(explosion, transform.position, transform.rotation);
            gameObject.SetActive(false);
            gameController.playerDied();
        }
    }

    void Update()
    {
        primaryWeapon.Update(fire1Button, this);
        if (primaryWeapon.Depleted) setPrimaryWeapon(new MachineGunDevice());
        if (secondaryWeapon != null)
        {
            secondaryWeapon.Update(fire2Button, this);
            if (secondaryWeapon.Depleted) setSecondaryWeapon(null);
        }

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
        if (Vector2.Dot(rb.velocity, transform.up) < (afterBurnerOn ? afterBurnerMaxSpeed : maxSpeed))
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

        rb.AddForce(thrusters + gravity);
        rb.angularVelocity = -turn * 300f;

        var positionDelta = rb.position - previousPosition;
        previousPosition = rb.position;
        var t = positionDelta - (positionNormalized * Vector2.Dot(positionDelta, positionNormalized));
        rb.rotation -= Mathf.Atan(t.magnitude / h) * Mathf.Sign(PSEdge.Cross(t, positionNormalized)) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        Vector3 up = transform.position.normalized;
        Vector3 lookAt = up * Mathf.Clamp(transform.position.magnitude, cameraMinDistance, cameraMaxDistance);
        playerCamera.transform.position = lookAt + cameraOffset;
        playerCamera.transform.LookAt(lookAt, up);
    }

    public bool isAlive()
    {
        return health > 0;
    }

    public void respawn(Vector3 position)
    {
        health = maxHealth;
        setPrimaryWeapon(new MachineGunDevice());
        setSecondaryWeapon(null);
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.position = position;
        isInWater = false;
        rb.drag = originalDrag;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.rotation = -Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg;
        previousPosition = position;
    }

    public void repair(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    public void setPrimaryWeapon(IDevice primary)
    {
        this.primaryWeapon = primary;
        updateWeaponHud();
        resetDeviceEffects();
    }

    public void setSecondaryWeapon(IDevice secondary)
    {
        this.secondaryWeapon = secondary;
        updateWeaponHud();
        resetDeviceEffects();
    }

    public void spawnDrone(Vector2 position)
    {
        var clone = Instantiate(droneTemplate, position, transform.rotation);
        clone.player = this;
        clone.color = color;
        clone.gameObject.SetActive(true);
    }
    private void resetDeviceEffects()
    {
        var devices = new IDevice[] { primaryWeapon, secondaryWeapon };
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

    public void updateWeaponHud()
    {
        hud.UpdateWeapons(primaryWeapon, secondaryWeapon);
    }
}

public interface IDevice
{
    void Update(string button, PlayerController player);
    string HudRow();
    bool Depleted { get; }
}

public class MachineGunDevice : IDevice
{
    private float fireRate = 0.05f;
    private float nextFire = 0.0f;

    public void Update(string button, PlayerController player)
    {
        if (Input.GetButton(button) && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.projectile, player.gunPoint.position, player.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = player.GetComponent<Rigidbody2D>().velocity;
        }
    }
    public string HudRow()
    {
        return "Machine gun";
    }

    public bool Depleted
    {
        get { return false; }
    }
}

public class LaserDevice : IDevice
{
    private float energy = 30;
    private float laserDamagePerSecond = 20f;
    public void Update(string button, PlayerController player)
    {
        bool laserOn = Input.GetButton(button) && energy > 0;
        if (laserOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        bool laserSparklesOn = false;
        if (laserOn)
        {
            laserSparklesOn = updateLaserBeam(player);
        }
        if (laserOn != player.laserRay.enabled)
        {
            player.laserRay.enabled = laserOn;
        }
        if (laserSparklesOn != player.laserSparkles.isEmitting)
        {
            if (laserSparklesOn)
                player.laserSparkles.Play();
            else
                player.laserSparkles.Stop();
        }
    }
    private bool updateLaserBeam(PlayerController player)
    {
        var laserSparklesOn = false;
        Vector2 position = player.laserRay.transform.position;
        RaycastHit2D hit = Physics2D.Raycast(position, player.transform.up, 100, player.laserLayerMask);
        var laserRay = player.laserRay;
        if (hit.collider != null)
        {
            laserRay.SetPosition(1, laserRay.transform.InverseTransformPoint(hit.point));
            player.laserSparkles.transform.position = new Vector3(hit.point.x, hit.point.y, player.laserSparkles.transform.position.z);
            laserSparklesOn = true;
            Damageable damageable = hit.collider.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.doDamage(laserDamagePerSecond * Time.smoothDeltaTime);
            }
        }
        else
        {
            laserRay.SetPosition(1, Vector3.up * 100);
        }
        return laserSparklesOn;
    }
    public string HudRow()
    {
        return "Laser: " + Hud.energyToString(energy);
    }

    public bool Depleted
    {
        get { return energy <= 0; }
    }
}

public class FlamerDevice : IDevice
{
    private float energy = 10;
    public void Update(string button, PlayerController player)
    {
        bool flamerOn = Input.GetButton(button) && energy > 0;
        if (flamerOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (flamerOn != player.flamer.isEmitting)
        {
            if (flamerOn)
                player.flamer.Play();
            else
                player.flamer.Stop();
        }
    }
    public string HudRow()
    {
        return "Flamer: " + Hud.energyToString(energy);
    }
    public bool Depleted
    {
        get { return energy <= 0; }
    }
}

public class AfterBurnerDevice : IDevice
{
    private float energy = 10;
    public void Update(string button, PlayerController player)
    {
        var afterBurnerOn = Input.GetButton(button) && energy > 0;
        if (afterBurnerOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (afterBurnerOn != player.afterBurner.isEmitting)
        {
            if (afterBurnerOn)
                player.afterBurner.Play();
            else
                player.afterBurner.Stop();
        }
        if (!player.spike.activeSelf)
        {
            player.spike.SetActive(true);
        }
    }
    public string HudRow()
    {
        return "After burner: " + Hud.energyToString(energy);
    }
    public bool Depleted
    {
        get { return energy <= 0; }
    }
}

public class MissileDevice : IDevice
{
    private float fireRate = 0.5f;
    private float nextFire = 0.0f;
    private int missiles = 10;
    public void Update(string button, PlayerController player)
    {
        if (Input.GetButton(button) && Time.time > nextFire && missiles > 0)
        {
            missiles--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.missile, player.gunPoint.position, player.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = player.GetComponent<Rigidbody2D>().velocity;
            player.updateWeaponHud();
        }
    }
    public string HudRow()
    {
        return "Missiles: " + missiles;
    }
    public bool Depleted
    {
        get { return missiles <= 0; }
    }
}

public class HomingMissileDevice : IDevice
{
    private float fireRate = 1f;
    private float nextFire = 0.0f;
    private int missiles = 5;
    public void Update(string button, PlayerController player)
    {
        if (Input.GetButton(button) && Time.time > nextFire && missiles > 0)
        {
            missiles--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.homingMissile, player.gunPoint.position, player.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = player.GetComponent<Rigidbody2D>().velocity;
            player.updateWeaponHud();
        }

    }
    public string HudRow()
    {
        return "Homing missiles: " + missiles;
    }
    public bool Depleted
    {
        get { return missiles <= 0; }
    }
}

public class BombDevice : IDevice
{
    private float fireRate = 1f;
    private float nextFire = 0.0f;
    private int bombs = 5;
    public void Update(string button, PlayerController player)
    {
        if (Input.GetButton(button) && Time.time > nextFire && bombs > 0)
        {
            bombs--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.bomb, player.gunPoint.position, player.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = player.GetComponent<Rigidbody2D>().velocity;
            player.updateWeaponHud();
        }
    }
    public string HudRow()
    {
        return "Bombs: " + bombs;
    }
    public bool Depleted
    {
        get { return bombs <= 0; }
    }
}

public class DeathrayDevice : IDevice
{
    private int rays = 5;
    private float deathrayDamage = 100f;
    private float deathrayLoadingTimeMin = 0.5f;
    private float deathrayLoadingTimeMax = 10f;
    private float loaded = 0.0f;
    public void Update(string button, PlayerController player)
    {
        bool deathrayLoadingOn = false;
        if (rays > 0)
        {
            if (Input.GetButtonDown(button))
            {
                loaded = 0.0f;
            }
            if (Input.GetButton(button))
            {
                loaded += Time.deltaTime;
                var loadedNormalized = Mathf.Clamp01(loaded / deathrayLoadingTimeMax);
                var emission = player.deathrayLoading.emission;
                emission.rateOverTime = Mathf.Max(10, loadedNormalized * 100);
                player.doInternalDamage(loadedNormalized * Time.deltaTime);
                deathrayLoadingOn = true;
            }
            if (Input.GetButtonUp(button))
            {
                if (loaded > deathrayLoadingTimeMin)
                {
                    rays--;
                    fireDeathray(player);
                    player.updateWeaponHud();
                }
                loaded = 0.0f;
            }
        }
        if (deathrayLoadingOn != player.deathrayLoading.isEmitting)
        {
            if (deathrayLoadingOn)
                player.deathrayLoading.Play();
            else
                player.deathrayLoading.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void fireDeathray(PlayerController player)
    {
        var loadedNormalized = Mathf.Clamp01(loaded / deathrayLoadingTimeMax);
        var distance = loadedNormalized * player.deathrayDistance;
        var damage = loadedNormalized * deathrayDamage;
        var start = player.gunPoint.position;
        var direction = player.transform.up * distance;
        var center = start + direction * 0.5f;
        var angle = player.GetComponent<Rigidbody2D>().rotation;
        Collider2D[] colliders = Physics2D.OverlapCapsuleAll(center, new Vector2(player.deathrayWidth, distance + player.deathrayWidth), CapsuleDirection2D.Vertical, angle);
        Damageable damageable;
        TerrainPiece terrainPiece;
        foreach (Collider2D coll in colliders)
        {
            if (coll.gameObject == player.gameObject)
            {
                continue;
            }
            damageable = coll.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.doDamage(damage);
            }
            terrainPiece = coll.GetComponent<TerrainPiece>();
            if (terrainPiece != null)
            {
                terrainPiece.destroyTerrain(start, direction, player.deathrayWidth);
            }
        }
        var clone = GameObject.Instantiate(player.deathrayBeam, center, player.gunPoint.rotation);
        clone.radius = distance * 0.5f;
    }
    public string HudRow()
    {
        return "Deathray: " + rays;
    }
    public bool Depleted
    {
        get { return rays <= 0; }
    }
}

public class ShieldDevice : IDevice
{
    private float energy = 10;
    public void Update(string button, PlayerController player)
    {
        bool shieldOn = Input.GetButton(button) && energy > 0;
        if (shieldOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (shieldOn != player.shield.activeSelf)
        {
            player.shield.SetActive(shieldOn);
        }
    }
    public string HudRow()
    {
        return "Shield: " + Hud.energyToString(energy);
    }
    public bool Depleted
    {
        get { return energy <= 0; }
    }
}