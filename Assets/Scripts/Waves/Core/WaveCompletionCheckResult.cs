using UnityEngine;

public readonly struct WaveCompletionCheckResult
{
    public readonly bool ShouldComplete;
    public readonly WaveCompletionReason CompletionReason;

    public WaveCompletionCheckResult(bool shouldComplete, WaveCompletionReason completionReason)
    {
        ShouldComplete = shouldComplete;
        CompletionReason = completionReason;
    }

    public static WaveCompletionCheckResult Continue()
    {
        return new WaveCompletionCheckResult(false, WaveCompletionReason.Unknown);
    }

    public static WaveCompletionCheckResult Complete(WaveCompletionReason completionReason)
    {
        return new WaveCompletionCheckResult(true, completionReason);
    }
}
