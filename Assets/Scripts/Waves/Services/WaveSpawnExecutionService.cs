using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 执行波次轨道节奏，并把敌人候选选择委托给 WaveSpawn ContentPool。
/// </summary>
public class WaveSpawnExecutionService
{
    private readonly EnemyFactory enemyFactory;
    private readonly ContentPoolSO waveSpawnPool;
    private readonly List<WaveSpawnRequest> requestBuffer = new();
    private readonly ContentPoolRollService contentPoolRollService = new();
    private readonly ContentPoolRuntimeState waveSpawnRuntimeState = new();

    public WaveSpawnExecutionService(EnemyFactory enemyFactory, ContentPoolSO waveSpawnPool)
    {
        this.enemyFactory = enemyFactory;
        this.waveSpawnPool = waveSpawnPool;
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
        AppendRolledSpawnRequests(modifierContext, schedule, requestBuffer);
        AppendModifierRequests(modifierContext, requestBuffer);
        int spawnedCount = SpawnRequests(requestBuffer, spawnContext, spawnPositionResolver);
        if (spawnedCount <= 0)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        WaveSegmentRuntimeState nextState = segmentState;
        nextState.SpawnedBatchCount++;
        nextState.SpawnedCount += spawnedCount;
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
        return SpawnRequests(requestBuffer, spawnContext, spawnPositionResolver) > 0;
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

    private void AppendRolledSpawnRequests(
        WaveSpawnModifierContext modifierContext,
        WaveSpawnSchedule schedule,
        List<WaveSpawnRequest> requests)
    {
        // 先从内容池抽取候选，再让波次 Modifier 对最终请求做结构性调整。
        ContentRollItem? item = SelectEnemyContent(modifierContext);
        if (!item.HasValue)
        {
            return;
        }

        ContentRollItem rollItem = item.Value;
        if (rollItem.Content is EnemySO enemyDefinition)
        {
            WaveSpawnRequest request = new WaveSpawnRequest(
                enemyDefinition,
                schedule.SpawnCountPerBatch,
                ResolveEnemyTags(rollItem),
                modifierContext.Segment.TrackId,
                modifierContext.SegmentIndex);
            ApplySpawnRequestModifiers(modifierContext, request);
            requests.Add(request);
            return;
        }

        if (rollItem.Content is WaveSpawnPackSO spawnPack)
        {
            AppendPackSpawnRequests(modifierContext, rollItem, spawnPack, requests);
        }
    }

    private void AppendPackSpawnRequests(
        WaveSpawnModifierContext modifierContext,
        ContentRollItem rollItem,
        WaveSpawnPackSO spawnPack,
        List<WaveSpawnRequest> requests)
    {
        IReadOnlyList<WaveSpawnPackEntry> entries = spawnPack.Entries;
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning($"[{nameof(WaveSpawnExecutionService)}] Wave spawn pack {spawnPack.name} has no entries.");
            return;
        }

        WaveEnemyTag fallbackTags = ResolveEnemyTags(rollItem);
        for (int i = 0; i < entries.Count; i++)
        {
            WaveSpawnPackEntry entry = entries[i];
            if (!entry.IsValid)
            {
                continue;
            }

            // 包条目可独立覆盖标签；未覆盖时继承池条目的 DomainFlags，方便权重项和最终请求保持同一语义。
            WaveSpawnRequest request = new WaveSpawnRequest(
                entry.EnemyDefinition,
                entry.SpawnCount,
                entry.OverrideTags ? entry.EnemyTags : fallbackTags,
                modifierContext.Segment.TrackId,
                modifierContext.SegmentIndex);
            ApplySpawnRequestModifiers(modifierContext, request);
            requests.Add(request);
        }
    }

    private static void ApplySpawnRequestModifiers(WaveSpawnModifierContext modifierContext, WaveSpawnRequest request)
    {
        IReadOnlyList<IWaveSpawnModifier> modifiers = WaveSpawnModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            modifiers[i].ModifySpawnRequest(modifierContext, request);
        }
    }

    private ContentRollItem? SelectEnemyContent(WaveSpawnModifierContext modifierContext)
    {
        if (waveSpawnPool == null)
        {
            Debug.LogError($"[{nameof(WaveSpawnExecutionService)}] Missing wave spawn content pool.");
            return null;
        }

        ContentRollResult result = contentPoolRollService.Roll(
            waveSpawnPool,
            CreateWaveFactSource(modifierContext),
            waveSpawnRuntimeState,
            1,
            entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);
        if (!result.HasAny)
        {
            return null;
        }

        return result.Items[0];
    }

    private static WaveEnemyTag ResolveEnemyTags(ContentRollItem item)
    {
        // WaveEnemyTag 存在内容池条目的 DomainFlags 中，未配置时按普通怪处理。
        WaveEnemyTag tags = (WaveEnemyTag)item.DomainFlags;
        return tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;
    }

    private static ContentFactSource CreateWaveFactSource(WaveSpawnModifierContext modifierContext)
    {
        // 刷怪池条件和 ContentPool Modifier 共享这份事实快照，避免再从旧波次候选字段取上下文。
        Player player = modifierContext.SpawnContext.Player;
        float progressPercent = modifierContext.SpawnContext.WaveDuration > 0f
            ? Mathf.Clamp01(modifierContext.SpawnContext.ElapsedTime / modifierContext.SpawnContext.WaveDuration) * 100f
            : 0f;
        if (player != null)
        {
            ContentFactSource playerSource = ContentFactSource.ForPlayer(player, modifierContext.SpawnContext.WaveNumber);
            playerSource.WaveId = modifierContext.SpawnContext.WaveId;
            playerSource.WaveTrackId = modifierContext.Segment.TrackId;
            playerSource.WaveProgressPercent = progressPercent;
            return playerSource;
        }

        return new ContentFactSource
        {
            WaveNumber = Mathf.Max(1, modifierContext.SpawnContext.WaveNumber),
            WaveId = modifierContext.SpawnContext.WaveId,
            WaveTrackId = modifierContext.Segment.TrackId,
            WaveProgressPercent = progressPercent
        };
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

    private int SpawnRequests(
        List<WaveSpawnRequest> requests,
        WaveSpawnContext context,
        SpawnPositionResolver spawnPositionResolver)
    {
        int spawnedCount = 0;
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
                SpawnContext positionContext = new(context.SpawnAnchor, context.ElapsedTime, context.WaveIndex);
                if (!spawnPositionResolver.TryResolve(positionContext, spawnRequest.EnemyDefinition, out Vector3 spawnPosition))
                {
                    Debug.LogWarning($"[{nameof(WaveSpawnExecutionService)}] Skipped spawning {spawnRequest.EnemyDefinition.name} because no safe spawn position could be resolved.");
                    continue;
                }

                enemyFactory.Spawn(spawnRequest.EnemyDefinition, player, spawnPosition, context.SpawnParent);
                spawnedCount++;
            }
        }

        return spawnedCount;
    }
}
