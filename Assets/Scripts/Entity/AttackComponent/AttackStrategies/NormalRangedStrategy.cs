using UnityEngine;

/// <summary>
/// 普通远程攻击策略
/// </summary>
[CreateAssetMenu(fileName = "NormalRangedStrategy", menuName = "Entity/Component/Attack/NormalRanged")]
public class NormalRangedStrategy : AttackStrategyBase
{
    public override void ExecuteAttack(IAttackable attackable, Entity self, Entity target)
    {
        attackable.TryAttack(target);
    }
}