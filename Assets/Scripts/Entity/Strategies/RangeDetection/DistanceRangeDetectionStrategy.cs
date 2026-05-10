using UnityEngine;

/// <summary>
/// 距离检测策略：以敌人中心为圆心，使用 DetectionRange 乘以范围倍率作为半径判断目标是否进入探测范围。
/// 通常用于远程攻击释放条件、索敌、站位与行为切换等“发现/决策”层判断。
/// </summary>
public sealed class DistanceRangeDetectionStrategy : RangeDetectionStrategyBase
{
    private readonly float rangeMultiplier;

    public DistanceRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        float rangeMultiplier = 1f)
        : base(owner, propertiesManager)
    {
        this.rangeMultiplier = Mathf.Max(0f, rangeMultiplier);
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        float range = PropValueUtility.DistancePointsToWorldUnits(propertiesManager.GetPropValue(PropType.DetectionRange)) * rangeMultiplier;
        return target.IsColliderWithinRange(owner.Center, range);
    }
}
