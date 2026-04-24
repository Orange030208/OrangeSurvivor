using UnityEngine;

/// <summary>
/// 中距离绕圈策略（法师默认状态）
/// </summary>
[CreateAssetMenu(fileName = "CircleKiteStrategy", menuName = "Entity/Component/Move/CircleKiteStrategy")]
public class CircleKiteStrategy : MovementStrategyBase
{
    [SerializeField] private float circleSpeed = 2f;
    [SerializeField] private float IdealCircleRange = 6f;

    public override void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData)
    {
        Vector3 targetDir = target.Center - self.Center;
        targetDir.y = 0f;
        targetDir.Normalize();

        Vector3 circleDir = Vector3.Cross(targetDir, Vector3.up);
        Vector3 targetPos = target.Center - (Vector2)targetDir * IdealCircleRange
                            + (Vector2)circleDir * Mathf.Sin(Time.time * circleSpeed) * 2f;

        movable.MoveTo(targetPos);
    }
}
