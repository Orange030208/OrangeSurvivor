using System;
using UnityEngine;

public sealed class BuffRuntimeHandle
{
    private readonly string runtimeSourceId;
    private readonly BuffDataSO buffData;
    private readonly bool hasDuration;
    private readonly float totalDurationSeconds;
    private float remainingDurationSeconds;

    public BuffRuntimeHandle(string runtimeSourceId, BuffDataSO buffData, BuffDurationPolicy durationPolicy, float durationSeconds)
    {
        this.runtimeSourceId = runtimeSourceId;
        this.buffData = buffData;
        hasDuration = durationPolicy == BuffDurationPolicy.Timed;
        totalDurationSeconds = hasDuration ? Mathf.Max(0f, durationSeconds) : 0f;
        remainingDurationSeconds = totalDurationSeconds;
    }

    public string RuntimeSourceId => runtimeSourceId;
    public BuffDataSO BuffData => buffData;
    public string BuffId => buffData != null ? buffData.BuffId : string.Empty;
    public bool HasDuration => hasDuration;
    public float RemainingDurationSeconds => remainingDurationSeconds;
    public float TotalDurationSeconds => totalDurationSeconds;
    public bool IsExpired => hasDuration && remainingDurationSeconds <= 0f;

    public void Tick(float deltaTime)
    {
        if (!hasDuration)
        {
            return;
        }

        remainingDurationSeconds = Mathf.Max(0f, remainingDurationSeconds - Mathf.Max(0f, deltaTime));
    }

    public bool RefreshDuration(float durationSeconds)
    {
        if (!hasDuration)
        {
            return false;
        }

        remainingDurationSeconds = Mathf.Max(0f, durationSeconds);
        return true;
    }

    public static ActiveBuffSnapshot CreateMergedSnapshot(BuffDataSO buffData, int stackCount, int maxStackCount, float remainingDurationSeconds, float totalDurationSeconds)
    {
        if (buffData == null)
        {
            return new ActiveBuffSnapshot(string.Empty, string.Empty, null, BuffPolarity.Neutral, 0, 0, false, 0f, 0f, IDescribable.Default);
        }

        bool hasDuration = totalDurationSeconds > 0f;
        BuffDescriptionContext descriptionContext = new(
            stackCount,
            maxStackCount,
            hasDuration,
            remainingDurationSeconds,
            totalDurationSeconds);

        return new ActiveBuffSnapshot(
            buffData.BuffId,
            buffData.DisplayName,
            buffData.Icon,
            buffData.Polarity,
            stackCount,
            maxStackCount,
            hasDuration,
            remainingDurationSeconds,
            totalDurationSeconds,
            buffData);
    }
}
