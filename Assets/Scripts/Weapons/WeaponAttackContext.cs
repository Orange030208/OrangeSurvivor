using UnityEngine;

/// <summary>
/// 一次攻击在真正执行前解析出的上下文数据。
/// 它把“这次攻击需要的所有输入”收拢到一起，避免执行器再回头到 Weapon 上取状态。
/// 当前包含：武器实例、发射/命中原点、目标、方向、结算后的运行时属性，以及最终伤害结果。
/// 如果后续要扩展蓄力等级、元素类型、击退参数等，也建议加在这里。
/// </summary>
public readonly struct WeaponAttackContext
{
    public Weapon Weapon { get; }
    public Transform Origin { get; }
    public Entity Target { get; }
    public Vector2 AimDirection { get; }
    public WeaponRuntimeStats Stats { get; }
    public ResolvedWeaponHit Hit { get; }

    public WeaponAttackContext(Weapon weapon, Transform origin, Entity target, Vector2 aimDirection, WeaponRuntimeStats stats, ResolvedWeaponHit hit)
    {
        Weapon = weapon;
        Origin = origin;
        Target = target;
        AimDirection = aimDirection;
        Stats = stats;
        Hit = hit;
    }
}
