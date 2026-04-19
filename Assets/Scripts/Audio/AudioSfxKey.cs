/// <summary>
/// 语义化音效键：
/// - UI 与业务层只表达“发生了什么音效语义”；
/// - 具体映射到哪个 cueId 由 AudioSfxCatalogSO 决定。
/// </summary>
public enum AudioSfxKey
{
    None,

    #region UI音效

    WoodenButtonClicked,

    #endregion

    #region 武器音效

    Swipe,
    Slap,

    #endregion
}
