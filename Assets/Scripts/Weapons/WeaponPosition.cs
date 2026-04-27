using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }

    public Weapon AssignWeapon(Entity owner,WeaponDataSO weaponData, int level)
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
        
        ItemQualityVisualResolver.Apply(Weapon, weaponData, level, Weapon.EntityRenderer.SpriteRenderer);
        
        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;
        Weapon.Initialize(owner);
        Weapon.SetWeaponData(weaponData);
        Weapon.OnEnableComponent();
        Weapon.SetLevel(level);
        return Weapon;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        if (Weapon == null || weapon == null || Weapon != weapon)
        {
            return false;
        }

        weapon.OnDisableComponent();
        Destroy(Weapon.gameObject);
        Weapon = null;
        return true;
    }
}
