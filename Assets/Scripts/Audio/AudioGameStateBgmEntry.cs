using System;
using UnityEngine;

/// <summary>
/// GameState 对应的 BGM 配置：
/// - 指定状态使用的 AudioBgmKey；
/// - 指定当目标 BGM 与当前一致时是否重新播放。
/// </summary>
[Serializable]
public class AudioGameStateBgmEntry
{
    [Tooltip("需要应用该 BGM 配置的 GameState。")]
    [SerializeField] private GameState gameState = GameState.None;
    [Tooltip("该状态对应的 BGM 键。None 表示进入该状态时停止当前 BGM。")]
    [SerializeField] private AudioBgmKey bgmKey = AudioBgmKey.None;
    [Tooltip("当当前正在播放的 BGM 与目标 BGM 相同时，是否仍然重新开始播放。关闭后可跨界面延续当前音乐。")]
    [SerializeField] private bool restartIfAlreadyPlaying = true;

    public GameState GameState => gameState;
    public AudioBgmKey BgmKey => bgmKey;
    public bool RestartIfAlreadyPlaying => restartIfAlreadyPlaying;
}
