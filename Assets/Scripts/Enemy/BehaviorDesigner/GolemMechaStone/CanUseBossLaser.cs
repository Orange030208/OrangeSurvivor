using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss can use laser against the current target.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseBossLaser : GolemMechaStoneBossConditionalBase
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current boss phase. Laser is enabled from this phase.")]
    public SharedInt currentPhase = 1;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("The first phase that can use LaserCast.")]
    public SharedInt minPhase = 2;

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() ||
            !HasTarget ||
            AttackController == null ||
            BossData.LaserRange <= 0f ||
            currentPhase.Value < minPhase.Value ||
            !AttackController.CanUseRuntimeAction(GolemMechaStoneBossSO.LASER_ACTION_ID))
        {
            return TaskStatus.Failure;
        }

        return TargetEntity.IsColliderWithinRange(OwnerEnemy.Center, BossData.LaserRange)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    public override void OnReset()
    {
        base.OnReset();
        currentPhase = 1;
        minPhase = 2;
    }
}
