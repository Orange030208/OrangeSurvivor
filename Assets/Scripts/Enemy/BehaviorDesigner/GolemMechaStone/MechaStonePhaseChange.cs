using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Runs Immune then Glow, updates the Mecha Stone phase, and removes transition modifiers on exit.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStonePhaseChange : MechaStoneTaskBase
{
    private const string PHASE_TRANSITION_MODIFIER_SOURCE = "GolemMechaStoneBoss_PhaseTransition";

    private float startTime;
    private int targetPhase;
    private bool modifiersApplied;
    private bool glowStarted;

    public override void OnStart()
    {
        base.OnStart();
        startTime = Time.time;
        modifiersApplied = false;
        glowStarted = false;

        if (!HasContext)
        {
            targetPhase = 1;
            return;
        }

        AcquireActionLock();
        targetPhase = BossBrain.ResolveNextPhase();
        StopMoving();
        ApplyModifiers();
        Animatable?.PlayState(BossAnimationConfig.ImmuneHash);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        FaceTarget();

        float elapsedTime = Time.time - startTime;
        if (!glowStarted && elapsedTime >= BossData.ImmuneDuration)
        {
            glowStarted = true;
            Animatable?.PlayState(BossAnimationConfig.GlowHash);
        }

        if (elapsedTime < BossData.ImmuneDuration + BossData.GlowDuration)
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
        if (modifiersApplied || PropertiesManager == null)
        {
            return;
        }

        PropertiesManager.AddModifiers(PHASE_TRANSITION_MODIFIER_SOURCE, BossData.PhaseTransitionModifiers);
        modifiersApplied = true;
    }

    private void RemoveModifiers()
    {
        if (!modifiersApplied || PropertiesManager == null)
        {
            return;
        }

        PropertiesManager.RemoveModifiers(PHASE_TRANSITION_MODIFIER_SOURCE);
        modifiersApplied = false;
    }
}
