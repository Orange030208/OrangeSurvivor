using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Moves the boss toward the target using the configured chase strategy.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneChaseTarget : MechaStoneTaskBase
{
    public override void OnStart()
    {
        base.OnStart();
        if (!HasContext)
        {
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
        return TaskStatus.Running;
    }

    public override void OnFixedUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.ChaseMovementStrategy == null)
        {
            return;
        }

        BossBrain.ChaseMovementStrategy.ExecuteMove(TargetEntity);
        FaceTarget();
    }

    public override void OnEnd()
    {
        StopMoving();
    }
}
