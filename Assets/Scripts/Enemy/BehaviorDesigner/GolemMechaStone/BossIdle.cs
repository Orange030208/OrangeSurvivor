using BehaviorDesigner.Runtime.Tasks;

[TaskDescription("Stops the boss and plays the configured idle animation.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossIdle : GolemMechaStoneBossTaskBase
{
    public override void OnStart()
    {
        base.OnStart();
        if (!HasContext)
        {
            return;
        }

        StopMoving();
        Animatable?.PlayState(BossAnimationConfig.IdleHash);
    }

    public override TaskStatus OnUpdate()
    {
        return RefreshContext() ? TaskStatus.Success : TaskStatus.Failure;
    }
}
