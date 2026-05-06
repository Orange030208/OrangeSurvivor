using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss has a live target.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class HasBossTarget : GolemMechaStoneBossConditionalBase
{
    public override TaskStatus OnUpdate()
    {
        return RefreshContext() && HasTarget ? TaskStatus.Success : TaskStatus.Failure;
    }
}
