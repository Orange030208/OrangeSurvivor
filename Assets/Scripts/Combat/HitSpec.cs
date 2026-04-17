using UnityEngine;

//攻击输入规格
public readonly struct HitSpec
{
    public float BaseDamage { get; }
    public float CritChance { get; }
    public float CritMultiplier { get; }

    public HitSpec(float baseDamage, float critChance, float critMultiplier)
    {
        BaseDamage = Mathf.Max(0f, baseDamage);
        CritChance = Mathf.Clamp01(critChance);
        CritMultiplier = Mathf.Max(1f, critMultiplier);
    }
}
