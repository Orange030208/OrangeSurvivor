using System;
using UnityEngine;

[Serializable]
public struct WaveRuntimeState
{
    public int CurrentWaveIndex;
    public float Timer;
    public bool IsRunning;
    public WaveSegmentRuntimeState[] SegmentStates;
    public bool CompletionTriggered;

    public WaveRuntimeState(
        int currentWaveIndex,
        float timer,
        bool isRunning,
        WaveSegmentRuntimeState[] segmentStates,
        bool completionTriggered)
    {
        CurrentWaveIndex = currentWaveIndex;
        Timer = timer;
        IsRunning = isRunning;
        SegmentStates = segmentStates;
        CompletionTriggered = completionTriggered;
    }

    public static WaveRuntimeState CreateIdle()
    {
        return new WaveRuntimeState(
            -1,
            0f,
            false,
            Array.Empty<WaveSegmentRuntimeState>(),
            false);
    }
}
