using UnityEngine;

public class WaveSpawnExecutionService
{
    private readonly EnemyFactory enemyFactory;

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
        if (segment.EnemyDefinition == null || request.SpawnAnchor == null)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        Vector2 timeRange = segment.TimeStartEnd;
        float tStart = timeRange.x / 100f * request.WaveDuration;
        float tEnd = timeRange.y / 100f * request.WaveDuration;
        if (request.CurrentTimer < tStart || request.CurrentTimer > tEnd)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        float spawnDelay = 1f / segment.SpawnFrequency;
        float timeSinceSegmentStart = request.CurrentTimer - tStart;
        if (timeSinceSegmentStart / spawnDelay < segmentState.SpawnedCount)
        {
            return WaveSpawnExecutionResult.Skip(segmentState);
        }

        Player player = request.SpawnAnchor as Player;
        for (int i = 0; i < segment.SpawnCountPerBatch; i++)
        {
            Vector3 spawnPosition = spawnPositionResolver.Resolve(new SpawnContext(request.SpawnAnchor, request.CurrentTimer, request.CurrentWaveIndex));
            enemyFactory.Spawn(segment.EnemyDefinition, player, spawnPosition, request.SpawnParent);
        }

        WaveSegmentRuntimeState nextState = segmentState;
        nextState.SpawnedCount++;
        nextState.LastSpawnTime = request.CurrentTimer;
        nextState.HasSpawned = true;
        return WaveSpawnExecutionResult.Spawned(nextState);
    }
}
