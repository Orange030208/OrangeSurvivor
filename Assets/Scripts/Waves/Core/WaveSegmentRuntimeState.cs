using System;

[Serializable]
public struct WaveSegmentRuntimeState
{
    public int SpawnedCount;
    public int SpawnedBatchCount;
    public float LastSpawnTime;
    public bool HasSpawned;

    public WaveSegmentRuntimeState(int spawnedCount, float lastSpawnTime, bool hasSpawned)
        : this(spawnedCount, spawnedCount, lastSpawnTime, hasSpawned)
    {
    }

    public WaveSegmentRuntimeState(int spawnedCount, int spawnedBatchCount, float lastSpawnTime, bool hasSpawned)
    {
        SpawnedCount = spawnedCount;
        SpawnedBatchCount = spawnedBatchCount;
        LastSpawnTime = lastSpawnTime;
        HasSpawned = hasSpawned;
    }

    public static WaveSegmentRuntimeState CreateDefault()
    {
        return new WaveSegmentRuntimeState(0, 0, 0f, false);
    }
}
