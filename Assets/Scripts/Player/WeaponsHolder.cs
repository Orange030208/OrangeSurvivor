using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsHolder : MonoBehaviour
{
    [SerializeField] private WeaponPosition[] weaponPositions;
    [SerializeField] private Transform weaponsParentTransform; // 武器实例化的父节点

    private readonly List<EquippedWeaponInfo> _equippedWeapons = new();

    public event Action OnWeaponsChanged;
    public IReadOnlyList<EquippedWeaponInfo> EquippedWeapons => _equippedWeapons.AsReadOnly();

    private void Awake()
    {
        RebuildEquippedWeaponsCache();
    }

    public void AddWeapon(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null)
        {
            return;
        }

        if (weaponPositions == null || weaponPositions.Length == 0)
        {
            Debug.LogWarning("Weapon positions are not configured.");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, weaponPositions.Length);
        weaponPositions[randomIndex].AssignWeapon(weaponData.WeaponPrefab, level);

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
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

    public void RefreshSnapshot()
    {
        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
    }

    private void RebuildEquippedWeaponsCache()
    {
        _equippedWeapons.Clear();

        if (weaponPositions == null)
        {
            return;
        }

        foreach (var weaponPosition in weaponPositions)
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

            _equippedWeapons.Add(new EquippedWeaponInfo(weapon.WeaponData, weapon.Level, weapon));
        }
    }
}

public readonly struct EquippedWeaponInfo
{
    public WeaponDataSO WeaponData { get; }
    public Weapon RuntimeWeapon { get; }

    public EquippedWeaponInfo(WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
    {
        WeaponData = weaponData;
        RuntimeWeapon = runtimeWeapon;
    }
}
