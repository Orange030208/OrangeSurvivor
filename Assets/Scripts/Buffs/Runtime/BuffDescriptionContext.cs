public readonly struct BuffDescriptionContext
{
    public readonly int CurrentStackCount;
    public readonly int MaxStackCount;
    public readonly bool HasDuration;
    public readonly float RemainingDurationSeconds;
    public readonly float TotalDurationSeconds;

    public BuffDescriptionContext(
        int currentStackCount,
        int maxStackCount,
        bool hasDuration,
        float remainingDurationSeconds,
        float totalDurationSeconds)
    {
        CurrentStackCount = currentStackCount;
        MaxStackCount = maxStackCount;
        HasDuration = hasDuration;
        RemainingDurationSeconds = remainingDurationSeconds;
        TotalDurationSeconds = totalDurationSeconds;
    }
}
