using System;
using System.Linq;
using UnityEngine;
using UniversalUI.Integration.Game.ScriptableObjects;
using Random = UnityEngine.Random;

public static class ResourcesManager
{
    private const string PROP_ICONS_DATA_PATH = "Data/Prop Icons";
    private const string ACCESSORY_DATA_PATH = "Data/Accessory Data List";
    private const string WEAPON_DATA_PATH = "Data/Weapon Data List";

    private static PropIcon[] propIcons;
    private static AccessoryDataSO[] Accessories;
    private static WeaponDataSO[] Weapons;

    public static Sprite GetPropIcon(PropType propType)
    {
        if (propIcons == null)
        {
            PropIconDataSO data = Resources.Load<PropIconDataSO>(PROP_ICONS_DATA_PATH);
            propIcons = data.PropIcons;
        }

        return propIcons.FirstOrDefault(propIcon => propIcon.propType == propType).icon;
    }

    private static void LoadAccessoryData()
    {
        if (Accessories == null)
        {
            Accessories = Resources.Load<AccessoryDataListSO>(ACCESSORY_DATA_PATH).Accessories;
        }
    }

    private static void LoadWeaponData()
    {
        if (Weapons == null)
        {
            Weapons = Resources.Load<WeaponDataListSO>(WEAPON_DATA_PATH).Weapons;
        }
    }

    public static WeaponDataSO GetWeapon(string weaponName)
    {
        LoadWeaponData();
        if (string.IsNullOrEmpty(weaponName))
        {
            Debug.LogWarning("WeaponName cannot be null or empty.");
            return null;
        }

        return Weapons.FirstOrDefault(w => w != null && w.ItemName == weaponName);
    }

    public static WeaponDataSO GetRandomWeapon()
    {
        LoadWeaponData();
        if (Weapons == null || Weapons.Length == 0)
        {
            Debug.LogWarning("No weapons available.");
            return null;
        }

        return Weapons[Random.Range(0, Weapons.Length)];
    }

    public static WeaponDataSO[] GetAllWeapons()
    {
        LoadWeaponData();
        return Weapons ?? Array.Empty<WeaponDataSO>();
    }

    public static AccessoryDataSO GetAccessory(string accessoryId)
    {
        LoadAccessoryData();
        if (string.IsNullOrEmpty(accessoryId))
        {
            Debug.LogWarning("AccessoryId cannot be null or empty.");
            return null;
        }

        return Accessories.FirstOrDefault(acc => acc != null && acc.AccessoryId == accessoryId);
    }

    public static AccessoryDataSO GetRandomAccessory()
    {
        LoadAccessoryData();
        if (Accessories == null || Accessories.Length == 0)
        {
            Debug.LogWarning("No accessories available.");
            return null;
        }

        return Accessories[Random.Range(0, Accessories.Length)];
    }

    public static AccessoryDataSO GetRandomAccessoryByRarity(int rarity)
    {
        LoadAccessoryData();
        var filteredAccessories = System.Array.FindAll(
            Accessories,
            acc => acc != null && acc.Rarity == rarity
        );

        if (filteredAccessories.Length == 0)
        {
            Debug.LogWarning($"No accessories found with rarity {rarity}.");
            return null;
        }

        return filteredAccessories[UnityEngine.Random.Range(0, filteredAccessories.Length)];
    }
}
