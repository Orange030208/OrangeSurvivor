using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss phase allows shield casting.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseBossShield : GolemMechaStoneBossConditionalBase
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current boss phase. Shield is enabled from this phase.")]
    public SharedInt currentPhase = 1;
    [BehaviorDesigner.Runtime.Tasks.Tooltip("The first phase that can use ShieldCast.")]
    public SharedInt minPhase = 3;

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() ||
            !HasTarget ||
            AttackController == null ||
            !AttackController.CanUseRuntimeAction(GolemMechaStoneBossSO.SHIELD_ACTION_ID))
        {
            return TaskStatus.Failure;
        }

        return currentPhase.Value >= minPhase.Value ? TaskStatus.Success : TaskStatus.Failure;
    }

    public override void OnReset()
    {
        base.OnReset();
        currentPhase = 1;
        minPhase = 3;
    }
}
