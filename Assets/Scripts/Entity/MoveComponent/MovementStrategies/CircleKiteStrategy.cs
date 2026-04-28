using UnityEngine;

/// <summary>
/// 中距离绕圈策略
/// </summary>
[CreateAssetMenu(fileName = "CircleKiteStrategy", menuName = ScriptableObjectMenuPaths.CIRCLE_KITE_STRATEGY)]
public class CircleKiteStrategy : MovementStrategyBase
{
    [SerializeField] private float circleSpeedRatio = 0.5f;
    [SerializeField] private float IdealCircleRangeRatio = 0.95f;

    public override void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData)
    {
        //指向目标的方向
        Vector2 targetDir = (Vector2)target.Center - (Vector2)self.Center;
        targetDir.Normalize();

        //计算环绕的左右方向
        Vector2 circleDir = new Vector2(-targetDir.y, targetDir.x);

        float attackRange = self.GetComponent<PropertiesManager>().GetPropValue(PropType.DetectionRange);
        Vector2 targetPos = (Vector2)target.Center 
                            - targetDir * IdealCircleRangeRatio * attackRange
                            + circleDir * Mathf.Sin(circleSpeedRatio * movable.Speed) * 2f;

        movable.MoveTo(targetPos);
    }
}
