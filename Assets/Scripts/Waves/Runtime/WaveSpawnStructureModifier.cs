using System;
using UnityEngine;

[Serializable]
/// <summary>
/// 只调整刷怪轨道节奏；敌人类型、标签和权重必须配置在 WaveSpawn ContentPool 中。
/// </summary>
public sealed class WaveSpawnStructureModifier : WaveSpawnModifier
{
    [SerializeField] private string targetTrackId;
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

        schedule.SpawnFrequency = Mathf.Max(0.01f, schedule.SpawnFrequency * Mathf.Max(0f, frequencyMultiplier));
        schedule.SpawnCountPerBatch = Mathf.Max(1, Mathf.RoundToInt(schedule.SpawnCountPerBatch * Mathf.Max(0f, spawnCountMultiplier)) + spawnCountAdd);
        if (schedule.MaxSpawnBatches > 0 || maxSpawnBatchesAdd != 0)
        {
            schedule.MaxSpawnBatches = Mathf.Max(0, schedule.MaxSpawnBatches + maxSpawnBatchesAdd);
        }

        schedule.NormalizedTimeRange += normalizedTimeRangeOffset;
        schedule.Validate();
    }
}
