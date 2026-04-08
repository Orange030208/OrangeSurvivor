using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }

    public void AssignWeapon(Weapon weapon, int level)
    {
        Weapon = Instantiate(weapon, transform);

        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;

        Weapon.UpgradeTo(level);
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
