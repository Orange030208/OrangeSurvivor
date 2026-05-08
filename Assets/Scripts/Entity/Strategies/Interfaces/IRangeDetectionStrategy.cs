public interface IRangeDetectionStrategy
{
    /// <summary>
    /// 判断目标是否满足当前策略定义的范围条件；这里只负责范围判断，不负责攻击执行或伤害结算。
    /// </summary>
    bool IsTargetInRange(Entity target);
}
