using UnityEngine;

//攻击输入规格
public readonly struct HitSpec
{
    public float BaseDamage { get; }
    public float CritChance { get; }
    public float CritMultiplier { get; }
    public float KnockbackStrength { get; }

    public HitSpec(float baseDamage, float critChance, float critMultiplier)
        : this(baseDamage, critChance, critMultiplier, 0f)
    {
    }

    public HitSpec(float baseDamage, float critChance, float critMultiplier, float knockbackStrength)
    {
        BaseDamage = PropValueUtility.ClampNonNegative(baseDamage);
        CritChance = PropValueUtility.ClampEffectiveRatio(PropType.CriticalChance, critChance);
        CritMultiplier = PropValueUtility.ClampEffectiveCriticalMultiplier(critMultiplier);
        KnockbackStrength = PropValueUtility.ClampEffectiveKnockbackStrength(knockbackStrength);
    }

    public static HitSpec EnemyHitSpec(float baseDamage)
    {
        return new HitSpec(baseDamage, 0f, 1f, 0f);
    }
}
