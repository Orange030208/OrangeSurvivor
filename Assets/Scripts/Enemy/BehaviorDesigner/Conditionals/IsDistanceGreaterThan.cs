using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Conditionals")]
[TaskDescription("Returns success if distance to target is greater than the threshold.")]
public sealed class IsDistanceGreaterThan : EnemyBehaviorDesignerConditionalBase
{
    public SharedFloat threshold;

    public override TaskStatus OnUpdate()
    {
        if (!HasController() || !behaviorController.HasValidTarget())
        {
            return TaskStatus.Failure;
        }

        return behaviorController.GetDistanceToTarget() > threshold.Value ? TaskStatus.Success : TaskStatus.Failure;
    }
}
