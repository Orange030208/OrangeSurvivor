using System;

public struct WaveStartedEvent : IGameEvent
{
    public int CurrentWave;
    public int TotalWaves;

    public WaveStartedEvent(int currentWave, int totalWaves)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
    }
}

public struct WaveCompletedEvent : IGameEvent
{
    public int WaveNumber;
    public int TotalWaves;
    public float ElapsedTime;
    public bool HasNextWave;

    public WaveCompletedEvent(int waveNumber)
        : this(waveNumber, 0, 0f, false)
    {
    }

    public WaveCompletedEvent(int waveNumber, int totalWaves, float elapsedTime, bool hasNextWave)
    {
        WaveNumber = waveNumber;
        TotalWaves = totalWaves;
        ElapsedTime = elapsedTime;
        HasNextWave = hasNextWave;
    }
}

public struct AllWavesCompletedEvent : IGameEvent
{
}

public struct WaveProgressEvent : IGameEvent
{
    public float RemainingTime;
    public float TotalTime;

    public WaveProgressEvent(float remainingTime, float totalTime)
    {
        RemainingTime = remainingTime;
        TotalTime = totalTime;
    }
}

public struct WaveRuntimeChangedEvent : IGameEvent
{
    public int CurrentWave;
    public int TotalWaves;
    public bool HasStarted;
    public bool HasMoreWaves;
    public bool IsRunning;
    public float ElapsedTime;
    public float CurrentWaveDuration;

    public WaveRuntimeChangedEvent(int currentWave, int totalWaves, bool hasStarted, bool hasMoreWaves, bool isRunning)
        : this(currentWave, totalWaves, hasStarted, hasMoreWaves, isRunning, 0f, 0f)
    {
    }

    public WaveRuntimeChangedEvent(
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

public struct ChestCollectedEvent : IGameEvent
{
}
