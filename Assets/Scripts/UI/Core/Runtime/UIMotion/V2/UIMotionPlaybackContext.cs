public readonly struct UIMotionPlaybackContext
{
    public UIMotionPlaybackContext(
        UIMotionPlayer player,
        UIMotionClipDefinition clip,
        UIMotionPlaybackMode playbackMode,
        float delay,
        float durationScale)
    {
        Player = player;
        Clip = clip;
        PlaybackMode = playbackMode;
        Delay = delay;
        DurationScale = durationScale;
    }

    public UIMotionPlayer Player { get; }
    public UIMotionClipDefinition Clip { get; }
    public UIMotionPlaybackMode PlaybackMode { get; }
    public float Delay { get; }
    public float DurationScale { get; }
    public bool IsImmediate => PlaybackMode != UIMotionPlaybackMode.PlayToEnd;
}
