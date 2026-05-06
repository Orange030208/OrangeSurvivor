using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Applies the boss shield modifiers while the shield animation is active.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossShieldCast : GolemMechaStoneBossTaskBase
{
    private const string SHIELD_MODIFIER_SOURCE = "GolemMechaStoneBoss_Shield";

    private float startTime;
    private bool modifiersApplied;
    private bool cooldownCommitted;

    public override void OnStart()
    {
        base.OnStart();
        startTime = Time.time;
        modifiersApplied = false;
        cooldownCommitted = false;
        if (!HasContext)
        {
            return;
        }

        StopMoving();
        FaceTarget();
        ApplyModifiers();
        Animatable?.PlayState(BossAnimationConfig.ShieldCastHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FaceTarget();
        if (Time.time < startTime + BossData.ShieldDuration)
        {
            return TaskStatus.Running;
        }

        CommitCooldown();
        return TaskStatus.Success;
    }

    public override void OnFixedUpdate()
    {
        StopMoving();
    }

    public override void OnEnd()
    {
        StopMoving();
        RemoveModifiers();
    }

    private void ApplyModifiers()
    {
        if (modifiersApplied || PropertiesManager == null)
        {
            return;
        }

        PropertiesManager.AddModifiers(SHIELD_MODIFIER_SOURCE, BossData.ShieldModifiers);
        modifiersApplied = true;
    }

    private void CommitCooldown()
    {
        if (cooldownCommitted || AttackController == null)
        {
            return;
        }

        AttackController.CommitRuntimeCooldown(GolemMechaStoneBossSO.SHIELD_ACTION_ID, BossData.ShieldCooldown);
        cooldownCommitted = true;
    }

    private void RemoveModifiers()
    {
        if (!modifiersApplied || PropertiesManager == null)
        {
            return;
        }

        PropertiesManager.RemoveModifiers(SHIELD_MODIFIER_SOURCE);
        modifiersApplied = false;
    }
}
