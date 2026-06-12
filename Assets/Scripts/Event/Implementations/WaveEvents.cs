using System;

public struct WaveStartedEvent
{
    public int CurrentWave;
    public int TotalWaves;

    public WaveStartedEvent(int currentWave, int totalWaves)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
    }
}
public struct WaveCompletedEvent
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

public enum WaveMilestone
{
    AllWavesCompleted
}

public struct WaveProgressEvent
{
    public float RemainingTime;
    public float TotalTime;
    public bool ShowTimer;

    public WaveProgressEvent(float remainingTime, float totalTime)
        : this(remainingTime, totalTime, true)
    {
    }

    public WaveProgressEvent(float remainingTime, float totalTime, bool showTimer)
    {
        RemainingTime = remainingTime;
        TotalTime = totalTime;
        ShowTimer = showTimer;
    }
}

public struct WaveRuntimeChangedEvent
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
