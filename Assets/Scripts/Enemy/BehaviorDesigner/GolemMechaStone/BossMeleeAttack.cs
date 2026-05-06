using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Plays the boss melee animation and commits the configured melee attack once.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossMeleeAttack : GolemMechaStoneBossTaskBase
{
    private bool attackCommitted;

    public override void OnStart()
    {
        base.OnStart();
        attackCommitted = false;
        if (!HasContext)
        {
            return;
        }

        StopMoving();
        FaceTarget();
        Animatable?.PlayState(BossAnimationConfig.MeleeHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.MeleeAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FaceTarget();

        if (Animatable == null || !Animatable.IsCurrentState(BossAnimationConfig.MeleeHash))
        {
            return TaskStatus.Running;
        }

        float normalizedTime = Animatable.GetCurrentStateNormalizedTime();
        if (!attackCommitted && normalizedTime >= BossData.MeleeCommitNormalizedTime)
        {
            attackCommitted = true;
            BossBrain.MeleeAttackStrategy.TryExecute(TargetEntity);
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
    }
}
