public sealed class DisplayConfirmModalContext
{
    public const float DEFAULT_TIMEOUT_SECONDS = 10f;

    public DisplayConfirmModalContext(
        DisplaySettingsSnapshot previousDisplay,
        DisplaySettingsSnapshot targetDisplay,
        float timeoutSeconds = DEFAULT_TIMEOUT_SECONDS)
    {
        PreviousDisplay = previousDisplay;
        TargetDisplay = targetDisplay;
        TimeoutSeconds = timeoutSeconds > 0f ? timeoutSeconds : DEFAULT_TIMEOUT_SECONDS;
    }

    public DisplaySettingsSnapshot PreviousDisplay { get; }
    public DisplaySettingsSnapshot TargetDisplay { get; }
    public float TimeoutSeconds { get; }
}
