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

[RequireComponent(typeof(Camera))]
public class PlayerController : MonoBehaviour
{
    public GameController gameController;
    public Hud hud;
    public RepairBase repairBase;
    public float cameraMinDistance = 55f;
    public float cameraMaxDistance = 105f;
    public Controls controls;
    public LayerMask laserLayerMask = ~(1 << 1);
    public float deathrayDistance = 50f;
    public float deathrayWidth = 1f;

    public List<ShipController> shipTemplates;
    public GameObject projectileTemplate;
    public GameObject missileTemplate;
    public GameObject homingMissileTemplate;
    public GameObject bombTemplate;
    public DeathrayBeamMover deathrayBeamTemplate;
    public DroneController droneTemplate;

    public Color color;

    private static Vector3 cameraOffset = new Vector3(0, 0, -10);
    private string turnAxis;
    private string fire1Button;
    private string fire2Button;
    private IDevice primaryWeapon = new MachineGunDevice();
    private IDevice secondaryWeapon;
    [HideInInspector]
    public Camera _camera;
    [HideInInspector]
    public ShipController ship;

    private int lives = 0;
    private int selectedShipTemplateIndex = 0;
    private float nextSwitchShip = 0.0f;

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    void Start()
    {
        turnAxis = controls.ToString() + " Turn";
        fire1Button = controls.ToString() + " Fire1";
        fire2Button = controls.ToString() + " Fire2";
    }

    void Update()
    {
        if (ship == null && lives > 0) {
            float turn = Input.GetAxis(turnAxis);
            if (Mathf.Abs(turn) > 0.2f && Time.time > nextSwitchShip)
            {
                nextSwitchShip = Time.time + 0.5f;
                var count = shipTemplates.Count();
                selectedShipTemplateIndex = (selectedShipTemplateIndex + Mathf.RoundToInt(Mathf.Sign(turn)) + count) % count;
                updateSelectShip();
            }
            if (Input.GetButton(fire1Button) || Input.GetButton(fire2Button)) {
                spawnShip();
                updateSelectShip();
            }
        }
        if (ship != null) {
            primaryWeapon.Update(fire1Button, this, ship);
            if (primaryWeapon.Depleted) setPrimaryWeapon(new MachineGunDevice());
            if (secondaryWeapon != null)
            {
                secondaryWeapon.Update(fire2Button, this, ship);
                if (secondaryWeapon.Depleted) setSecondaryWeapon(null);
            }
        }
    }

    void LateUpdate()
    {
        if (ship != null) {
            UpdateCameraTransform(ship.transform.position);
            hud.UpdateHealth(Mathf.RoundToInt(ship.health));
        } else {
            UpdateCameraTransform(repairBase.transform.position);
            hud.UpdateHealth(0);
        }
    }

    private void UpdateCameraTransform(Vector3 position)
    {
        Vector3 up = position.normalized;
        Vector3 lookAt = up * Mathf.Clamp(position.magnitude, cameraMinDistance, cameraMaxDistance);
        _camera.transform.position = lookAt + cameraOffset;
        _camera.transform.LookAt(lookAt, up);
    }

    public bool isAlive()
    {
        return (ship != null && ship.health > 0.0f) || lives > 0;
    }

    public void respawn(Vector3 position)
    {
        repairBase.respawn(position);
        lives = 1;
        if (ship != null) {
            Destroy(ship.gameObject);
        }
        setPrimaryWeapon(new MachineGunDevice());
        setSecondaryWeapon(null);
        updateSelectShip();
    }

    private void spawnShip() {
        var position = repairBase.transform.position;
        lives--;
        var shipTemplate = shipTemplates[selectedShipTemplateIndex];
        ship = Instantiate(shipTemplate, position, Quaternion.Euler(0, 0, -Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg)) as ShipController;
        ship.gameObject.SetActive(true);
        ship.playerController = this;
        ship.color = color;
        resetDeviceEffects();
        hud.UpdateHealth(Mathf.RoundToInt(ship.health));
    }

    private void updateSelectShip() {
        if (ship == null && lives > 0) {
            hud.UpdateSelectShip(shipTemplates[selectedShipTemplateIndex]);
        } else {
            hud.UpdateSelectShip(null);
        }
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
        if (ship != null) {
            var clone = Instantiate(droneTemplate, position, transform.rotation);
            clone.ship = ship.GetComponent<Rigidbody2D>();
            clone.color = color;
            clone.gameObject.SetActive(true);
        }
    }
    private void resetDeviceEffects()
    {
        if (ship != null) {
            ship.resetDeviceEffects(new IDevice[] { primaryWeapon, secondaryWeapon });
        }
    }

    public void updateWeaponHud()
    {
        hud.UpdateWeapons(primaryWeapon, secondaryWeapon);
    }
}

public interface IDevice
{
    void Update(string button, PlayerController player, ShipController ship);
    string HudRow();
    bool Depleted { get; }
}

public class MachineGunDevice : IDevice
{
    private float fireRate = 0.2f;
    private float nextFire = 0.0f;

    public void Update(string button, PlayerController player, ShipController ship)
    {
        if (Input.GetButton(button) && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.projectileTemplate, ship.gunPoint.position, ship.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = ship.GetComponent<Rigidbody2D>().velocity;
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
    private float fireRate = 0.5f;
    private float nextFire = 0.0f;
    private int pulses = 10;
    private float laserDamage = 10f;
    public void Update(string button, PlayerController player, ShipController ship)
    {
        if (Input.GetButton(button) && Time.time > nextFire && pulses > 0)
        {
            pulses--;
            player.updateWeaponHud();
            nextFire = Time.time + fireRate;
            Vector3 start = ship.gunPoint.position;
            RaycastHit2D hit = Physics2D.Raycast(start, ship.gunPoint.up, 100, player.laserLayerMask);
            Vector3 end;
            if (hit.collider != null)
            {
                end = hit.point;
                Damageable damageable = hit.collider.GetComponent<Damageable>();
                if (damageable != null)
                {
                    damageable.doDamage(laserDamage);
                }
            }
            else
            {
                end = start + ship.gunPoint.up * 100f;
            }
            var center = (start + end) * 0.5f;

            var clone = GameObject.Instantiate(player.deathrayBeamTemplate, center, ship.gunPoint.rotation);
            clone.radius = (start - end).magnitude * 0.5f;
        }
    }
    public string HudRow()
    {
        return "Laser charges: " + pulses;
    }
    public bool Depleted
    {
        get { return pulses <= 0; }
    }
}

public class FlamerDevice : IDevice
{
    private float energy = 10;
    private bool wasOn = false;
    private Vector3 oldPosition;
    private Vector3 oldVelocity;
    private Vector3 oldUp;
    public void Update(string button, PlayerController player, ShipController ship)
    {
        bool flamerOn = Input.GetButton(button) && energy > 0;
        if (flamerOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (flamerOn)
        {
            var currentPosition = ship.gunPoint.position;
            Vector3 currentVelocity = ship.GetComponent<Rigidbody2D>().velocity;
            var currentUp = ship.gunPoint.up;

            if (wasOn) {
                var count = Mathf.RoundToInt(Time.deltaTime / 0.005f);
                for (int i = 1; i < count; i++)
                {
                    var fraction = i * 1f / count;
                    var position = Vector3.Lerp(oldPosition, currentPosition, fraction);
                    var velocity = Vector3.Lerp(oldVelocity, currentVelocity, fraction);
                    var up = Vector3.Slerp(oldUp, currentUp, fraction);
                    var elapsed = Time.deltaTime * (1f - fraction);
                    Emit(position, velocity, up, elapsed, player);
                }
            }
            Emit(currentPosition, currentVelocity, currentUp, 0, player);

            oldPosition = currentPosition;
            oldVelocity = currentVelocity;
            oldUp = currentUp;
        }
        wasOn = flamerOn;
    }

    private void Emit(Vector3 position, Vector3 velocity, Vector3 up, float elapsed, PlayerController player) {
        Vector3 v = velocity + up * 15f;
        Vector3 p = position + v * elapsed;
        player.gameController.fluidSystem.EmitFire(p, v);
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
    public void Update(string button, PlayerController player, ShipController ship)
    {
        var afterBurnerOn = Input.GetButton(button) && energy > 0;
        if (afterBurnerOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (afterBurnerOn != ship.afterBurner.isEmitting)
        {
            if (afterBurnerOn)
                ship.afterBurner.Play();
            else
                ship.afterBurner.Stop();
        }
        if (!ship.spike.activeSelf)
        {
            ship.spike.SetActive(true);
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
    public void Update(string button, PlayerController player, ShipController ship)
    {
        if (Input.GetButton(button) && Time.time > nextFire && missiles > 0)
        {
            missiles--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.missileTemplate, ship.gunPoint.position, ship.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = ship.GetComponent<Rigidbody2D>().velocity;
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
    public void Update(string button, PlayerController player, ShipController ship)
    {
        if (Input.GetButton(button) && Time.time > nextFire && missiles > 0)
        {
            missiles--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.homingMissileTemplate, ship.gunPoint.position, ship.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = ship.GetComponent<Rigidbody2D>().velocity;
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
    public void Update(string button, PlayerController player, ShipController ship)
    {
        if (Input.GetButton(button) && Time.time > nextFire && bombs > 0)
        {
            bombs--;
            nextFire = Time.time + fireRate;
            GameObject clone = GameObject.Instantiate(player.bombTemplate, ship.gunPoint.position, ship.gunPoint.rotation);
            clone.GetComponent<Rigidbody2D>().velocity = ship.GetComponent<Rigidbody2D>().velocity;
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
    public void Update(string button, PlayerController player, ShipController ship)
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
                var emission = ship.deathrayLoading.emission;
                emission.rateOverTime = Mathf.Max(10, loadedNormalized * 100);
                ship.doDamage(loadedNormalized * Time.deltaTime);
                deathrayLoadingOn = true;
            }
            if (Input.GetButtonUp(button))
            {
                if (loaded > deathrayLoadingTimeMin)
                {
                    rays--;
                    fireDeathray(player, ship);
                    player.updateWeaponHud();
                }
                loaded = 0.0f;
            }
        }
        if (deathrayLoadingOn != ship.deathrayLoading.isEmitting)
        {
            if (deathrayLoadingOn)
                ship.deathrayLoading.Play();
            else
                ship.deathrayLoading.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void fireDeathray(PlayerController player, ShipController ship)
    {
        var loadedNormalized = Mathf.Clamp01(loaded / deathrayLoadingTimeMax);
        var distance = loadedNormalized * player.deathrayDistance;
        var damage = loadedNormalized * deathrayDamage;
        var start = ship.gunPoint.position;
        var direction = ship.gunPoint.transform.up * distance;
        var center = start + direction * 0.5f;
        var angle = ship.gunPoint.transform.eulerAngles.z;
        Collider2D[] colliders = Physics2D.OverlapCapsuleAll(center, new Vector2(player.deathrayWidth, distance + player.deathrayWidth), CapsuleDirection2D.Vertical, angle);
        Damageable damageable;
        TerrainPiece terrainPiece;
        foreach (Collider2D coll in colliders)
        {
            if (coll.gameObject == ship.gameObject)
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
        var clone = GameObject.Instantiate(player.deathrayBeamTemplate, center, ship.gunPoint.rotation);
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
    public void Update(string button, PlayerController player, ShipController ship)
    {
        bool shieldOn = Input.GetButton(button) && energy > 0;
        if (shieldOn)
        {
            energy -= Time.deltaTime;
            player.updateWeaponHud();
        }
        if (shieldOn != ship.shield.activeSelf)
        {
            ship.shield.SetActive(shieldOn);
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