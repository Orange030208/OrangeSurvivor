using System;
using UnityEngine;

[Serializable]
public struct WaveRuntimeState
{
    public int CurrentWaveIndex;
    public float Timer;
    public bool IsRunning;
    public bool CompletionTriggered;

    public WaveRuntimeState(
        int currentWaveIndex,
        float timer,
        bool isRunning,
        bool completionTriggered)
    {
        CurrentWaveIndex = currentWaveIndex;
        Timer = timer;
        IsRunning = isRunning;
        CompletionTriggered = completionTriggered;
    }

    public static WaveRuntimeState CreateIdle()
    {
        return new WaveRuntimeState(
            -1,
            0f,
            false,
            false);
    }
}
