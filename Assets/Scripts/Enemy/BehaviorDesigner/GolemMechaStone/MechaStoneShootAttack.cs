using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Plays the Mecha Stone shoot animation and commits the configured projectile attack once.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneShootAttack : MechaStoneTaskBase
{
    private bool attackCommitted;
    private Entity executionTarget;

    public override void OnStart()
    {
        base.OnStart();
        attackCommitted = false;
        executionTarget = TargetEntity;
        if (!HasContext)
        {
            return;
        }

        AcquireActionLock();
        StopMoving();
        FaceTarget();
        Animatable?.PlayState(BossAnimationConfig.ShootHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || BossBrain?.ShootAttackStrategy == null || executionTarget == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FacingController?.FaceTarget(executionTarget);

        if (Animatable == null || !Animatable.IsCurrentState(BossAnimationConfig.ShootHash))
        {
            return TaskStatus.Running;
        }

        float normalizedTime = Animatable.GetCurrentStateNormalizedTime();
        if (!attackCommitted && normalizedTime >= BossData.ShootCommitNormalizedTime)
        {
            attackCommitted = true;
            BossBrain.ShootAttackStrategy.TryExecuteCommitted(executionTarget);
        }

        return normalizedTime >= 1f ? TaskStatus.Success : TaskStatus.Running;
    }

    public override void OnFixedUpdate()
    {
        StopMoving();
    }

    public override void OnEnd()
    {
        StopMoving();
        ReleaseActionLock();
    }
}
