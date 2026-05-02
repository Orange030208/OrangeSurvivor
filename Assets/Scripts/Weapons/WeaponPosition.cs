using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    private const string WEAPON_PREFAB_RESOURCE_PATH = "Prefabs/Weapons/Weapon";

    public Weapon Weapon { get; private set; }

    public Weapon AssignWeapon(Entity owner,WeaponDataSO weaponData, int level)
    {
        if (weaponData == null)
        {
            return null;
        }

        if (Weapon != null)
        {
            Destroy(Weapon.gameObject);
            Weapon = null;
        }

        Weapon weaponPrefab = LoadWeaponPrefab();
        Weapon = Instantiate(weaponPrefab, transform);
        
        ItemQualityVisualResolver.Apply(Weapon, weaponData, level, Weapon.EntityRenderer.SpriteRenderer);
        
        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;
        Weapon.Initialize(owner);
        Weapon.SetWeaponData(weaponData);
        Weapon.OnEnableComponent();
        Weapon.SetLevel(level);
        return Weapon;
    }

    private static Weapon LoadWeaponPrefab()
    {
        Weapon weaponPrefab = Resources.Load<Weapon>(WEAPON_PREFAB_RESOURCE_PATH);
        if (weaponPrefab == null)
        {
            throw new MissingReferenceException(
                $"{nameof(WeaponPosition)} requires a {nameof(Weapon)} prefab at " +
                $"Assets/Resources/{WEAPON_PREFAB_RESOURCE_PATH}.prefab.");
        }

        return weaponPrefab;
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
