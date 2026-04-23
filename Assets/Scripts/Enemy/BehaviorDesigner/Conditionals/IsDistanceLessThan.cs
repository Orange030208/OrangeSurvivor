using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Conditionals")]
[TaskDescription("Returns success if distance to target is less than the threshold.")]
public sealed class IsDistanceLessThan : EnemyBehaviorDesignerConditionalBase
{
    public SharedFloat threshold;

    public override TaskStatus OnUpdate()
    {
        if (!HasController() || !behaviorController.HasValidTarget())
        {
            return TaskStatus.Failure;
        }
        return behaviorController.GetDistanceToTarget() < threshold.Value ? TaskStatus.Success : TaskStatus.Failure;
    }
}
