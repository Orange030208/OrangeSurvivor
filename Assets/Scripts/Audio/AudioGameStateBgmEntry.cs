using System;
using UnityEngine;

/// <summary>
/// GameState 对应的 BGM 配置：
/// - 指定状态使用的 cueId；
/// - 指定当目标 cue 与当前 BGM 相同时是否重新播放。
/// </summary>
[Serializable]
public class AudioGameStateBgmEntry
{
    [Tooltip("需要应用该 BGM 配置的 GameState。")]
    [SerializeField] private GameState gameState = GameState.None;
    [Tooltip("该状态对应的 BGM cueId。留空表示进入该状态时停止当前 BGM。")]
    [SerializeField] private string cueId = AudioConstants.DEFAULT_CUE_ID;
    [Tooltip("当当前正在播放的 BGM 与该 cue 相同时，是否仍然重新开始播放。关闭后可跨界面延续当前音乐。")]
    [SerializeField] private bool restartIfAlreadyPlaying = true;

    public GameState GameState => gameState;
    public string CueId => cueId;
    public bool RestartIfAlreadyPlaying => restartIfAlreadyPlaying;

    public void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            cueId = string.Empty;
            return;
        }

        if (!cueId.StartsWith(AudioConstants.CUE_ID_PREFIX, StringComparison.Ordinal))
        {
            cueId = AudioConstants.CUE_ID_PREFIX + cueId;
        }
    }
}
