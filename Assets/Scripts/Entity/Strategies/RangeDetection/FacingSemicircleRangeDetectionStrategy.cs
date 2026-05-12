using UnityEngine;

/// <summary>
/// 近战半圆入场检测：以攻击点为圆心，用 AttackRange 判断目标是否处于当前目标方向的前半圆。
/// </summary>
public sealed class FacingSemicircleRangeDetectionStrategy : RangeDetectionStrategyBase
{
    private readonly Transform attackPointTransform;
    private readonly float rangeMultiplier;
    private bool hasWarnedMissingAttackPoint;

    public FacingSemicircleRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        Transform attackPointTransform,
        float rangeMultiplier = 1f)
        : base(owner, propertiesManager)
    {
        this.attackPointTransform = attackPointTransform;
        this.rangeMultiplier = Mathf.Max(0f, rangeMultiplier);
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        Vector2 attackCenter = ResolveAttackCenter();
        float attackRadius = PropValueUtility.DistancePointsToWorldUnits(propertiesManager.GetPropValue(PropType.AttackRange)) * rangeMultiplier;
        if (!target.IsColliderWithinRange(attackCenter, attackRadius))
        {
            return false;
        }

        Vector2 facingDirection = ResolveHorizontalDirection(target);
        return AreaHitQueryUtility.IsColliderInFacingSemicircle(
            target.EntityCollider,
            attackCenter,
            facingDirection.normalized);
    }

    private Vector2 ResolveAttackCenter()
    {
        if (attackPointTransform != null)
        {
            return attackPointTransform.position;
        }

        if (!hasWarnedMissingAttackPoint)
        {
            hasWarnedMissingAttackPoint = true;
            Debug.LogWarning($"{nameof(FacingSemicircleRangeDetectionStrategy)} on {owner.name} is missing attack point. Falling back to owner center.", owner);
        }

        return owner.Center;
    }

    private Vector2 ResolveHorizontalDirection(Entity target)
    {
        Vector2 direction = target.Center - owner.Center;
        if (Mathf.Abs(direction.x) <= Mathf.Epsilon)
        {
            direction = owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
        }

        return direction.x < 0f ? Vector2.left : Vector2.right;
    }
}
