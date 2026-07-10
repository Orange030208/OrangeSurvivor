using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Applies the Mecha Stone shield modifiers while the shield animation is active.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneShieldCast : MechaStoneTaskBase
{
    private const string SHIELD_MODIFIER_SOURCE = "GolemMechaStoneBoss_Shield";

    private bool modifiersApplied;
    private bool cooldownCommitted;

    public override void OnStart()
    {
        base.OnStart();
        modifiersApplied = false;
        cooldownCommitted = false;
        if (!HasContext)
        {
            return;
        }

        StopMoving();
        FaceTarget();
        ApplyModifiers();
        AudioSfxBridge.RequestPlay(AudioSfxKey.GolemMechaStoneBossShield);
        BeginBossAction(BossData.ShieldAction);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FaceTarget();
        TickBossAction(Time.deltaTime);
        if (!ActionRunner.IsComplete)
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
        ReleaseActionLock();
    }

    private void ApplyModifiers()
    {
        if (modifiersApplied || AttributeManager == null)
        {
            return;
        }

        AttributeManager.AddModifiers(SHIELD_MODIFIER_SOURCE, BossData.ShieldModifiers);
        modifiersApplied = true;
    }

    private void CommitCooldown()
    {
        if (cooldownCommitted || AttackController == null)
        {
            return;
        }

        AttackController.CommitSkillCooldown(GolemMechaStoneBossSO.SHIELD_ACTION_ID, BossData.ShieldCooldown);
        cooldownCommitted = true;
    }

    private void RemoveModifiers()
    {
        if (!modifiersApplied || AttributeManager == null)
        {
            return;
        }

        AttributeManager.RemoveModifiers(SHIELD_MODIFIER_SOURCE);
        modifiersApplied = false;
    }
}
