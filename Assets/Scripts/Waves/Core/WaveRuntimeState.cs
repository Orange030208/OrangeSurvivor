using System;
using UnityEngine;

[Serializable]
public struct WaveRuntimeState
{
    public int CurrentWaveIndex;
    public float Timer;
    public bool IsRunning;
    public WaveSegmentRuntimeState[] SegmentStates;
    public WaveCompletionType CompletionType;
    public WaveTag WaveTags;
    public WaveRewardSnapshot RewardSnapshot;
    public WaveFlowSnapshot FlowSnapshot;
    public bool CompletionTriggered;

    public WaveRuntimeState(
        int currentWaveIndex,
        float timer,
        bool isRunning,
        WaveSegmentRuntimeState[] segmentStates,
        WaveCompletionType completionType,
        WaveTag waveTags,
        WaveRewardSnapshot rewardSnapshot,
        WaveFlowSnapshot flowSnapshot,
        bool completionTriggered)
    {
        CurrentWaveIndex = currentWaveIndex;
        Timer = timer;
        IsRunning = isRunning;
        SegmentStates = segmentStates;
        CompletionType = completionType;
        WaveTags = waveTags;
        RewardSnapshot = rewardSnapshot;
        FlowSnapshot = flowSnapshot;
        CompletionTriggered = completionTriggered;
    }

    public static WaveRuntimeState CreateIdle()
    {
        return new WaveRuntimeState(
            -1,
            0f,
            false,
            Array.Empty<WaveSegmentRuntimeState>(),
            WaveCompletionType.DurationElapsed,
            WaveTag.None,
            WaveRewardSnapshot.CreateDefault(),
            WaveFlowSnapshot.CreateDefault(),
            false);
    }
}
