using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss can use the configured shoot attack.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseBossShoot : GolemMechaStoneBossConditionalBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.ShootAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        return BossBrain.ShootAttackStrategy.CanUse(TargetEntity)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
