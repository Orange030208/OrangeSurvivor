using UnityEngine;

/// <summary>
/// 音频播放桥接入口：
/// - 统一封装 BGM 播放与停止请求；
/// - 供状态层与业务层直接表达音频意图；
/// - 调用方不直接依赖 AudioManager 实现。
/// </summary>
public static class AudioPlaybackBridge
{
    public static void RequestPlayMusic(AudioBgmKey bgmKey, bool restartIfPlaying = false)
    {
        if (bgmKey == AudioBgmKey.None)
        {
            return;
        }

        YokiFrame.EventKit.Type.Send(new AudioMusicPlayRequestedEvent(bgmKey, restartIfPlaying));
    }

    public static void RequestStopMusic()
    {
        YokiFrame.EventKit.Enum.Send(AudioCommand.MusicStopRequested);
    }

    // 扩展说明：业务层与状态层统一通过这里发出 BGM 播放意图，后续可替换为直接服务调用而不修改调用方。
    public static void RequestPlay(AudioBgmKey bgmKey, bool restartIfPlaying = false)
    {
        RequestPlayMusic(bgmKey, restartIfPlaying);
    }

    // 扩展说明：如需增加淡出、分组停止或按具体键停止，可继续扩展独立请求事件。
    public static void RequestStop(AudioBusType busType)
    {
        if (busType == AudioBusType.Music)
        {
            RequestStopMusic();
            return;
        }

        YokiFrame.EventKit.Type.Send(new AudioStopRequestedEvent(busType));
    }

    public static void RequestStopSfxGroup(string groupId)
    {
        YokiFrame.EventKit.Type.Send(new AudioSfxGroupStopRequestedEvent(groupId));
    }

    public static void RequestSetSfxGroupVolume(string groupId, float volume)
    {
        YokiFrame.EventKit.Type.Send(new AudioSfxGroupVolumeChangedEvent(groupId, volume));
    }
}
