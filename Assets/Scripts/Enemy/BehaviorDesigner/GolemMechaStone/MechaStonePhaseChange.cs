using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Plays Immune until the animation finishes, updates the Mecha Stone phase, and removes transition modifiers on exit.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStonePhaseChange : MechaStoneTaskBase
{
    private const string PHASE_TRANSITION_MODIFIER_SOURCE = "GolemMechaStoneBoss_PhaseTransition";

    private int targetPhase;
    private bool modifiersApplied;

    public override void OnStart()
    {
        base.OnStart();
        modifiersApplied = false;

        if (!HasContext)
        {
            targetPhase = 1;
            return;
        }

        targetPhase = BossBrain.ResolveNextPhase();
        StopMoving();
        ApplyModifiers();
        AudioSfxBridge.RequestPlay(AudioSfxKey.GolemMechaStoneBossPhaseChanged);
        BeginBossAction(BossData.PhaseChangeAction);
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

        BossBrain.CommitPhase(targetPhase);
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

        AttributeManager.AddModifiers(PHASE_TRANSITION_MODIFIER_SOURCE, BossData.PhaseTransitionModifiers);
        modifiersApplied = true;
    }

    private void RemoveModifiers()
    {
        if (!modifiersApplied || AttributeManager == null)
        {
            return;
        }

        AttributeManager.RemoveModifiers(PHASE_TRANSITION_MODIFIER_SOURCE);
        modifiersApplied = false;
    }
}
