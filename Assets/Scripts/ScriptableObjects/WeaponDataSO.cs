using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ItemDataSO
{
    [SerializeField] protected Weapon weaponPrefab;

    [Header("属性")]
    [SerializeField] protected float attack;
    [SerializeField] protected float attackSpeed;
    [SerializeField] protected float criticalChance;
    [SerializeField] protected float criticalPercent;
    [SerializeField] protected float range;

    public Weapon WeaponPrefab => weaponPrefab;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
    }

    public List<PropEntry> GetPropsList()
    {
        List<PropEntry> list =
            new()
            {
                new PropEntry(PropType.Attack, attack),
                new PropEntry(PropType.AttackSpeed, attackSpeed),
                new PropEntry(PropType.CriticalChance, criticalChance),
                new PropEntry(PropType.CriticalPercent, criticalPercent),
                new PropEntry(PropType.Range, range)
            };
        return list;
    }

    public Dictionary<PropType, float> GetPropsByLevel(int level)
    {
        return WeaponPropsCalculator.GetProps(this, level);
    }
}
