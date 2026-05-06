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
            BossData.LaserWidth <= 0f ||
            BossBrain.IsActionRunning ||
            !BossBrain.CanUseLaser ||
            !AttackController.CanUseRuntimeAction(GolemMechaStoneBossSO.LASER_ACTION_ID))
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Success;
    }
}
