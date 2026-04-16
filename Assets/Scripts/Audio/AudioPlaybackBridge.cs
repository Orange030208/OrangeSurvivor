using UnityEngine;

/// <summary>
/// 音频播放桥接入口：
/// - 统一封装播放与停止请求；
/// - 供 UI 层与业务层直接表达“音频意图”；
/// - 调用方不直接依赖 AudioManager 实现。
/// </summary>
public static class AudioPlaybackBridge
{
    // 扩展说明：业务层与 UI 层统一通过这里发出底层 cue 播放意图，后续可替换为直接服务调用而不修改调用方。
    public static void RequestPlay(string cueId, bool restartIfPlaying = false)
    {
        GameEventBus.Publish(new AudioPlayRequestedEvent(new AudioPlaybackRequest(cueId, restartIfPlaying)));
    }

    // 扩展说明：如需增加淡出、分组停止或按 cueId 停止，可继续扩展独立请求事件。
    public static void RequestStop(AudioBusType busType)
    {
        GameEventBus.Publish(new AudioStopRequestedEvent(busType));
    }
}
