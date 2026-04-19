using System;

[Serializable]
public struct WaveSegmentRuntimeState
{
    public int SpawnedCount;
    public float LastSpawnTime;
    public bool HasSpawned;

    public WaveSegmentRuntimeState(int spawnedCount, float lastSpawnTime, bool hasSpawned)
    {
        SpawnedCount = spawnedCount;
        LastSpawnTime = lastSpawnTime;
        HasSpawned = hasSpawned;
    }

    public static WaveSegmentRuntimeState CreateDefault()
    {
        return new WaveSegmentRuntimeState(0, 0f, false);
    }
}
