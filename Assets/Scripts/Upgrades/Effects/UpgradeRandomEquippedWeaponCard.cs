using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UpgradeRandomEquippedWeaponCard : FeatureEffectBase
{
    [SerializeField] private int levelIncrease = 1;

    public UpgradeRandomEquippedWeaponCard()
    {
    }

    public UpgradeRandomEquippedWeaponCard(int levelIncrease)
    {
        this.levelIncrease = Mathf.Max(1, levelIncrease);
    }

    public override string Description => $"随机一把已装备武器等级 +{Mathf.Max(1, levelIncrease)}。";

    public override void OnInstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        List<Weapon> candidates = new();
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            Weapon weapon = equippedWeapons[i].RuntimeWeapon;
            if (weapon != null && !WeaponLevelHelper.IsMaxLevel(weapon.Level))
            {
                candidates.Add(weapon);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        Weapon selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        selected.SetLevel(WeaponLevelHelper.ClampLevel(selected.Level + Mathf.Max(1, levelIncrease)));
    }
}
