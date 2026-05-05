public readonly struct WaveSpawnModifierContext
{
    public readonly WaveSpawnContext SpawnContext;
    public readonly WaveSegment Segment;
    public readonly int SegmentIndex;

    public WaveSpawnModifierContext(WaveSpawnContext spawnContext, WaveSegment segment, int segmentIndex)
    {
        SpawnContext = spawnContext;
        Segment = segment;
        SegmentIndex = segmentIndex;
    }

    public bool HasSegment => SegmentIndex >= 0;
}
