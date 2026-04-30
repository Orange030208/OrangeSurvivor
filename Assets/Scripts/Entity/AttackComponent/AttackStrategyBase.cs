using UnityEngine;

/// <summary>
/// 攻击策略基类。
/// 这里只描述“如何出手”的攻击行为，例如单发、散射、近战判定等。
/// 不应该在策略里直接覆写实体的运行时属性、状态数值或固定冷却，
/// 这类会影响进入/退出恢复的问题，应放到实体配置与状态机的 OnEnter / OnExit 中，
/// 再通过属性管理器以加成或倍率的形式修改。
/// </summary>
public abstract class AttackStrategyBase : ScriptableObject
{
    public virtual bool CanExecute(IEntityAttackExecutor attackExecutor, Entity self, Entity target)
    {
        return false;
    }

    public virtual bool IsInAttackRange(IEntityAttackExecutor attackExecutor, Entity self, Entity target)
    {
        return false;
    }

    public abstract void ExecuteAttack(IEntityAttackExecutor attackExecutor, Entity self, Entity target);
}
