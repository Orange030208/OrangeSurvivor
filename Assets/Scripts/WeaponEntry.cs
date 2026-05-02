using System;
using UnityEngine;

[Serializable]
public struct WeaponEntry
{
    public WeaponDataSO weaponData;
    [Range(WeaponLevelHelper.MinLevel, WeaponLevelHelper.MaxLevel)] public int level;

    public WeaponEntry(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = WeaponLevelHelper.ClampLevel(level);
    }

    public WeaponEntry Validated()
    {
        return new WeaponEntry(weaponData, level);
    }
}
