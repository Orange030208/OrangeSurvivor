using UnityEngine;

/// <summary>
/// 后撤策略
/// </summary>
[CreateAssetMenu(fileName = "RetreatStrategy", menuName = "Entity/Component/Move/RetreatStrategy")]
public class RetreatStrategy : MovementStrategyBase
{
    [SerializeField] private float safeDistance = 8f;

    public override void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData)
    {
        float currentDist = Vector2.Distance(self.Center, target.Center);
        if (currentDist < safeDistance)
        {
            Vector3 retreatDir = (self.Center - target.Center).normalized;
            movable.MoveTo(self.Center + (Vector2)retreatDir * 3f);
            return;
        }

        movable.StopMoving();
    }
}
