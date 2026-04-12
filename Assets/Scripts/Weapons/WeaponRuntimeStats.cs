using UnityEngine;

/// <summary>
/// 武器运行时结算后的核心战斗参数。
/// 这是 Weapon 在每次属性刷新后真正使用的值：
/// - Damage：基础伤害；
/// - AttackInterval：两次攻击之间的最小间隔；
/// - Range：索敌与攻击范围；
/// - CriticalChance / CriticalMultiplier：暴击相关参数。
/// 如果后续要支持蓄力速度、命中硬直、穿透等，也可以继续往这里扩展。
/// </summary>
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
