using System;
using UnityEngine;

[Serializable]
public sealed class AddRandomWeaponUpgradeCardEffect : FeatureEffectBase
{
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private int level = WeaponLevelHelper.MinLevel;

    public AddRandomWeaponUpgradeCardEffect()
    {
    }

    public AddRandomWeaponUpgradeCardEffect(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = WeaponLevelHelper.ClampLevel(level);
    }

    public override string Description
    {
        get
        {
            string weaponName = weaponData != null ? weaponData.ItemName : "随机武器";
            return $"获得 1 把 {weaponName}。";
        }
    }

    public override void OnInstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        WeaponDataSO selectedWeapon = weaponData != null ? weaponData : ResourcesManager.GetRandomWeapon();
        if (selectedWeapon != null)
        {
            weaponsHolder.AddWeapon(selectedWeapon, level);
        }
    }
}
