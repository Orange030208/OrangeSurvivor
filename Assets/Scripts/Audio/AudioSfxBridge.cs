/// <summary>
/// 语义音效播放桥接入口：
/// - UI 与业务层只通过 AudioSfxKey 表达播放意图；
/// - 具体映射由 AudioBusSettingsSO 中的 SFX 分组负责。
/// </summary>
public static class AudioSfxBridge
{
    // 扩展说明：后续如需支持按上下文变体、随机权重或角色专属音效，可继续在这里扩展重载。
    public static void RequestPlay(AudioSfxKey sfxKey)
    {
        if (sfxKey == AudioSfxKey.None)
        {
            return;
        }

        GameEventBus.Publish(new AudioSfxPlayRequestedEvent(sfxKey));
    }

    public static void RequestPlay(AudioSfxKey sfxKey, UnityEngine.Vector2 position)
    {
        if (sfxKey == AudioSfxKey.None)
        {
            return;
        }

        GameEventBus.Publish(new AudioSfxPlayRequestedEvent(sfxKey, position));
    }
}
