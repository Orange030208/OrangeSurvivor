using System;
using UnityEngine;

[Serializable]
public struct WeaponEntry
{
    public WeaponDataSO weaponData;
    [Min(1)] public int level;

    public WeaponEntry(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = Mathf.Max(1, level);
    }
}
