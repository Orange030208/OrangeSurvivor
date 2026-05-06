using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss should enter the next configured health phase.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class ShouldChangeMechaStonePhase : MechaStoneConditionBase
{
    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || BossBrain.IsActionRunning)
        {
            return TaskStatus.Failure;
        }

        return BossBrain.ShouldEnterNextPhase() ? TaskStatus.Success : TaskStatus.Failure;
    }
}
