using UnityEngine;

//攻击输入规格
public readonly struct HitSpec
{
    public float BaseDamage { get; }
    public float CritChance { get; }
    public float CritMultiplier { get; }
    public float KnockbackForce { get; }

    public HitSpec(float baseDamage, float critChance, float critMultiplier)
        : this(baseDamage, critChance, critMultiplier, 0f)
    {
    }

    public HitSpec(float baseDamage, float critChance, float critMultiplier, float knockbackForce)
    {
        BaseDamage = Mathf.Max(0f, baseDamage);
        CritChance = Mathf.Clamp01(critChance);
        CritMultiplier = Mathf.Max(1f, critMultiplier);
        KnockbackForce = Mathf.Max(0f, knockbackForce);
    }

    public static HitSpec EnemyHitSpec(float baseDamage)
    {
        return new HitSpec(baseDamage, 0f, 1f, 0f);
    }
}
