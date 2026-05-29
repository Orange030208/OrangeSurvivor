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
    public readonly IInfoDocumentSource InfoSource;

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
        IInfoDocumentSource infoSource)
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
        InfoSource = infoSource;
    }
}
