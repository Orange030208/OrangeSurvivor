using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Moves the boss toward the target using the configured chase strategy.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneChaseTarget : MechaStoneTaskBase
{
    public override void OnStart()
    {
        base.OnStart();
        if (!HasContext || IsTargetInMeleeRange())
        {
            StopMoving();
            return;
        }

        Animatable?.PlayState(BossAnimationConfig.MoveHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.ChaseMovementStrategy == null)
        {
            StopMoving();
            return TaskStatus.Failure;
        }

        FaceTarget();
        if (IsTargetInMeleeRange())
        {
            StopMoving();
            return TaskStatus.Failure;
        }

        return TaskStatus.Running;
    }

    public override void OnFixedUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.ChaseMovementStrategy == null)
        {
            return;
        }

        if (IsTargetInMeleeRange())
        {
            StopMoving();
            return;
        }

        BossBrain.ChaseMovementStrategy.ExecuteMove(TargetEntity);
        FaceTarget();
    }

    public override void OnEnd()
    {
        StopMoving();
    }

    private bool IsTargetInMeleeRange()
    {
        return BossBrain?.MeleeDetectionStrategy != null &&
               HasTarget &&
               BossBrain.MeleeDetectionStrategy.IsTargetInRange(TargetEntity);
    }
}
