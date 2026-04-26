using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }

    public Weapon AssignWeapon(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null || weaponData.WeaponPrefab == null)
        {
            return null;
        }

        if (Weapon != null)
        {
            Destroy(Weapon.gameObject);
            Weapon = null;
        }

        Weapon = Instantiate(weaponData.WeaponPrefab, transform);
        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;
        Weapon.SetWeaponData(weaponData);
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
