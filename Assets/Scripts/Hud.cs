using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class Hud : MonoBehaviour
{

    public Text health;
    public Text weapons;
    public void UpdateHealth(int health)
    {
        this.health.text = "Health: " + health;
    }

    public void UpdateWeapons(WeaponState weaponState)
    {
        var text = new List<string>();
        switch (weaponState.primary)
        {
            case PrimaryWeapon.MachineGun:
                text.Add("Machine gun");
                break;
            case PrimaryWeapon.Laser:
                text.Add("Laser: " + energyToString(weaponState.primaryEnergy));
                break;
            case PrimaryWeapon.Flamer:
                text.Add("Flamer: " + energyToString(weaponState.primaryEnergy));
                break;
        }
        switch (weaponState.secondary)
        {
            case SecondaryWeapon.Missiles:
                text.Add("Missiles: " + weaponState.secondaryAmmunition);
                break;
            case SecondaryWeapon.Bombs:
                text.Add("Bombs: " + weaponState.secondaryAmmunition);
                break;
        }
        weapons.text = string.Join("\n", text.ToArray());
    }

    private static string energyToString(float energy)
    {
        return Mathf.Max(0, energy).ToString("f1");
    }
}
