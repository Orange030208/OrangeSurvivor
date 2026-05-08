public readonly struct WaveRuntimeViewData
{
    public int CurrentWave { get; }
    public int TotalWaves { get; }
    public bool HasStarted { get; }
    public bool HasMoreWaves { get; }
    public bool IsRunning { get; }
    public float ElapsedTime { get; }
    public float CurrentWaveDuration { get; }

    public WaveRuntimeViewData(
        int currentWave,
        int totalWaves,
        bool hasStarted,
        bool hasMoreWaves,
        bool isRunning,
        float elapsedTime,
        float currentWaveDuration)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        HasStarted = hasStarted;
        HasMoreWaves = hasMoreWaves;
        IsRunning = isRunning;
        ElapsedTime = elapsedTime;
        CurrentWaveDuration = currentWaveDuration;
    }
}
