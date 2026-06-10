using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Endless Director Profile", menuName = ScriptableObjectMenuPaths.ENDLESS_DIRECTOR_PROFILE, order = 0)]
public sealed class EndlessDirectorProfileSO : ScriptableObject
{
    [SerializeField] private EndlessPhaseCardSO[] phaseCards = Array.Empty<EndlessPhaseCardSO>();
    [SerializeField] private EndlessPhaseSelectionMode phaseSelectionMode = EndlessPhaseSelectionMode.Sequence;
    [SerializeField] private int selectionSeed = 12345;
    [SerializeField] private AnimationCurve budgetGrowth = CreateFlatGrowthCurve();
    [SerializeField] private AnimationCurve durationGrowth = CreateFlatGrowthCurve();
    [SerializeField] private AnimationCurve aliveCapGrowth = CreateFlatGrowthCurve();
    [SerializeField] private EndlessRosterUnlockRule[] unlockRules = Array.Empty<EndlessRosterUnlockRule>();
    [SerializeField] private EndlessMilestoneBeat[] milestoneBeats = Array.Empty<EndlessMilestoneBeat>();

    public EndlessPhaseCardSO[] PhaseCards => phaseCards ?? Array.Empty<EndlessPhaseCardSO>();
    public EndlessPhaseSelectionMode PhaseSelectionMode => phaseSelectionMode;
    public int SelectionSeed => selectionSeed;
    public AnimationCurve BudgetGrowth => budgetGrowth != null ? budgetGrowth : CreateFlatGrowthCurve();
    public AnimationCurve DurationGrowth => durationGrowth != null ? durationGrowth : CreateFlatGrowthCurve();
    public AnimationCurve AliveCapGrowth => aliveCapGrowth != null ? aliveCapGrowth : CreateFlatGrowthCurve();
    public EndlessRosterUnlockRule[] UnlockRules => unlockRules ?? Array.Empty<EndlessRosterUnlockRule>();
    public EndlessMilestoneBeat[] MilestoneBeats => milestoneBeats ?? Array.Empty<EndlessMilestoneBeat>();
    public bool HasPhaseCards => PhaseCards.Length > 0;

    public static AnimationCurve CreateFlatGrowthCurve()
    {
        return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(10f, 1f));
    }

    public void Configure(
        EndlessPhaseCardSO[] phaseCards,
        EndlessPhaseSelectionMode phaseSelectionMode,
        int selectionSeed,
        AnimationCurve budgetGrowth,
        AnimationCurve durationGrowth,
        AnimationCurve aliveCapGrowth,
        EndlessRosterUnlockRule[] unlockRules,
        EndlessMilestoneBeat[] milestoneBeats)
    {
        this.phaseCards = phaseCards ?? Array.Empty<EndlessPhaseCardSO>();
        this.phaseSelectionMode = phaseSelectionMode;
        this.selectionSeed = selectionSeed;
        this.budgetGrowth = budgetGrowth;
        this.durationGrowth = durationGrowth;
        this.aliveCapGrowth = aliveCapGrowth;
        this.unlockRules = unlockRules ?? Array.Empty<EndlessRosterUnlockRule>();
        this.milestoneBeats = milestoneBeats ?? Array.Empty<EndlessMilestoneBeat>();
        OnValidate();
    }

    private void OnValidate()
    {
        phaseCards ??= Array.Empty<EndlessPhaseCardSO>();
        budgetGrowth ??= CreateFlatGrowthCurve();
        durationGrowth ??= CreateFlatGrowthCurve();
        aliveCapGrowth ??= CreateFlatGrowthCurve();
        unlockRules ??= Array.Empty<EndlessRosterUnlockRule>();
        milestoneBeats ??= Array.Empty<EndlessMilestoneBeat>();

        for (int i = 0; i < unlockRules.Length; i++)
        {
            unlockRules[i]?.Validate();
        }

        for (int i = 0; i < milestoneBeats.Length; i++)
        {
            milestoneBeats[i]?.Validate();
        }
    }
}
