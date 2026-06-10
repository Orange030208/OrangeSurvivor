using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave Director Definition", menuName = ScriptableObjectMenuPaths.WAVE_DIRECTOR_DEFINITION, order = 0)]
public sealed class WaveDirectorDefinitionSO : ScriptableObject
{
    private const float MIN_DURATION = 1f;

    [SerializeField] private string waveId = "Wave_001";
    [SerializeField] private string displayName = "Wave 1";
    [SerializeField] private float duration = 30f;
    [SerializeField] private WaveCompletionMode completionMode = WaveCompletionMode.TimerOnly;
    [SerializeField] private float totalBudget = 30f;
    [SerializeField] private float alivePressureCap = 10f;
    [SerializeField] private AnimationCurve pacingCurve = CreateDefaultPacingCurve();
    [SerializeField] private SpawnRoleTarget[] compositionTargets = Array.Empty<SpawnRoleTarget>();
    [SerializeField] private EnemyRosterEntry[] roster = Array.Empty<EnemyRosterEntry>();
    [SerializeField] private ScriptedSpawnBeat[] scriptedBeats = Array.Empty<ScriptedSpawnBeat>();
    [SerializeField] private SpawnLocationDefinition spawnLocationOverride;

    public string WaveId => waveId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? waveId : displayName;
    public float Duration => Mathf.Max(MIN_DURATION, duration);
    public WaveCompletionMode CompletionMode => completionMode;
    public float TotalBudget => Mathf.Max(0f, totalBudget);
    public float AlivePressureCap => Mathf.Max(0f, alivePressureCap);
    public AnimationCurve PacingCurve => pacingCurve != null ? pacingCurve : CreateDefaultPacingCurve();
    public SpawnRoleTarget[] CompositionTargets => compositionTargets ?? Array.Empty<SpawnRoleTarget>();
    public EnemyRosterEntry[] Roster => roster ?? Array.Empty<EnemyRosterEntry>();
    public ScriptedSpawnBeat[] ScriptedBeats => scriptedBeats ?? Array.Empty<ScriptedSpawnBeat>();
    public SpawnLocationDefinition SpawnLocationOverride => spawnLocationOverride;

    public static AnimationCurve CreateDefaultPacingCurve()
    {
        return AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    public void Configure(
        string waveId,
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
        this.waveId = waveId;
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
        if (string.IsNullOrWhiteSpace(waveId))
        {
            waveId = "Wave_001";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = waveId;
        }

        duration = Mathf.Max(MIN_DURATION, duration);
        totalBudget = Mathf.Max(0f, totalBudget);
        alivePressureCap = Mathf.Max(0f, alivePressureCap);
        pacingCurve ??= CreateDefaultPacingCurve();
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
            scriptedBeats[i]?.Validate($"{waveId}_Beat_{i}");
        }
    }
}
