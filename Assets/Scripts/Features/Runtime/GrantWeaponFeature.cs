using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GrantWeaponFeature : FeatureBase
{
    [SerializeField] private string weaponId;
    [SerializeField, Min(WeaponLevelHelper.MinLevel)] private int level = WeaponLevelHelper.MinLevel;
    [SerializeField] private bool playEquipSfx = true;

    public GrantWeaponFeature()
    {
    }

    public GrantWeaponFeature(string weaponId, int level, bool playEquipSfx = true)
    {
        this.weaponId = weaponId;
        this.level = WeaponLevelHelper.ClampLevel(level);
        this.playEquipSfx = playEquipSfx;
    }

    public override string Title => "立即获得武器";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            Debug.LogWarning($"[{nameof(GrantWeaponFeature)}] Missing weapon id.");
            return;
        }

        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Debug.LogWarning($"[{nameof(GrantWeaponFeature)}] Could not resolve game content provider.");
            return;
        }

        WeaponDataSO weaponData = ResolveWeapon(provider.Weapons);
        if (weaponData == null)
        {
            Debug.LogWarning($"[{nameof(GrantWeaponFeature)}] Weapon '{weaponId}' was not found in content catalog.");
            return;
        }

        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            Debug.LogWarning($"[{nameof(GrantWeaponFeature)}] Owner is missing {nameof(WeaponsHolder)}.");
            return;
        }

        if (!weaponsHolder.AddWeapon(weaponData, WeaponLevelHelper.ClampLevel(level), playEquipSfx))
        {
            Debug.LogWarning(
                $"[{nameof(GrantWeaponFeature)}] Failed to grant weapon '{weaponId}' at level {WeaponLevelHelper.ClampLevel(level)}.");
        }
    }

    private WeaponDataSO ResolveWeapon(IReadOnlyList<WeaponDataSO> weapons)
    {
        if (weapons == null)
        {
            return null;
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDataSO weapon = weapons[i];
            if (weapon != null && string.Equals(weapon.WeaponId, weaponId, StringComparison.Ordinal))
            {
                return weapon;
            }
        }

        return null;
    }

    private string BuildDescription()
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            return "未配置要获得的武器。";
        }

        return $"立即获得 {WeaponLevelHelper.ClampLevel(level)} 级 {weaponId}。";
    }
}
