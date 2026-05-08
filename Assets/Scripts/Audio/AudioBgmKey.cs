/// <summary>
/// 语义化背景音乐键：
/// - 状态层与业务层只表达“播放哪类 BGM”；
/// - 具体绑定到哪个 AudioClip 由音频配置资源决定。
/// </summary>
public enum AudioBgmKey
{
    None = 0,
    Menu = 1,
    CharacterSelection = 2,
    Gameplay = 3,
    GameOver = 4,
    StageComplete = 5,
    Shop = 7,
}
