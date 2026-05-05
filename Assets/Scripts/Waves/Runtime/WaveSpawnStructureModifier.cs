using System;
using UnityEngine;

[Serializable]
public sealed class WaveSpawnStructureModifier : WaveSpawnModifierEffect
{
    [SerializeField] private string targetTrackId;
    [SerializeField] private WaveEnemyTag targetTags = WaveEnemyTag.None;
    [SerializeField] private float frequencyMultiplier = 1f;
    [SerializeField] private float spawnCountMultiplier = 1f;
    [SerializeField] private int spawnCountAdd;
    [SerializeField] private int maxSpawnBatchesAdd;
    [SerializeField] private Vector2 normalizedTimeRangeOffset;

    public override string Description => "改变匹配轨道的刷怪结构。";

    public override void ModifySchedule(WaveSpawnModifierContext context, WaveSpawnSchedule schedule)
    {
        if (!context.HasSegment || schedule == null || !AffectsWave(context.SpawnContext))
        {
            return;
        }

        if (!MatchesTrack(context.Segment, targetTrackId))
        {
            return;
        }

        if (targetTags != WaveEnemyTag.None && !SegmentHasMatchingTags(context.Segment, targetTags))
        {
            return;
        }

        schedule.SpawnFrequency = Mathf.Max(0.01f, schedule.SpawnFrequency * Mathf.Max(0f, frequencyMultiplier));
        schedule.SpawnCountPerBatch = Mathf.Max(1, Mathf.RoundToInt(schedule.SpawnCountPerBatch * Mathf.Max(0f, spawnCountMultiplier)) + spawnCountAdd);
        if (schedule.MaxSpawnBatches > 0 || maxSpawnBatchesAdd != 0)
        {
            schedule.MaxSpawnBatches = Mathf.Max(0, schedule.MaxSpawnBatches + maxSpawnBatchesAdd);
        }

        schedule.NormalizedTimeRange += normalizedTimeRangeOffset;
        schedule.Validate();
    }

    private static bool SegmentHasMatchingTags(WaveSegment segment, WaveEnemyTag requiredTags)
    {
        WaveEnemySpawnOption[] enemyPool = segment.EnemyPool;
        if (enemyPool == null)
        {
            return false;
        }

        for (int i = 0; i < enemyPool.Length; i++)
        {
            if (MatchesTags(enemyPool[i].Tags, requiredTags))
            {
                return true;
            }
        }

        return false;
    }
}
