using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Plays the boss shoot animation and commits the configured projectile attack once.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossShootAttack : GolemMechaStoneBossTaskBase
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
        Animatable?.PlayState(BossAnimationConfig.ShootHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || !HasTarget || BossBrain?.ShootAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FaceTarget();

        if (Animatable == null || !Animatable.IsCurrentState(BossAnimationConfig.ShootHash))
        {
            return TaskStatus.Running;
        }

        float normalizedTime = Animatable.GetCurrentStateNormalizedTime();
        if (!attackCommitted && normalizedTime >= BossData.ShootCommitNormalizedTime)
        {
            attackCommitted = true;
            BossBrain.ShootAttackStrategy.TryExecute(TargetEntity);
        }

        return normalizedTime >= BossData.ShootFinishNormalizedTime ? TaskStatus.Success : TaskStatus.Running;
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
