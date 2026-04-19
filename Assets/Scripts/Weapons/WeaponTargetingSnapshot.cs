using UnityEngine;

/// <summary>
/// 武器索敌快照：
/// - 把当前目标与瞄准方向解算所需的外部状态收敛为一个只读结构；
/// - 让索敌/转向判断可以在纯 C# 逻辑中完成。
/// 扩展说明：后续若要支持更多索敌来源或瞄准上下文，优先扩展该快照结构，不要把更多判断散回 Weapon。
/// </summary>
public readonly struct WeaponTargetingSnapshot
{
    public Entity OwnerEntity { get; }
    public Entity CurrentTarget { get; }
    public float Range { get; }
    public LayerMask TargetLayerMask { get; }
    public Vector2 WeaponPosition { get; }
    public Vector2 CurrentAimDirection { get; }
    public Vector2 LastAimDirection { get; }
    public float AttackCooldownTimer { get; }
    public float AttackInterval { get; }
    public bool IsAttacking { get; }
    public bool StopAimingWhenAttackReady { get; }
    public float AttackStartAimToleranceDegrees { get; }

    public WeaponTargetingSnapshot(
        Entity ownerEntity,
        Entity currentTarget,
        float range,
        LayerMask targetLayerMask,
        Vector2 weaponPosition,
        Vector2 currentAimDirection,
        Vector2 lastAimDirection,
        float attackCooldownTimer,
        float attackInterval,
        bool isAttacking,
        bool stopAimingWhenAttackReady,
        float attackStartAimToleranceDegrees)
    {
        OwnerEntity = ownerEntity;
        CurrentTarget = currentTarget;
        Range = range;
        TargetLayerMask = targetLayerMask;
        WeaponPosition = weaponPosition;
        CurrentAimDirection = currentAimDirection;
        LastAimDirection = lastAimDirection;
        AttackCooldownTimer = attackCooldownTimer;
        AttackInterval = attackInterval;
        IsAttacking = isAttacking;
        StopAimingWhenAttackReady = stopAimingWhenAttackReady;
        AttackStartAimToleranceDegrees = attackStartAimToleranceDegrees;
    }
}
