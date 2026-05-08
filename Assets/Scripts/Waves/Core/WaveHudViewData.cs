public readonly struct WaveHudViewData
{
    public int CurrentWave { get; }
    public int TotalWaves { get; }
    public bool HasStarted { get; }
    public float RemainingTime { get; }
    public float TotalTime { get; }

    public WaveHudViewData(int currentWave, int totalWaves, bool hasStarted, float remainingTime, float totalTime)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        HasStarted = hasStarted;
        RemainingTime = remainingTime;
        TotalTime = totalTime;
    }
}
