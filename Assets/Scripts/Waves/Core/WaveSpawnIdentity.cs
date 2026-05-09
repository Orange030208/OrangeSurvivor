using UnityEngine;

public readonly struct WaveSpawnIdentity
{
    public string TrackId { get; }
    public WaveSpawnTriggerMode TriggerMode { get; }
    public float SpawnFrequency { get; }
    public int SpawnCountPerBatch { get; }
    public int MaxSpawnBatches { get; }
    public Vector2 NormalizedTimeRange { get; }

    public WaveSpawnIdentity(
        string trackId,
        WaveSpawnTriggerMode triggerMode,
        float spawnFrequency,
        int spawnCountPerBatch,
        int maxSpawnBatches,
        Vector2 normalizedTimeRange)
    {
        TrackId = string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
        TriggerMode = triggerMode;
        SpawnFrequency = spawnFrequency;
        SpawnCountPerBatch = spawnCountPerBatch;
        MaxSpawnBatches = maxSpawnBatches;
        NormalizedTimeRange = normalizedTimeRange;
    }
}
