using UnityEngine;

/// <summary>
/// 普通近战攻击策略
/// </summary>
[CreateAssetMenu(fileName = "MeleeAttackStrategy", menuName = ScriptableObjectMenuPaths.MELEE_ATTACK_STRATEGY)]
public class MeleeAttackStrategy : AttackStrategyBase
{
    public override void ExecuteAttack(IAttackable attackable, Entity self, Entity target)
    {
        attackable.TryAttack(target);
    }
}
