using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss can use the configured melee attack.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseBossMelee : GolemMechaStoneBossConditionalBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.MeleeAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        return BossBrain.MeleeAttackStrategy.CanUse(TargetEntity)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
