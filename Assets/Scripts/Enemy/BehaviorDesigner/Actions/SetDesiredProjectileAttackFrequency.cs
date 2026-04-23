using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired projectile attack frequency to the blackboard.")]
public sealed class SetDesiredProjectileAttackFrequency : EnemyBehaviorDesignerTaskBase
{
    public SharedFloat attackFrequency;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredProjectileAttackFrequency(attackFrequency.Value);
        return TaskStatus.Success;
    }
}
