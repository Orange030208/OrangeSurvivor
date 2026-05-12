public readonly struct WaveHudViewData
{
    public int CurrentWave { get; }
    public int TotalWaves { get; }
    public bool HasStarted { get; }
    public float RemainingTime { get; }
    public float TotalTime { get; }
    public bool ShowTimer { get; }

    public WaveHudViewData(int currentWave, int totalWaves, bool hasStarted, float remainingTime, float totalTime)
        : this(currentWave, totalWaves, hasStarted, remainingTime, totalTime, true)
    {
    }

    public WaveHudViewData(
        int currentWave,
        int totalWaves,
        bool hasStarted,
        float remainingTime,
        float totalTime,
        bool showTimer)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        HasStarted = hasStarted;
        RemainingTime = remainingTime;
        TotalTime = totalTime;
        ShowTimer = showTimer;
    }
}
