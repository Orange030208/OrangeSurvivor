using System;

public readonly struct WaveSpawnExecutionResult
{
    public readonly bool DidSpawn;
    public readonly WaveSegmentRuntimeState SegmentState;

    public WaveSpawnExecutionResult(bool didSpawn, WaveSegmentRuntimeState segmentState)
    {
        DidSpawn = didSpawn;
        SegmentState = segmentState;
    }

    public static WaveSpawnExecutionResult Skip(WaveSegmentRuntimeState segmentState)
    {
        return new WaveSpawnExecutionResult(false, segmentState);
    }

    public static WaveSpawnExecutionResult Spawned(WaveSegmentRuntimeState segmentState)
    {
        return new WaveSpawnExecutionResult(true, segmentState);
    }
}
