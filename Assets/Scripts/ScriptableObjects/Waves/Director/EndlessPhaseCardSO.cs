using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Endless Phase Card", menuName = ScriptableObjectMenuPaths.ENDLESS_PHASE_CARD, order = 0)]
public sealed class EndlessPhaseCardSO : ScriptableObject
{
    private const float MIN_DURATION = 1f;

    [SerializeField] private string phaseId = "Phase_001";
    [SerializeField] private string displayName = "Phase 1";
    [SerializeField] private float duration = 30f;
    [SerializeField] private WaveCompletionMode completionMode = WaveCompletionMode.TimerOnly;
    [SerializeField] private float totalBudget = 30f;
    [SerializeField] private float alivePressureCap = 10f;
    [SerializeField] private AnimationCurve pacingCurve = WaveDirectorDefinitionSO.CreateDefaultPacingCurve();
    [SerializeField] private SpawnRoleTarget[] compositionTargets = Array.Empty<SpawnRoleTarget>();
    [SerializeField] private EnemyRosterEntry[] roster = Array.Empty<EnemyRosterEntry>();
    [SerializeField] private ScriptedSpawnBeat[] scriptedBeats = Array.Empty<ScriptedSpawnBeat>();
    [SerializeField] private SpawnLocationDefinition spawnLocationOverride;

    public string PhaseId => phaseId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? phaseId : displayName;
    public float Duration => Mathf.Max(MIN_DURATION, duration);
    public WaveCompletionMode CompletionMode => completionMode;
    public float TotalBudget => Mathf.Max(0f, totalBudget);
    public float AlivePressureCap => Mathf.Max(0f, alivePressureCap);
    public AnimationCurve PacingCurve => pacingCurve != null ? pacingCurve : WaveDirectorDefinitionSO.CreateDefaultPacingCurve();
    public SpawnRoleTarget[] CompositionTargets => compositionTargets ?? Array.Empty<SpawnRoleTarget>();
    public EnemyRosterEntry[] Roster => roster ?? Array.Empty<EnemyRosterEntry>();
    public ScriptedSpawnBeat[] ScriptedBeats => scriptedBeats ?? Array.Empty<ScriptedSpawnBeat>();
    public SpawnLocationDefinition SpawnLocationOverride => spawnLocationOverride;

    public void Configure(
        string phaseId,
        string displayName,
        float duration,
        WaveCompletionMode completionMode,
        float totalBudget,
        float alivePressureCap,
        AnimationCurve pacingCurve,
        SpawnRoleTarget[] compositionTargets,
        EnemyRosterEntry[] roster,
        ScriptedSpawnBeat[] scriptedBeats,
        SpawnLocationDefinition spawnLocationOverride = null)
    {
        this.phaseId = phaseId;
        this.displayName = displayName;
        this.duration = duration;
        this.completionMode = completionMode;
        this.totalBudget = totalBudget;
        this.alivePressureCap = alivePressureCap;
        this.pacingCurve = pacingCurve;
        this.compositionTargets = compositionTargets ?? Array.Empty<SpawnRoleTarget>();
        this.roster = roster ?? Array.Empty<EnemyRosterEntry>();
        this.scriptedBeats = scriptedBeats ?? Array.Empty<ScriptedSpawnBeat>();
        this.spawnLocationOverride = spawnLocationOverride;
        OnValidate();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(phaseId))
        {
            phaseId = "Phase_001";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = phaseId;
        }

        duration = Mathf.Max(MIN_DURATION, duration);
        totalBudget = Mathf.Max(0f, totalBudget);
        alivePressureCap = Mathf.Max(0f, alivePressureCap);
        pacingCurve ??= WaveDirectorDefinitionSO.CreateDefaultPacingCurve();
        compositionTargets ??= Array.Empty<SpawnRoleTarget>();
        roster ??= Array.Empty<EnemyRosterEntry>();
        scriptedBeats ??= Array.Empty<ScriptedSpawnBeat>();
        spawnLocationOverride?.Validate();

        for (int i = 0; i < compositionTargets.Length; i++)
        {
            compositionTargets[i].Validate();
        }

        for (int i = 0; i < roster.Length; i++)
        {
            roster[i]?.Validate();
        }

        for (int i = 0; i < scriptedBeats.Length; i++)
        {
            scriptedBeats[i]?.Validate($"{phaseId}_Beat_{i}");
        }
    }
}
