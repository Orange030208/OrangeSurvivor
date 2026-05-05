using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BonusWaveSpawnModifier : WaveSpawnModifierEffect
{
    private const float MIN_FREQUENCY = 0.01f;
    private const int MIN_SPAWN_COUNT = 1;

    [SerializeField] private string trackId = "Bonus";
    [SerializeField] private EnemySO enemyDefinition;
    [SerializeField] private WaveEnemyTag enemyTags = WaveEnemyTag.Special;
    [SerializeField] private WaveSpawnTriggerMode triggerMode = WaveSpawnTriggerMode.OnceOnEnter;
    [SerializeField] private float spawnFrequency = 1f;
    [SerializeField] private int spawnCountPerBatch = 1;
    [SerializeField] private int maxSpawnBatches = 1;
    [SerializeField] private Vector2 normalizedTimeRange = new(0f, 100f);

    private int currentWaveNumber;
    private int spawnedBatchCount;

    public override string Description
    {
        get
        {
            string enemyName = enemyDefinition != null ? enemyDefinition.name : "指定特殊怪";
            return $"波次中额外生成 {Mathf.Max(MIN_SPAWN_COUNT, spawnCountPerBatch)} 个{enemyName}。";
        }
    }

    public override void OnWaveStarted(WaveSpawnContext context)
    {
        if (!AffectsWave(context))
        {
            return;
        }

        currentWaveNumber = context.WaveNumber;
        spawnedBatchCount = 0;
    }

    public override void AppendSpawnRequests(WaveSpawnModifierContext context, List<WaveSpawnRequest> requests)
    {
        if (context.HasSegment || requests == null || enemyDefinition == null || !AffectsWave(context.SpawnContext))
        {
            return;
        }

        if (currentWaveNumber != context.SpawnContext.WaveNumber)
        {
            currentWaveNumber = context.SpawnContext.WaveNumber;
            spawnedBatchCount = 0;
        }

        int safeMaxSpawnBatches = Mathf.Max(0, maxSpawnBatches);
        if (safeMaxSpawnBatches > 0 && spawnedBatchCount >= safeMaxSpawnBatches)
        {
            return;
        }

        if (!IsInsideTimeRange(context.SpawnContext, out float timeSinceRangeStart))
        {
            return;
        }

        if (!ShouldSpawn(timeSinceRangeStart))
        {
            return;
        }

        requests.Add(new WaveSpawnRequest(
            enemyDefinition,
            Mathf.Max(MIN_SPAWN_COUNT, spawnCountPerBatch),
            enemyTags,
            string.IsNullOrWhiteSpace(trackId) ? "Bonus" : trackId,
            -1));
        spawnedBatchCount++;
    }

    private bool IsInsideTimeRange(WaveSpawnContext context, out float timeSinceRangeStart)
    {
        float start = Mathf.Clamp(normalizedTimeRange.x, 0f, 100f);
        float end = Mathf.Clamp(normalizedTimeRange.y, start, 100f);
        float startTime = start / 100f * context.WaveDuration;
        float endTime = end / 100f * context.WaveDuration;
        timeSinceRangeStart = context.ElapsedTime - startTime;
        return context.ElapsedTime >= startTime && context.ElapsedTime <= endTime;
    }

    private bool ShouldSpawn(float timeSinceRangeStart)
    {
        if (triggerMode == WaveSpawnTriggerMode.OnceOnEnter)
        {
            return spawnedBatchCount == 0;
        }

        float spawnDelay = 1f / Mathf.Max(MIN_FREQUENCY, spawnFrequency);
        return timeSinceRangeStart / spawnDelay >= spawnedBatchCount;
    }
}
