using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Returns success when the boss should enter the next configured health phase.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class ShouldEnterBossPhase : GolemMechaStoneBossConditionalBase
{
    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current boss phase. Phase starts at 1.")]
    public SharedInt currentPhase = 1;

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        float healthRatio = HealthRatio();
        if (currentPhase.Value < 2 && healthRatio <= BossData.PhaseTwoHealthRatio)
        {
            return TaskStatus.Success;
        }

        if (currentPhase.Value < 3 && healthRatio <= BossData.PhaseThreeHealthRatio)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Failure;
    }

    public override void OnReset()
    {
        base.OnReset();
        currentPhase = 1;
    }
}
