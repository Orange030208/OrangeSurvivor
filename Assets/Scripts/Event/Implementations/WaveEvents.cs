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

    public WaveCompletedEvent(int waveNumber)
    {
        WaveNumber = waveNumber;
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

/// <summary>
/// 由于业务的加载顺序可能快于UI,因此事件可能没有订阅上就触发了,所以重发一份快照帮助ui更新状态
/// </summary>
public struct WaveTransitionSnapshot : IGameEvent
{
}

public struct WaveTransitionPhaseChanged : IGameEvent
{
    public TransitionPhase oldPhase;
    public TransitionPhase newPhase;

    public WaveTransitionPhaseChanged(TransitionPhase oldPhase, TransitionPhase newPhase)
    {
        this.oldPhase = oldPhase;
        this.newPhase = newPhase;
    }
}

public struct RequestWaveHudSnapshotEvent : IGameEvent
{
}