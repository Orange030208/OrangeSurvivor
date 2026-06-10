using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Stage Spawn Director Profile", menuName = ScriptableObjectMenuPaths.STAGE_DIRECTOR_PROFILE, order = 0)]
public sealed class StageDirectorProfileSO : ScriptableObject
{
    [SerializeField] private StageDirectorMode mode = StageDirectorMode.FiniteThenEndless;
    [SerializeField] private WaveDirectorDefinitionSO[] finiteWaves = Array.Empty<WaveDirectorDefinitionSO>();
    [SerializeField] private EndlessDirectorProfileSO endlessProfile;
    [SerializeField] private float directorTickInterval = 0.5f;
    [SerializeField] private int maxCatchUpTicksPerFrame = 3;
    [SerializeField] private SpawnLocationDefinition defaultSpawnLocation = SpawnLocationDefinition.CreateDefault();

    public StageDirectorMode Mode => mode;
    public WaveDirectorDefinitionSO[] FiniteWaves => finiteWaves ?? Array.Empty<WaveDirectorDefinitionSO>();
    public EndlessDirectorProfileSO EndlessProfile => endlessProfile;
    public float DirectorTickInterval => Mathf.Max(0.01f, directorTickInterval);
    public int MaxCatchUpTicksPerFrame => Mathf.Max(1, maxCatchUpTicksPerFrame);
    public SpawnLocationDefinition DefaultSpawnLocation => defaultSpawnLocation;
    public int FiniteWaveCount => FiniteWaves.Length;
    public bool SupportsEndless => mode != StageDirectorMode.FiniteOnly && endlessProfile != null && endlessProfile.HasPhaseCards;

    public void Configure(
        StageDirectorMode mode,
        WaveDirectorDefinitionSO[] finiteWaves,
        EndlessDirectorProfileSO endlessProfile,
        float directorTickInterval,
        int maxCatchUpTicksPerFrame,
        SpawnLocationDefinition defaultSpawnLocation)
    {
        this.mode = mode;
        this.finiteWaves = finiteWaves ?? Array.Empty<WaveDirectorDefinitionSO>();
        this.endlessProfile = endlessProfile;
        this.directorTickInterval = directorTickInterval;
        this.maxCatchUpTicksPerFrame = maxCatchUpTicksPerFrame;
        this.defaultSpawnLocation = defaultSpawnLocation ?? SpawnLocationDefinition.CreateDefault();
        OnValidate();
    }

    private void OnValidate()
    {
        finiteWaves ??= Array.Empty<WaveDirectorDefinitionSO>();
        directorTickInterval = Mathf.Max(0.01f, directorTickInterval);
        maxCatchUpTicksPerFrame = Mathf.Max(1, maxCatchUpTicksPerFrame);
        defaultSpawnLocation ??= SpawnLocationDefinition.CreateDefault();
        defaultSpawnLocation.Validate();
    }
}
