using System;
using UnityEngine;

[Serializable]
public struct Wave
{
    [SerializeField] private string name;
    [SerializeField] private float duration;
    [SerializeField] private WaveSegment[] segments;
    [SerializeField] private WaveCompletionType completionType;
    [SerializeField] private WaveTag waveTags;
    [SerializeField] private WaveRewardSnapshot rewardSnapshot;
    [SerializeField] private WaveFlowSnapshot flowSnapshot;

    public string Name => name;
    public float Duration => duration;
    public WaveSegment[] Segments => segments;
    public WaveCompletionType CompletionType => completionType;
    public WaveTag WaveTags => waveTags;
    public WaveRewardSnapshot RewardSnapshot => rewardSnapshot;
    public WaveFlowSnapshot FlowSnapshot => flowSnapshot;

    public Wave(
        string name,
        float duration,
        WaveSegment[] segments,
        WaveCompletionType completionType,
        WaveTag waveTags,
        WaveRewardSnapshot rewardSnapshot,
        WaveFlowSnapshot flowSnapshot)
    {
        this.name = name;
        this.duration = duration;
        this.segments = segments;
        this.completionType = completionType;
        this.waveTags = waveTags;
        this.rewardSnapshot = rewardSnapshot;
        this.flowSnapshot = flowSnapshot;
    }
}
