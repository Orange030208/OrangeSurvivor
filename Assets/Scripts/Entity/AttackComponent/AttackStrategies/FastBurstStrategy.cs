using UnityEngine;

/// <summary>
/// 快速爆发远程策略
/// </summary>
[CreateAssetMenu(fileName = "FastBurstStrategy", menuName = "Entity/Component/Attack/FastBurstStrategy")]
public class FastBurstStrategy : AttackStrategyBase
{
    [SerializeField] private float burstInterval = 0.2f;
    public override void ExecuteAttack(IAttackable attackable, Entity self, Entity target)
    {
        attackable.AttackInterval = burstInterval;
        attackable.TryAttack(target);
    }
}