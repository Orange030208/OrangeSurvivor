using UnityEngine;

/// <summary>
/// 直接追击策略
/// </summary>
[CreateAssetMenu(fileName = "DirectChaseStrategy", menuName = "Entity/Component/Move/DirectChaseStrategy", order = 0)]
public class DirectChaseStrategy : MovementStrategyBase
{
    public override void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData)
    {
        movable.MoveTo(target.Center);
    }
}
