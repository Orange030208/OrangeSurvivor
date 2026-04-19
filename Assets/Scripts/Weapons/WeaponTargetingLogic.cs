using UnityEngine;

/// <summary>
/// 武器索敌/转向纯逻辑：
/// - 负责解析当前目标；
/// - 负责判断本帧是否需要维持或更新瞄准方向；
/// - 负责攻击前朝向是否已满足发起条件的判定；
/// - 负责攻击方向与回退方向的解算；
/// - 不直接依赖 Transform、MonoBehaviour 或 Time。
/// 扩展说明：后续若要支持不同索敌策略或瞄准插值前置判断，优先扩展这里，不要继续膨胀 Weapon 基类。
/// </summary>
public static class WeaponTargetingLogic
{
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public static Entity ResolveTarget(WeaponTargetingSnapshot snapshot)
    {
        return snapshot.OwnerEntity != null
            ? snapshot.OwnerEntity.FindClosestTargetInRange(snapshot.Range, snapshot.TargetLayerMask)
            : null;
    }

    public static WeaponAimUpdate BuildAimUpdate(WeaponTargetingSnapshot snapshot, Entity resolvedTarget)
    {
        Vector2 desiredAimDirection = ResolveDesiredAimDirection(snapshot, resolvedTarget);
        bool hasReachedAttackAimDirection = HasReachedAttackAimDirection(snapshot, desiredAimDirection);
        bool holdCurrentAim = snapshot.IsAttacking ||
            (snapshot.StopAimingWhenAttackReady &&
             resolvedTarget != null &&
             snapshot.AttackCooldownTimer >= snapshot.AttackInterval &&
             hasReachedAttackAimDirection);
        if (holdCurrentAim)
        {
            return new WeaponAimUpdate(snapshot.CurrentAimDirection, snapshot.LastAimDirection, false);
        }

        if (desiredAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            Vector2 normalizedAimDirection = desiredAimDirection.normalized;
            return new WeaponAimUpdate(normalizedAimDirection, normalizedAimDirection, true);
        }

        if (snapshot.CurrentTarget != null && resolvedTarget == null)
        {
            return new WeaponAimUpdate(snapshot.LastAimDirection, snapshot.LastAimDirection, true);
        }

        return new WeaponAimUpdate(snapshot.CurrentAimDirection, snapshot.LastAimDirection, false);
    }

    public static Vector2 ResolveDesiredAimDirection(WeaponTargetingSnapshot snapshot, Entity target)
    {
        if (target != null)
        {
            return (target.Center - snapshot.WeaponPosition).normalized;
        }

        if (snapshot.OwnerEntity != null && snapshot.OwnerEntity.CurrentFacingDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return snapshot.OwnerEntity.CurrentFacingDirection.normalized;
        }

        return snapshot.LastAimDirection;
    }

    public static bool HasReachedAttackAimDirection(WeaponTargetingSnapshot snapshot, Vector2 desiredAimDirection)
    {
        if (desiredAimDirection.sqrMagnitude <= MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return true;
        }

        Vector2 currentAimDirection = snapshot.CurrentAimDirection;
        if (currentAimDirection.sqrMagnitude <= MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return true;
        }

        float angle = Vector2.Angle(currentAimDirection, desiredAimDirection.normalized);
        return angle <= snapshot.AttackStartAimToleranceDegrees;
    }

    public static Vector2 ResolveAttackDirection(WeaponTargetingSnapshot snapshot, Entity target, Vector2 originPosition)
    {
        if (target != null)
        {
            Vector2 targetDirection = target.Center - originPosition;
            if (targetDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
            {
                return targetDirection.normalized;
            }
        }

        return ResolveFallbackAttackDirection(snapshot);
    }

    public static Vector2 ResolveFallbackAttackDirection(WeaponTargetingSnapshot snapshot)
    {
        if (snapshot.CurrentAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return snapshot.CurrentAimDirection.normalized;
        }

        if (snapshot.LastAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return snapshot.LastAimDirection.normalized;
        }

        return Vector2.up;
    }
}
