using UnityEngine;

/// <summary>
/// 攻击策略基类
/// </summary>
public abstract class AttackStrategyBase : ScriptableObject
{
    public abstract void ExecuteAttack(IAttackable attackable, Entity self, Entity target);
}