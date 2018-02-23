using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Controls
{
    Keyboard,
    Joystick1,
    Joystick2,
    Joystick3,
    Joystick4
}

public enum PrimaryWeapon
{
    MachineGun,
    Flamer,
    Laser
}

public enum SecondaryWeapon
{
    None,
    Missiles,
    HomingMissiles,
    Bombs
}

public struct WeaponState
{
    public PrimaryWeapon primary;
    public float primaryEnergy;
    public SecondaryWeapon secondary;
    public int secondaryAmmunition;

    public WeaponState(PrimaryWeapon primary, float primaryEnergy, SecondaryWeapon secondary, int secondaryAmmunition)
    {
        this.primary = primary;
        this.primaryEnergy = primaryEnergy;
        this.secondary = secondary;
        this.secondaryAmmunition = secondaryAmmunition;
    }
}

public class PlayerController : MonoBehaviour, Damageable
{
    public Camera playerCamera;
    public Hud hud;
    public float cameraMinDistance = 55f;
    public float cameraMaxDistance = 105f;
    public GameController gameController;
    public Controls controls;
    public GameObject projectile;
    public Transform gunPoint;
    public GameObject missile;
    public GameObject homingMissile;
    public GameObject bomb;
    public float maxHealth = 100;
    public float maxThrustPower = 2000f;
    public float maxSpeed = 10f;
    public float fireRate = 0.2f;
    public float secondaryFireRate = 1f;
    public float laserDamagePerSecond = 20f;

    public ParticleSystem thruster;
    public ParticleSystem flamer;
    public ParticleSystem smoke;
    public ParticleSystem explosion;
    public LineRenderer laserRay;
    public ParticleSystem laserSparkles;

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
    private float nextFire = 0.0f;
    private float nextSecondaryFire = 0.0f;
    private int laserLayerMask = ~(1 << 1);
    private float gravityForceMagnitude;
    private bool isInWater = false;
    private string turnAxis;
    private string thrustAxis;
    private string fire1Button;
    private string fire2Button;
    private string fire3Button;
    private string fire4Button;
    private WeaponState _weaponState = new WeaponState(PrimaryWeapon.MachineGun, 0, SecondaryWeapon.None, 0);
    private WeaponState weaponState
    {
        get { return _weaponState; }
        set
        {
            _weaponState = value;
            hud.UpdateWeapons(value);
        }
    }

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
        doDamage(collision.relativeVelocity.sqrMagnitude / 100);
    }

    public void doDamage(float damage)
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
        bool flamerOn = false;
        bool laserOn = false;
        switch (weaponState.primary)
        {
            case PrimaryWeapon.MachineGun:
                if (Input.GetButton(fire1Button) && Time.time > nextFire)
                {
                    nextFire = Time.time + fireRate;
                    GameObject clone = Instantiate(projectile, gunPoint.position, gunPoint.rotation) as GameObject;
                    clone.GetComponent<Rigidbody2D>().velocity = rb.velocity;
                }
                break;
            case PrimaryWeapon.Flamer:
                flamerOn = Input.GetButton(fire1Button) && weaponState.primaryEnergy > 0;
                break;
            case PrimaryWeapon.Laser:
                laserOn = Input.GetButton(fire1Button) && weaponState.primaryEnergy > 0;
                break;
        }
        switch (weaponState.secondary)
        {
            case SecondaryWeapon.Missiles:
                if (Input.GetButton(fire2Button) && Time.time > nextSecondaryFire && weaponState.secondaryAmmunition > 0)
                {
                    var oldState = weaponState;
                    weaponState = new WeaponState(oldState.primary, oldState.primaryEnergy, oldState.secondary, oldState.secondaryAmmunition - 1);
                    nextSecondaryFire = Time.time + secondaryFireRate;
                    GameObject clone = Instantiate(missile, gunPoint.position, gunPoint.rotation) as GameObject;
                    clone.GetComponent<Rigidbody2D>().velocity = rb.velocity;
                }
                break;
            case SecondaryWeapon.HomingMissiles:
                if (Input.GetButton(fire2Button) && Time.time > nextSecondaryFire && weaponState.secondaryAmmunition > 0)
                {
                    var oldState = weaponState;
                    weaponState = new WeaponState(oldState.primary, oldState.primaryEnergy, oldState.secondary, oldState.secondaryAmmunition - 1);
                    nextSecondaryFire = Time.time + secondaryFireRate;
                    GameObject clone = Instantiate(homingMissile, gunPoint.position, gunPoint.rotation) as GameObject;
                    clone.GetComponent<Rigidbody2D>().velocity = rb.velocity;
                }
                break;
            case SecondaryWeapon.Bombs:
                if (Input.GetButton(fire2Button) && Time.time > nextSecondaryFire && weaponState.secondaryAmmunition > 0)
                {
                    var oldState = weaponState;
                    weaponState = new WeaponState(oldState.primary, oldState.primaryEnergy, oldState.secondary, oldState.secondaryAmmunition - 1);
                    nextSecondaryFire = Time.time + secondaryFireRate;
                    GameObject clone = Instantiate(bomb, gunPoint.position, gunPoint.rotation) as GameObject;
                    clone.GetComponent<Rigidbody2D>().velocity = rb.velocity;
                }
                break;
        }
        if (flamerOn || laserOn)
        {
            var oldState = weaponState;
            weaponState = new WeaponState(oldState.primary, oldState.primaryEnergy - Time.deltaTime, oldState.secondary, oldState.secondaryAmmunition);
            if (weaponState.primaryEnergy <= 0)
            {
                setPrimaryWeapon(PrimaryWeapon.MachineGun, 0);
            }
        }
        if (flamerOn != flamer.isEmitting)
        {
            if (flamerOn)
                flamer.Play();
            else
                flamer.Stop();
        }
        bool laserSparklesOn = false;
        if (laserOn)
        {
            Vector2 position = laserRay.transform.position;
            RaycastHit2D hit = Physics2D.Raycast(position, transform.up, 100, laserLayerMask);
            if (hit.collider != null)
            {
                laserRay.SetPosition(1, laserRay.transform.InverseTransformPoint(hit.point));
                laserSparkles.transform.position = hit.point;
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
        }
        if (laserOn != laserRay.enabled)
        {
            laserRay.enabled = laserOn;
        }
        if (laserSparklesOn != laserSparkles.isEmitting)
        {
            if (laserSparklesOn)
                laserSparkles.Play();
            else
                laserSparkles.Stop();
        }

        var smokeEmission = smoke.emission;
        bool thrustersOn = Input.GetAxis(thrustAxis) > 0f;
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

    void FixedUpdate()
    {
        float turn = Input.GetAxis(turnAxis);

        float floatingAndGravityForceMagnitude = (isInWater ? -1.2f : 1f) * gravityForceMagnitude;
        float thursterForceMagnitude = 0f;
        if (Vector2.Dot(rb.velocity, transform.up) < maxSpeed)
        {
            float thrust = Mathf.Max(Input.GetAxis(thrustAxis), 0f);
            float athmosphereCoefficient = Mathf.Clamp((120f - rb.position.magnitude) / 20f, 0f, 1f);
            thursterForceMagnitude = thrust * athmosphereCoefficient * maxThrustPower;
            if (isInWater)
            {
                thursterForceMagnitude = Mathf.Min(thursterForceMagnitude, 0.8f * floatingAndGravityForceMagnitude);
            }
        }

        Vector2 gravity = rb.position.normalized * floatingAndGravityForceMagnitude;
        Vector2 thrusters = transform.up * thursterForceMagnitude;

        rb.AddForce(thrusters + gravity);
        rb.angularVelocity = -turn * 300f;
    }

    private Vector2 previousPosition;
    void LateUpdate()
    {
        Vector3 up = transform.position.normalized;
        Vector3 lookAt = up * Mathf.Clamp(transform.position.magnitude, cameraMinDistance, cameraMaxDistance);
        playerCamera.transform.position = lookAt + cameraOffset;
        playerCamera.transform.LookAt(lookAt, up);

        var h = rb.position.magnitude;
        var positionDelta = rb.position - previousPosition;
        previousPosition = rb.position;
        var positionNormalized = rb.position.normalized;
        var t = positionDelta - (positionNormalized * Vector2.Dot(positionDelta, positionNormalized));
        rb.rotation -= Mathf.Atan(t.magnitude / h) * Mathf.Sign(PSEdge.Cross(t, positionNormalized)) * Mathf.Rad2Deg;
    }

    public bool isAlive()
    {
        return health > 0;
    }

    public void respawn(Vector3 position)
    {
        health = maxHealth;
        setPrimaryWeapon(PrimaryWeapon.MachineGun, 0);
        setSecondaryWeapon(SecondaryWeapon.None, 0);
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

    public void setPrimaryWeapon(PrimaryWeapon primary, float energy)
    {
        var oldState = weaponState;
        weaponState = new WeaponState(primary, energy, oldState.secondary, oldState.secondaryAmmunition);
    }

    public void setSecondaryWeapon(SecondaryWeapon secondary, int ammunition)
    {
        var oldState = weaponState;
        weaponState = new WeaponState(oldState.primary, oldState.primaryEnergy, secondary, ammunition);
    }
}
