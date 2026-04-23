using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired direct attack frequency to the blackboard.")]
public sealed class SetDesiredDirectAttackFrequency : EnemyBehaviorDesignerTaskBase
{
    public SharedFloat attackFrequency;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredDirectAttackFrequency(attackFrequency.Value);
        return TaskStatus.Success;
    }
}
