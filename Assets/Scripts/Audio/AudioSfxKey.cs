/// <summary>
/// 语义化音效键：
/// - UI 与业务层只表达“发生了什么音效语义”；
/// - 具体映射到哪个 cueId 由 AudioSfxCatalogSO 决定。
/// </summary>
public enum AudioSfxKey
{
    None = 0,

    #region UI音效

    WoodenButtonClicked = 1,
    UpgradeCardCommonReveal = 100,
    UpgradeCardRareReveal = 101,
    UpgradeCardEpicReveal = 102,
    UpgradeCardLegendaryReveal = 103,
    UpgradeCardRareSelected = 104,
    UpgradeCardEpicSelected = 105,
    UpgradeCardLegendarySelected = 106,

    #endregion

    #region 武器音效

    Swipe = 2,
    Slap = 3,

    #endregion
}
