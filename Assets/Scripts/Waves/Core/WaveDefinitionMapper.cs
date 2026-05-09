using System;
using System.Collections.Generic;
using UnityEngine;

public static class WaveDefinitionMapper
{
    public static Wave[] ToRuntimeWaves(StageDefinitionSO stageDefinition)
    {
        if (stageDefinition == null || stageDefinition.Waves == null || stageDefinition.Waves.Length == 0)
        {
            return Array.Empty<Wave>();
        }

        WaveDefinitionSO[] sourceWaves = stageDefinition.Waves;
        List<Wave> runtimeWaves = new(sourceWaves.Length);
        for (int i = 0; i < sourceWaves.Length; i++)
        {
            WaveDefinitionSO waveDefinition = sourceWaves[i];
            if (waveDefinition == null)
            {
                continue;
            }

            runtimeWaves.Add(ToRuntimeWave(waveDefinition, i));
        }

        return runtimeWaves.ToArray();
    }

    public static Wave ToRuntimeWave(WaveDefinitionSO waveDefinition, int waveIndex)
    {
        if (waveDefinition == null)
        {
            throw new ArgumentNullException(nameof(waveDefinition));
        }

        string waveName = string.IsNullOrWhiteSpace(waveDefinition.DisplayName)
            ? $"Wave {waveIndex + 1}"
            : waveDefinition.DisplayName;
        WaveSegment[] segments = ToRuntimeSegments(waveDefinition.SpawnPlans);
        return new Wave(
            waveDefinition.WaveId,
            waveName,
            waveDefinition.Duration,
            segments);
    }

    public static WaveSegment[] ToRuntimeSegments(WaveSpawnPlan[] spawnPlans)
    {
        if (spawnPlans == null || spawnPlans.Length == 0)
        {
            return Array.Empty<WaveSegment>();
        }

        List<WaveSegment> segments = new(spawnPlans.Length);
        for (int i = 0; i < spawnPlans.Length; i++)
        {
            WaveSpawnPlan spawnPlan = spawnPlans[i];
            Vector2 normalizedTimeRange = NormalizeTimeRange(spawnPlan.NormalizedTimeRange);
            WaveSpawnIdentity spawnIdentity = new WaveSpawnIdentity(
                spawnPlan.TrackId,
                spawnPlan.TriggerMode,
                spawnPlan.SpawnFrequency,
                spawnPlan.SpawnCountPerBatch,
                spawnPlan.MaxSpawnBatches,
                normalizedTimeRange);
            segments.Add(new WaveSegment(spawnIdentity, normalizedTimeRange));
        }

        return segments.ToArray();
    }

    private static Vector2 NormalizeTimeRange(Vector2 normalizedTimeRange)
    {
        float start = Mathf.Clamp(normalizedTimeRange.x, 0f, 100f);
        float end = Mathf.Clamp(normalizedTimeRange.y, start, 100f);
        return new Vector2(start, end);
    }

}
