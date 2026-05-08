using System.Collections.Generic;
using UnityEngine;

public readonly struct ActiveBuffViewData
{
    public readonly string BuffId;
    public readonly string DisplayName;
    public readonly Sprite Icon;
    public readonly BuffPolarity Polarity;
    public readonly int StackCount;
    public readonly int MaxStackCount;
    public readonly bool HasDuration;
    public readonly float RemainingDurationSeconds;
    public readonly float TotalDurationSeconds;
    public readonly IDescribable Describable;

    public ActiveBuffViewData(
        string buffId,
        string displayName,
        Sprite icon,
        BuffPolarity polarity,
        int stackCount,
        int maxStackCount,
        bool hasDuration,
        float remainingDurationSeconds,
        float totalDurationSeconds,
        IDescribable describable)
    {
        BuffId = buffId;
        DisplayName = displayName;
        Icon = icon;
        Polarity = polarity;
        StackCount = stackCount;
        MaxStackCount = maxStackCount;
        HasDuration = hasDuration;
        RemainingDurationSeconds = remainingDurationSeconds;
        TotalDurationSeconds = totalDurationSeconds;
        Describable = describable;
    }
}
