using UnityEngine;

/// <summary>
/// 直接追击策略
/// </summary>
[CreateAssetMenu(fileName = "DirectChaseStrategy", menuName = ScriptableObjectMenuPaths.DIRECT_CHASE_STRATEGY, order = 0)]
public class DirectChaseStrategy : MovementStrategyBase
{
    public override void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData)
    {
        movable.MoveTo(target.Center);
    }
}
