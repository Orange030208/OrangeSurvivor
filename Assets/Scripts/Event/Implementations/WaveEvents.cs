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



public struct WaveRuntimeChangedEvent : IGameEvent

{

    public int CurrentWave;

    public int TotalWaves;

    public bool HasStarted;

    public bool HasMoreWaves;

    public bool IsRunning;



    public WaveRuntimeChangedEvent(int currentWave, int totalWaves, bool hasStarted, bool hasMoreWaves, bool isRunning)

    {

        CurrentWave = currentWave;

        TotalWaves = totalWaves;

        HasStarted = hasStarted;

        HasMoreWaves = hasMoreWaves;

        IsRunning = isRunning;

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

