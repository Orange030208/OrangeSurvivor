using UnityEngine;

/// <summary>
/// 一次攻击在真正执行前解析出的上下文数据。
/// 它把“这次攻击需要的所有输入”收拢到一起，避免执行器再回头到 Weapon 上取状态。
/// 当前包含：武器实例、真实伤害来源实体、发射/命中原点、目标、方向、运行时属性，以及本次命中的基础规格。
/// </summary>
public readonly struct WeaponAttackContext
{
    public Weapon Weapon { get; }
    public Entity SourceEntity { get; }
    public Transform Origin { get; }
    public Entity Target { get; }
    public Vector2 AimDirection { get; }
    public WeaponRuntimeStats Stats { get; }
    public HitSpec HitSpec { get; }

    public WeaponAttackContext(Weapon weapon, Entity sourceEntity, Transform origin, Entity target, Vector2 aimDirection, WeaponRuntimeStats stats, HitSpec hitSpec)
    {
        Weapon = weapon;
        SourceEntity = sourceEntity;
        Origin = origin;
        Target = target;
        AimDirection = aimDirection;
        Stats = stats;
        HitSpec = hitSpec;
    }
}
