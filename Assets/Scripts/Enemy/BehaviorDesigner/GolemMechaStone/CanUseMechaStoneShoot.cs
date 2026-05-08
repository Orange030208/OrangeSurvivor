using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss can use the configured shoot attack.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseMechaStoneShoot : MechaStoneConditionBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() ||
            !HasTarget ||
            BossBrain.IsActionRunning ||
            BossBrain.ShootAttackStrategy == null ||
            IsTargetInMeleeRange())
        {
            return TaskStatus.Failure;
        }

        return BossBrain.ShootAttackStrategy.CanUse(TargetEntity)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    private bool IsTargetInMeleeRange()
    {
        return BossBrain.MeleeDetectionStrategy != null &&
               BossBrain.MeleeDetectionStrategy.IsTargetInRange(TargetEntity);
    }
}
