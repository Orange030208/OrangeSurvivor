
namespace Orange.UIFramework
{
    // 播放上下文把 Player、Clip 和本次播放模式传给 Track。
// Track 不需要反查外部状态，只根据这份上下文决定是播放 Tween 还是立即采样。
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
    // 来自 Clip 的整体时长缩放，Track 使用它统一调整自己的 duration。
    public float DurationScale { get; }
    public bool IsImmediate => PlaybackMode != UIMotionPlaybackMode.PlayToEnd;
}
}
