using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Plays the Mecha Stone melee animation and commits the configured melee attack once.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneMeleeAttack : MechaStoneTaskBase
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
        Animatable?.PlayState(BossAnimationConfig.MeleeHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || BossBrain?.MeleeAttackStrategy == null || executionTarget == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FacingController?.FaceTarget(executionTarget);

        if (Animatable == null || !Animatable.IsCurrentState(BossAnimationConfig.MeleeHash))
        {
            return TaskStatus.Running;
        }

        float normalizedTime = Animatable.GetCurrentStateNormalizedTime();
        if (!attackCommitted && normalizedTime >= BossData.MeleeCommitNormalizedTime)
        {
            attackCommitted = true;
            BossBrain.MeleeAttackStrategy.TryExecuteCommitted(executionTarget);
        }

        return normalizedTime >= BossData.MeleeFinishNormalizedTime ? TaskStatus.Success : TaskStatus.Running;
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
