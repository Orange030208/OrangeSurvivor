using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }

    public Weapon AssignWeapon(Weapon weaponPrefab, int level)
    {
        if (weaponPrefab == null)
        {
            return null;
        }

        Weapon = Instantiate(weaponPrefab, transform);
        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;
        Weapon.SetLevel(level);
        return Weapon;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        if (Weapon == null || weapon == null || Weapon != weapon)
        {
            return false;
        }

        Destroy(Weapon.gameObject);
        Weapon = null;
        return true;
    }
}
