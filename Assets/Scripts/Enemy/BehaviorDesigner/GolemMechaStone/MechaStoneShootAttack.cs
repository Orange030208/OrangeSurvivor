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

        StopMoving();
        FaceTarget();
        AudioSfxBridge.RequestPlay(AudioSfxKey.GolemMechaStoneBossShoot);
        BeginBossAction(BossData.ShootAction);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || BossBrain?.ShootAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FacingController?.FaceTarget(executionTarget);

        TickBossAction(UnityEngine.Time.deltaTime);
        if (!attackCommitted && ActionRunner.ShouldCommit)
        {
            attackCommitted = true;
            ActionRunner.MarkCommitted();
            BossBrain.ShootAttackStrategy.TryExecuteCommitted(executionTarget);
        }

        return ActionRunner.IsComplete ? TaskStatus.Success : TaskStatus.Running;
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
