using System.Collections.Generic;
using UnityEngine;

public readonly struct ActiveBuffSnapshot
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
    public readonly IReadOnlyList<string> Descriptions;

    public ActiveBuffSnapshot(
        string buffId,
        string displayName,
        Sprite icon,
        BuffPolarity polarity,
        int stackCount,
        int maxStackCount,
        bool hasDuration,
        float remainingDurationSeconds,
        float totalDurationSeconds,
        IReadOnlyList<string> descriptions)
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
        Descriptions = descriptions;
    }
}
