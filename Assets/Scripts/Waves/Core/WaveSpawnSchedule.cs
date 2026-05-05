using UnityEngine;

public sealed class WaveSpawnSchedule
{
    private const float MIN_FREQUENCY = 0.01f;
    private const int MIN_SPAWN_COUNT = 1;

    public WaveSpawnTriggerMode TriggerMode { get; set; }
    public Vector2 NormalizedTimeRange { get; set; }
    public float SpawnFrequency { get; set; }
    public int SpawnCountPerBatch { get; set; }
    public int MaxSpawnBatches { get; set; }

    public WaveSpawnSchedule(
        WaveSpawnTriggerMode triggerMode,
        Vector2 normalizedTimeRange,
        float spawnFrequency,
        int spawnCountPerBatch,
        int maxSpawnBatches)
    {
        TriggerMode = triggerMode;
        NormalizedTimeRange = normalizedTimeRange;
        SpawnFrequency = spawnFrequency;
        SpawnCountPerBatch = spawnCountPerBatch;
        MaxSpawnBatches = maxSpawnBatches;
    }

    public void Validate()
    {
        float start = Mathf.Clamp(NormalizedTimeRange.x, 0f, 100f);
        float end = Mathf.Clamp(NormalizedTimeRange.y, start, 100f);
        NormalizedTimeRange = new Vector2(start, end);
        SpawnFrequency = Mathf.Max(MIN_FREQUENCY, SpawnFrequency);
        SpawnCountPerBatch = Mathf.Max(MIN_SPAWN_COUNT, SpawnCountPerBatch);
        MaxSpawnBatches = Mathf.Max(0, MaxSpawnBatches);
    }
}
