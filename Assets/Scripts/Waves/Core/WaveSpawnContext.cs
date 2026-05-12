using UnityEngine;
using Random = UnityEngine.Random;

public readonly struct WaveSpawnContext
{
    public readonly int WaveIndex;
    public readonly int WaveNumber;
    public readonly string WaveId;
    public readonly string WaveName;
    public readonly float ElapsedTime;
    public readonly float WaveDuration;
    public readonly Entity SpawnAnchor;
    public readonly Transform SpawnParent;
    public readonly RunProgressionService RunProgressionService;

    public WaveSpawnContext(
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

    public Player Player => SpawnAnchor as Player;
    public float NormalizedProgress => WaveDuration > 0f ? Mathf.Clamp01(ElapsedTime / WaveDuration) : 0f;
    public RunProgressionSnapshot ProgressionSnapshot =>
        RunProgressionService != null ? RunProgressionService.CurrentSnapshot : RunProgressionRuntime.CurrentSnapshot;

    public float Roll01()
    {
        return Random.value;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return Random.Range(minInclusive, maxExclusive);
    }
}
