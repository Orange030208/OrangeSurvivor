using System.Collections.Generic;
using UnityEngine;

public sealed class UpgradeCardOfferContext
{
    private readonly List<WeaponDataSO> ownedWeapons = new();
    private readonly HashSet<string> ownedWeaponNames = new(System.StringComparer.Ordinal);

    public UpgradeCardOfferContext(UpgradeRunState runState, int waveNumber, WeaponsHolder weaponsHolder)
    {
        RunState = runState;
        WaveNumber = Mathf.Max(1, waveNumber);
        AddEquippedWeapons(weaponsHolder);
    }

    public UpgradeCardOfferContext(UpgradeRunState runState, int waveNumber, IReadOnlyList<WeaponDataSO> ownedWeapons)
    {
        RunState = runState;
        WaveNumber = Mathf.Max(1, waveNumber);
        if (ownedWeapons == null)
        {
            return;
        }

        for (int i = 0; i < ownedWeapons.Count; i++)
        {
            AddOwnedWeapon(ownedWeapons[i]);
        }
    }

    public UpgradeRunState RunState { get; }
    public int WaveNumber { get; }
    public IReadOnlyList<WeaponDataSO> OwnedWeapons => ownedWeapons;

    public bool HasOwnedWeapon(WeaponDataSO weaponData)
    {
        if (weaponData == null)
        {
            return false;
        }

        if (ownedWeapons.Contains(weaponData))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(weaponData.ItemName) && ownedWeaponNames.Contains(weaponData.ItemName);
    }

    private void AddEquippedWeapons(WeaponsHolder weaponsHolder)
    {
        if (weaponsHolder == null)
        {
            return;
        }

        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            AddOwnedWeapon(equippedWeapons[i].WeaponData);
        }
    }

    private void AddOwnedWeapon(WeaponDataSO weaponData)
    {
        if (weaponData == null)
        {
            return;
        }

        ownedWeapons.Add(weaponData);
        if (!string.IsNullOrWhiteSpace(weaponData.ItemName))
        {
            ownedWeaponNames.Add(weaponData.ItemName);
        }
    }
}
