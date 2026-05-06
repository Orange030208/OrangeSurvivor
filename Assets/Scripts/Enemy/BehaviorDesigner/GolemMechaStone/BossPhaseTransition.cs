using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Runs Immune then Glow, updates the boss phase, and removes transition modifiers on exit.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossPhaseTransition : GolemMechaStoneBossTaskBase
{
    private const string PHASE_TRANSITION_MODIFIER_SOURCE = "GolemMechaStoneBoss_PhaseTransition";

    [BehaviorDesigner.Runtime.Tasks.Tooltip("Current boss phase. Phase starts at 1.")]
    public SharedInt currentPhase = 1;

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
            targetPhase = Mathf.Max(1, currentPhase.Value);
            return;
        }

        targetPhase = ResolveTargetPhase();
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

        currentPhase.Value = targetPhase;
        Owner.SetVariableValue("CurrentPhase", targetPhase);
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

    public override void OnReset()
    {
        base.OnReset();
        currentPhase = 1;
    }

    private int ResolveTargetPhase()
    {
        float healthRatio = HealthRatio();
        if (currentPhase.Value < 3 && healthRatio <= BossData.PhaseThreeHealthRatio)
        {
            return 3;
        }

        if (currentPhase.Value < 2 && healthRatio <= BossData.PhaseTwoHealthRatio)
        {
            return 2;
        }

        return Mathf.Max(1, currentPhase.Value);
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
