using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Conditionals")]
[TaskDescription("Returns success if health ratio is less than the threshold.")]
public sealed class IsHealthRatioLessThan : EnemyBehaviorDesignerConditionalBase
{
    public SharedFloat threshold;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        return behaviorController.GetHealthRatio() < threshold.Value ? TaskStatus.Success : TaskStatus.Failure;
    }
}
