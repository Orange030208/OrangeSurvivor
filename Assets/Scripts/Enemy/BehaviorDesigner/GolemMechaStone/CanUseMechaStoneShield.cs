using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss phase allows shield casting.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class CanUseMechaStoneShield : MechaStoneConditionBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() ||
            !HasTarget ||
            AttackController == null ||
            BossBrain.IsActionRunning ||
            !BossBrain.CanUseShield ||
            !AttackController.CanUseRuntimeAction(GolemMechaStoneBossSO.SHIELD_ACTION_ID))
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Success;
    }
}
