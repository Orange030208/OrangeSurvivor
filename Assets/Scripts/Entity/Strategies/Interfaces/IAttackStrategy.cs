public interface IAttackStrategy
{
    string ActionId { get; }
    IRangeDetectionStrategy DetectionStrategy { get; }
    bool CanUse(Entity target);
    /// <summary>
    /// 即时尝试发起攻击，会完整检查冷却与入场范围。
    /// </summary>
    bool TryExecute(Entity target);
    /// <summary>
    /// 攻击状态已经入场后的提交点执行；具体策略决定是否按实际命中范围空挥。
    /// </summary>
    bool TryExecuteCommitted(Entity target);
    void ResetCooldown();
}
