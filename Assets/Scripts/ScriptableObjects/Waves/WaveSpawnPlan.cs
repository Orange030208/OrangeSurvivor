using System;
using UnityEngine;

/// <summary>
/// 单条刷怪计划。
/// 描述一条刷怪轨道在一段归一化时间区间内的触发节奏；敌人候选统一由 Wave Spawn ContentPool 决定。
/// </summary>
[Serializable]
public struct WaveSpawnPlan
{
    private const int MIN_SPAWN_COUNT_PER_BATCH = 1;
    private const float MIN_SPAWN_FREQUENCY = 0.01f;

    [SerializeField] private string trackId;
    [SerializeField] private WaveSpawnTriggerMode triggerMode;
    [SerializeField] private float spawnFrequency;
    [SerializeField] private int spawnCountPerBatch;
    [SerializeField] private int maxSpawnBatches;
    [SerializeField] private Vector2 normalizedTimeRange;

    public string TrackId => string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
    public WaveSpawnTriggerMode TriggerMode => triggerMode;
    public float SpawnFrequency => Mathf.Max(MIN_SPAWN_FREQUENCY, spawnFrequency);
    public int SpawnCountPerBatch => Mathf.Max(MIN_SPAWN_COUNT_PER_BATCH, spawnCountPerBatch);
    public int MaxSpawnBatches => Mathf.Max(0, maxSpawnBatches);
    public Vector2 NormalizedTimeRange => normalizedTimeRange;

    public WaveSpawnPlan(
        string trackId,
        WaveSpawnTriggerMode triggerMode,
        float spawnFrequency,
        int spawnCountPerBatch,
        int maxSpawnBatches,
        Vector2 normalizedTimeRange)
    {
        this.trackId = string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
        this.triggerMode = triggerMode;
        this.spawnFrequency = spawnFrequency;
        this.spawnCountPerBatch = spawnCountPerBatch;
        this.maxSpawnBatches = maxSpawnBatches;
        this.normalizedTimeRange = normalizedTimeRange;
    }
}
