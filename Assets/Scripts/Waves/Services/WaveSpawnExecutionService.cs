using System.Collections.Generic;
using UnityEngine;

public class WaveSpawnExecutionService
{
    private readonly EnemyFactory enemyFactory;
    private readonly List<WaveEnemySpawnCandidate> candidateBuffer = new();
    private readonly List<WaveSpawnRequest> requestBuffer = new();

    public WaveSpawnExecutionService(EnemyFactory enemyFactory)
    {
        this.enemyFactory = enemyFactory;
    }

    public WaveSpawnExecutionResult Execute(
        WaveSpawnExecutionRequest request,
        WaveSegmentRuntimeState segmentState,
        SpawnPositionResolver spawnPositionResolver)
    {
        WaveSegment segment = request.Segment;
        if (request.SpawnAnchor == null)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        WaveSpawnContext spawnContext = CreateSpawnContext(request);
        WaveSpawnModifierContext modifierContext = new WaveSpawnModifierContext(
            spawnContext,
            segment,
            request.SegmentIndex);
        WaveSpawnSchedule schedule = CreateSchedule(segment);
        ApplyScheduleModifiers(modifierContext, schedule);

        if (!ShouldTrigger(schedule, request.CurrentTimer, request.WaveDuration, segmentState))
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        requestBuffer.Clear();
        WaveSpawnRequest spawnRequest = CreateSpawnRequest(modifierContext, schedule);
        if (spawnRequest != null)
        {
            requestBuffer.Add(spawnRequest);
        }

        AppendModifierRequests(modifierContext, requestBuffer);
        bool spawnedAny = SpawnRequests(requestBuffer, spawnContext, spawnPositionResolver);
        if (!spawnedAny)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        WaveSegmentRuntimeState nextState = segmentState;
        nextState.SpawnedBatchCount++;
        nextState.SpawnedCount += schedule.SpawnCountPerBatch;
        nextState.LastSpawnTime = request.CurrentTimer;
        nextState.HasSpawned = true;
        return WaveSpawnExecutionResult.Spawned(nextState);
    }

    public bool ExecuteModifierOnlyRequests(
        WaveSpawnContext spawnContext,
        SpawnPositionResolver spawnPositionResolver)
    {
        if (spawnContext.SpawnAnchor == null)
        {
            return false;
        }

        WaveSpawnModifierContext modifierContext = new WaveSpawnModifierContext(spawnContext, default, -1);
        requestBuffer.Clear();
        AppendModifierRequests(modifierContext, requestBuffer);
        return SpawnRequests(requestBuffer, spawnContext, spawnPositionResolver);
    }

    private static WaveSpawnContext CreateSpawnContext(WaveSpawnExecutionRequest request)
    {
        return new WaveSpawnContext(
            request.CurrentWaveIndex,
            request.CurrentWaveIndex + 1,
            request.CurrentWaveId,
            request.CurrentWaveName,
            request.CurrentTimer,
            request.WaveDuration,
            request.SpawnAnchor,
            request.SpawnParent);
    }

    private static WaveSpawnSchedule CreateSchedule(WaveSegment segment)
    {
        WaveSpawnSchedule schedule = new WaveSpawnSchedule(
            segment.TriggerMode,
            segment.TimeStartEnd,
            segment.SpawnFrequency,
            segment.SpawnCountPerBatch,
            segment.MaxSpawnBatches);
        schedule.Validate();
        return schedule;
    }

    private static bool ShouldTrigger(
        WaveSpawnSchedule schedule,
        float currentTimer,
        float waveDuration,
        WaveSegmentRuntimeState segmentState)
    {
        Vector2 timeRange = schedule.NormalizedTimeRange;
        float tStart = timeRange.x / 100f * waveDuration;
        float tEnd = timeRange.y / 100f * waveDuration;
        if (currentTimer < tStart || currentTimer > tEnd)
        {
            return false;
        }

        if (schedule.MaxSpawnBatches > 0 && segmentState.SpawnedBatchCount >= schedule.MaxSpawnBatches)
        {
            return false;
        }

        if (schedule.TriggerMode == WaveSpawnTriggerMode.OnceOnEnter)
        {
            return !segmentState.HasSpawned;
        }

        float spawnDelay = 1f / schedule.SpawnFrequency;
        float timeSinceSegmentStart = currentTimer - tStart;
        return timeSinceSegmentStart / spawnDelay >= segmentState.SpawnedBatchCount;
    }

    private void ApplyScheduleModifiers(WaveSpawnModifierContext modifierContext, WaveSpawnSchedule schedule)
    {
        IReadOnlyList<IWaveSpawnModifier> modifiers = WaveSpawnModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].ModifySchedule(modifierContext, schedule);
        }
    }

    private WaveSpawnRequest CreateSpawnRequest(WaveSpawnModifierContext modifierContext, WaveSpawnSchedule schedule)
    {
        WaveEnemySpawnCandidate candidate = SelectEnemyCandidate(modifierContext);
        if (candidate == null || candidate.EnemyDefinition == null)
        {
            return null;
        }

        WaveSpawnRequest request = new WaveSpawnRequest(
            candidate.EnemyDefinition,
            schedule.SpawnCountPerBatch,
            candidate.Tags,
            modifierContext.Segment.TrackId,
            modifierContext.SegmentIndex);

        IReadOnlyList<IWaveSpawnModifier> modifiers = WaveSpawnModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].ModifySpawnRequest(modifierContext, request);
        }

        return request;
    }

    private WaveEnemySpawnCandidate SelectEnemyCandidate(WaveSpawnModifierContext modifierContext)
    {
        candidateBuffer.Clear();
        WaveEnemySpawnOption[] enemyPool = modifierContext.Segment.EnemyPool;
        if (enemyPool != null)
        {
            for (int i = 0; i < enemyPool.Length; i++)
            {
                WaveEnemySpawnOption option = enemyPool[i];
                if (option.EnemyDefinition == null || option.Weight <= 0f)
                {
                    continue;
                }

                candidateBuffer.Add(new WaveEnemySpawnCandidate(
                    option.EnemyDefinition,
                    option.Weight,
                    option.Tags));
            }
        }

        IReadOnlyList<IWaveSpawnModifier> modifiers = WaveSpawnModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].ModifyEnemyCandidates(modifierContext, candidateBuffer);
        }

        return PickWeightedCandidate(candidateBuffer, modifierContext.SpawnContext.Roll01());
    }

    private static WaveEnemySpawnCandidate PickWeightedCandidate(
        List<WaveEnemySpawnCandidate> candidates,
        float roll)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            WaveEnemySpawnCandidate candidate = candidates[i];
            if (candidate != null && candidate.IsValid)
            {
                totalWeight += candidate.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float cursor = Mathf.Clamp01(roll) * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            WaveEnemySpawnCandidate candidate = candidates[i];
            if (candidate == null || !candidate.IsValid)
            {
                continue;
            }

            cursor -= candidate.Weight;
            if (cursor <= 0f)
            {
                return candidate;
            }
        }

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (candidates[i] != null && candidates[i].IsValid)
            {
                return candidates[i];
            }
        }

        return null;
    }

    private static void AppendModifierRequests(
        WaveSpawnModifierContext modifierContext,
        List<WaveSpawnRequest> requests)
    {
        IReadOnlyList<IWaveSpawnModifier> modifiers = WaveSpawnModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].AppendSpawnRequests(modifierContext, requests);
        }
    }

    private bool SpawnRequests(
        List<WaveSpawnRequest> requests,
        WaveSpawnContext context,
        SpawnPositionResolver spawnPositionResolver)
    {
        bool spawnedAny = false;
        Player player = context.Player;
        for (int requestIndex = 0; requestIndex < requests.Count; requestIndex++)
        {
            WaveSpawnRequest spawnRequest = requests[requestIndex];
            if (spawnRequest == null || !spawnRequest.IsValid)
            {
                continue;
            }

            for (int i = 0; i < spawnRequest.SpawnCount; i++)
            {
                Vector3 spawnPosition = spawnPositionResolver.Resolve(new SpawnContext(context.SpawnAnchor, context.ElapsedTime, context.WaveIndex));
                enemyFactory.Spawn(spawnRequest.EnemyDefinition, player, spawnPosition, context.SpawnParent);
                spawnedAny = true;
            }
        }

        return spawnedAny;
    }
}
