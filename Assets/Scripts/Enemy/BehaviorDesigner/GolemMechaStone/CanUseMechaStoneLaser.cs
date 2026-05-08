using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss can use laser against the current target.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseMechaStoneLaser : MechaStoneConditionBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() ||
            !HasTarget ||
            AttackController == null ||
            BossBrain.IsActionRunning ||
            !BossBrain.CanUseLaser ||
            BossBrain.LaserDetectionStrategy == null ||
            !BossBrain.LaserDetectionStrategy.IsTargetInRange(TargetEntity) ||
            !AttackController.CanUseSkill(GolemMechaStoneBossSO.LASER_ACTION_ID))
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Success;
    }
}
