using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired chase stop distance to the blackboard.")]
public sealed class SetDesiredChaseStopDistance : EnemyBehaviorDesignerTaskBase
{
    public SharedFloat stopDistance;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredChaseStopDistance(stopDistance.Value);
        return TaskStatus.Success;
    }
}
