using UnityEngine;

/// <summary>
/// 武器瞄准更新结果：
/// - ShouldApplyAim 表示本帧是否需要把 AimDirection 写回 Transform；
/// - LastAimDirection 用于武器在丢失目标后维持最近一次有效朝向。
/// 扩展说明：后续若要支持不同瞄准模式，优先扩展该结果结构，不要把更多状态回塞进 Weapon。
/// </summary>
public readonly struct WeaponAimUpdate
{
    public Vector2 AimDirection { get; }
    public Vector2 LastAimDirection { get; }
    public bool ShouldApplyAim { get; }

    public WeaponAimUpdate(Vector2 aimDirection, Vector2 lastAimDirection, bool shouldApplyAim)
    {
        AimDirection = aimDirection;
        LastAimDirection = lastAimDirection;
        ShouldApplyAim = shouldApplyAim;
    }
}
