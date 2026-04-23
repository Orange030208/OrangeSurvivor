using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired orbit radius to the blackboard.")]
public sealed class SetDesiredOrbitRadius : EnemyBehaviorDesignerTaskBase
{
    public SharedFloat orbitRadius;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredOrbitRadius(orbitRadius.Value);
        return TaskStatus.Success;
    }
}
