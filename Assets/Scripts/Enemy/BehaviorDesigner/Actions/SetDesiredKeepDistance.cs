using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired keep-distance value to the blackboard.")]
public sealed class SetDesiredKeepDistance : EnemyBehaviorDesignerTaskBase
{
    public SharedFloat desiredDistance;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredKeepDistance(desiredDistance.Value);
        return TaskStatus.Success;
    }
}
