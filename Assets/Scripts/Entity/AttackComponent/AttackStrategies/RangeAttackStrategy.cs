using UnityEngine;
/// <summary>
/// 普通远程攻击策略
/// </summary>
[CreateAssetMenu(fileName = "RangeAttackStrategy", menuName = "Entity/Component/Attack/RangeAttack")]
public class RangeAttackStrategy : AttackStrategyBase
{
    public override void ExecuteAttack(IAttackable attackable, Entity self, Entity target)
    {
        attackable.TryAttack(target);
    }
}