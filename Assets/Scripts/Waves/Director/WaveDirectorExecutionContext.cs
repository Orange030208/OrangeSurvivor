using UnityEngine;

public readonly struct WaveDirectorExecutionContext
{
    public WaveDirectorExecutionContext(
        int waveIndex,
        int waveNumber,
        string waveId,
        string waveName,
        float elapsedTime,
        float waveDuration,
        Entity spawnAnchor,
        Transform spawnParent,
        RunProgressionService runProgressionService = null)
    {
        WaveIndex = waveIndex;
        WaveNumber = waveNumber;
        WaveId = waveId;
        WaveName = waveName;
        ElapsedTime = elapsedTime;
        WaveDuration = waveDuration;
        SpawnAnchor = spawnAnchor;
        SpawnParent = spawnParent;
        RunProgressionService = runProgressionService;
    }

    public int WaveIndex { get; }
    public int WaveNumber { get; }
    public string WaveId { get; }
    public string WaveName { get; }
    public float ElapsedTime { get; }
    public float WaveDuration { get; }
    public Entity SpawnAnchor { get; }
    public Transform SpawnParent { get; }
    public RunProgressionService RunProgressionService { get; }
    public Player Player => SpawnAnchor as Player;
    public RunProgressionSnapshot ProgressionSnapshot =>
        RunProgressionService != null ? RunProgressionService.CurrentSnapshot : RunProgressionRuntime.CurrentSnapshot;
}
