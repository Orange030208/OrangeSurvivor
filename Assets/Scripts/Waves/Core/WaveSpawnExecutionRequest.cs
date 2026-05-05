using UnityEngine;

public readonly struct WaveSpawnExecutionRequest
{
    public readonly WaveSegment Segment;
    public readonly int SegmentIndex;
    public readonly float CurrentTimer;
    public readonly float WaveDuration;
    public readonly int CurrentWaveIndex;
    public readonly string CurrentWaveId;
    public readonly string CurrentWaveName;
    public readonly Entity SpawnAnchor;
    public readonly Transform SpawnParent;

    public WaveSpawnExecutionRequest(
        WaveSegment segment,
        int segmentIndex,
        float currentTimer,
        float waveDuration,
        int currentWaveIndex,
        string currentWaveId,
        string currentWaveName,
        Entity spawnAnchor,
        Transform spawnParent)
    {
        Segment = segment;
        SegmentIndex = segmentIndex;
        CurrentTimer = currentTimer;
        WaveDuration = waveDuration;
        CurrentWaveIndex = currentWaveIndex;
        CurrentWaveId = currentWaveId;
        CurrentWaveName = currentWaveName;
        SpawnAnchor = spawnAnchor;
        SpawnParent = spawnParent;
    }
}
