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

public enum WaveCompletionReason
{
    Unknown,
    DurationElapsed,
    ClearedAllEnemies,
    BossDefeated
}

public struct WaveCompletedEvent : IGameEvent
{
    public int WaveNumber;
    public int TotalWaves;
    public WaveCompletionReason CompletionReason;
    public float ElapsedTime;
    public bool HasNextWave;

    public WaveCompletedEvent(int waveNumber)
        : this(waveNumber, 0, WaveCompletionReason.Unknown, 0f, false)
    {
    }

    public WaveCompletedEvent(int waveNumber, int totalWaves, WaveCompletionReason completionReason, float elapsedTime, bool hasNextWave)
    {
        WaveNumber = waveNumber;
        TotalWaves = totalWaves;
        CompletionReason = completionReason;
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
    public WaveCompletionType CompletionType;
    public WaveTag WaveTags;

    public WaveRuntimeChangedEvent(int currentWave, int totalWaves, bool hasStarted, bool hasMoreWaves, bool isRunning)
        : this(currentWave, totalWaves, hasStarted, hasMoreWaves, isRunning, 0f, 0f, WaveCompletionType.DurationElapsed, WaveTag.None)
    {
    }

    public WaveRuntimeChangedEvent(
        int currentWave,
        int totalWaves,
        bool hasStarted,
        bool hasMoreWaves,
        bool isRunning,
        float elapsedTime,
        float currentWaveDuration,
        WaveCompletionType completionType,
        WaveTag waveTags)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
        HasStarted = hasStarted;
        HasMoreWaves = hasMoreWaves;
        IsRunning = isRunning;
        ElapsedTime = elapsedTime;
        CurrentWaveDuration = currentWaveDuration;
        CompletionType = completionType;
        WaveTags = waveTags;
    }
}

public struct WaveRewardGrantedEvent : IGameEvent
{
    public int WaveNumber;
    public WaveCompletedEvent WaveCompletedEvent;
    public WaveRewardSnapshot RewardSnapshot;
    public WaveFlowSnapshot FlowSnapshot;

    public WaveRewardGrantedEvent(int waveNumber, WaveCompletedEvent waveCompletedEvent, WaveRewardSnapshot rewardSnapshot, WaveFlowSnapshot flowSnapshot)
    {
        WaveNumber = waveNumber;
        WaveCompletedEvent = waveCompletedEvent;
        RewardSnapshot = rewardSnapshot;
        FlowSnapshot = flowSnapshot;
    }
}

public struct WaveFlowDecisionRequestedEvent : IGameEvent
{
    public WaveCompletedEvent WaveCompletedEvent;
    public WaveRewardSnapshot RewardSnapshot;
    public WaveFlowSnapshot FlowSnapshot;

    public WaveFlowDecisionRequestedEvent(WaveCompletedEvent waveCompletedEvent, WaveRewardSnapshot rewardSnapshot, WaveFlowSnapshot flowSnapshot)
    {
        WaveCompletedEvent = waveCompletedEvent;
        RewardSnapshot = rewardSnapshot;
        FlowSnapshot = flowSnapshot;
    }
}

public struct WaveFlowDecisionEvent : IGameEvent
{
    public GameState NextState;

    public WaveFlowDecisionEvent(GameState nextState)
    {
        NextState = nextState;
    }
}

/// <summary>
/// 由于业务的加载顺序可能快于UI，因此事件可能没有订阅上就触发了，所以重发一份快照帮助 UI 更新状态。
/// </summary>
public struct RequestWaveTransitionStateSnapshotEvent : IGameEvent
{
}

public struct WaveTransitionPhaseChangedEvent : IGameEvent
{
    public TransitionPhase oldPhase;
    public TransitionPhase newPhase;

    public WaveTransitionPhaseChangedEvent(TransitionPhase oldPhase, TransitionPhase newPhase)
    {
        this.oldPhase = oldPhase;
        this.newPhase = newPhase;
    }
}

public struct RequestWaveHudSnapshotEvent : IGameEvent
{
}

public struct RequestWaveRuntimeSnapshotEvent : IGameEvent
{
}

public struct StartFirstWaveRequestedEvent : IGameEvent
{
}

public struct StartNextWaveRequestedEvent : IGameEvent
{
}

public struct StopCurrentWaveRequestedEvent : IGameEvent
{
}

public struct ResetWavesRequestedEvent : IGameEvent
{
}

public struct DefeatAllEnemiesRequestedEvent : IGameEvent
{
}

public struct ChestCollectedEvent : IGameEvent
{
}
