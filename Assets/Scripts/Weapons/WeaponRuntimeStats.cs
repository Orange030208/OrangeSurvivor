using UnityEngine;

public readonly struct WeaponRuntimeStats
{
    public float Damage { get; }
    public float AttackInterval { get; }
    public float Range { get; }
    public float CriticalChance { get; }
    public float CriticalMultiplier { get; }

    public WeaponRuntimeStats(float damage, float attackInterval, float range, float criticalChance, float criticalMultiplier)
    {
        Damage = damage;
        AttackInterval = Mathf.Max(0.01f, attackInterval);
        Range = Mathf.Max(0.1f, range);
        CriticalChance = Mathf.Clamp01(criticalChance);
        CriticalMultiplier = Mathf.Max(1f, criticalMultiplier);
    }
}
