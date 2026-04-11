using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsHolder : MonoBehaviour
{
    [SerializeField] private WeaponPosition[] weaponPositions;

    private readonly List<EquippedWeaponInfo> equippedWeapons = new();

    public event Action OnWeaponsChanged;
    public IReadOnlyList<EquippedWeaponInfo> EquippedWeapons => equippedWeapons.AsReadOnly();

    private void Awake()
    {
        RebuildEquippedWeaponsCache();
    }

    public bool AddWeapon(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null)
        {
            return false;
        }

        WeaponPosition emptyPosition = GetEmptyWeaponPosition();
        if (emptyPosition == null)
        {
            Debug.LogWarning("No empty weapon position available.");
            return false;
        }

        Weapon runtimeWeapon = emptyPosition.AssignWeapon(weaponData.WeaponPrefab, WeaponLevelHelper.ClampLevel(level));
        if (runtimeWeapon == null)
        {
            return false;
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        if (weapon == null || weaponPositions == null)
        {
            return false;
        }

        bool removed = false;
        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].RemoveWeapon(weapon))
            {
                removed = true;
                break;
            }
        }

        if (!removed)
        {
            return false;
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    public bool MergeWeapon(Weapon sourceWeapon, Weapon targetWeapon)
    {
        if (sourceWeapon == null || targetWeapon == null || sourceWeapon == targetWeapon)
        {
            return false;
        }

        if (sourceWeapon.WeaponData != targetWeapon.WeaponData || sourceWeapon.Level != targetWeapon.Level)
        {
            return false;
        }

        if (!WeaponLevelHelper.TryGetMergedLevel(sourceWeapon.Level, out int mergedLevel))
        {
            return false;
        }

        WeaponPosition sourcePosition = FindWeaponPosition(sourceWeapon);
        WeaponPosition targetPosition = FindWeaponPosition(targetWeapon);
        if (sourcePosition == null || targetPosition == null)
        {
            return false;
        }

        WeaponDataSO weaponData = sourceWeapon.WeaponData;

        if (!sourcePosition.RemoveWeapon(sourceWeapon))
        {
            return false;
        }

        if (!targetPosition.RemoveWeapon(targetWeapon))
        {
            sourcePosition.AssignWeapon(weaponData.WeaponPrefab, sourceWeapon.Level);
            RebuildEquippedWeaponsCache();
            OnWeaponsChanged?.Invoke();
            return false;
        }

        targetPosition.AssignWeapon(weaponData.WeaponPrefab, mergedLevel);

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    public void RefreshSnapshot()
    {
        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
    }

    private WeaponPosition GetEmptyWeaponPosition()
    {
        if (weaponPositions == null)
        {
            return null;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].Weapon == null)
            {
                return weaponPositions[i];
            }
        }

        return null;
    }

    private WeaponPosition FindWeaponPosition(Weapon weapon)
    {
        if (weaponPositions == null || weapon == null)
        {
            return null;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].Weapon == weapon)
            {
                return weaponPositions[i];
            }
        }

        return null;
    }

    private void RebuildEquippedWeaponsCache()
    {
        equippedWeapons.Clear();

        if (weaponPositions == null)
        {
            return;
        }

        foreach (WeaponPosition weaponPosition in weaponPositions)
        {
            if (weaponPosition == null || weaponPosition.Weapon == null)
            {
                continue;
            }

            Weapon weapon = weaponPosition.Weapon;
            if (weapon.WeaponData == null)
            {
                continue;
            }

            equippedWeapons.Add(new EquippedWeaponInfo(weapon.WeaponData, weapon.Level, weapon));
        }
    }
}

public readonly struct EquippedWeaponInfo
{
    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public Weapon RuntimeWeapon { get; }

    public EquippedWeaponInfo(WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
    {
        WeaponData = weaponData;
        Level = level;
        RuntimeWeapon = runtimeWeapon;
    }
}
