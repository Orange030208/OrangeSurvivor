using UnityEngine;

/// <summary>
/// 空攻击策略，用于需要显式禁止攻击的状态。
/// </summary>
[CreateAssetMenu(fileName = "NoAttackStrategy", menuName = ScriptableObjectMenuPaths.NO_ATTACK_STRATEGY)]
public class NoAttackStrategy : AttackStrategyBase
{
    public override void ExecuteAttack(IAttackable attackable, Entity self, Entity target)
    {
    }
}
