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

        StopMoving();
        FaceTarget();
        BeginBossAction(BossData.MeleeAction);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext() || BossBrain?.MeleeAttackStrategy == null)
        {
            return TaskStatus.Failure;
        }

        StopMoving();

        TickBossAction(UnityEngine.Time.deltaTime);
        if (!attackCommitted && ActionRunner.ShouldCommit)
        {
            attackCommitted = true;
            ActionRunner.MarkCommitted();
            BossBrain.MeleeAttackStrategy.TryExecuteCommitted(executionTarget);
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
