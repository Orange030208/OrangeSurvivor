using UnityEngine;

/// <summary>
/// 武器命中事件数据：
/// - 统一承载武器实例与命中结果；
/// - 供后续命中特效、被动、统计等系统消费；
/// - 事件只携带最小必要数据，避免直接透传重量级上下文。
/// </summary>
public readonly struct WeaponHitEvent
{
    public Weapon Weapon { get; }
    public HitResult HitResult { get; }

    public WeaponHitEvent(Weapon weapon, HitResult hitResult)
    {
        Weapon = weapon;
        HitResult = hitResult;
    }
}
