using UnityEngine;

/// <summary>
/// 一次攻击最终解析出的伤害结果。
/// Weapon 会在真正出手前就把本次是否暴击、最终伤害值算好，
/// 然后近战和远程统一消费这份结果，确保同一次攻击链条里的数据一致。
/// </summary>
public readonly struct ResolvedWeaponHit
{
    public float Damage { get; }
    public bool IsCritical { get; }

    public ResolvedWeaponHit(float damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }

    /// <summary>
    /// 转成 HealthComponent 使用的 DamageInfo。
    /// 当前位置由命中目标提供，因此这里在转换时再传入 position。
    /// </summary>
    public DamageInfo ToDamageInfo(Vector2 position)
    {
        return new DamageInfo(Damage, position, IsCritical);
    }
}
