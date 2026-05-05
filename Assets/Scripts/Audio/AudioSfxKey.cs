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
    UiCancel = 107,
    UiConfirm = 108,
    UiError = 109,
    ShopPurchaseSucceeded = 110,
    ShopPurchaseFailed = 111,
    ShopRerolled = 112,

    #endregion

    #region 拾取音效

    CoinCollected = 200,
    ChestCollected = 201,

    #endregion

    #region 武器音效

    Swipe = 2,
    Slap = 3,
    GunshotLight = 300,
    GunshotHeavy = 301,
    GunshotMuffled = 302,
    EnergyShot = 303,
    SwordSlash = 304,
    SwordHeavySlash = 305,
    DaggerStab = 306,
    GenericProjectileImpact = 307,
    HeavyImpact = 308,
    SwordClash = 309,

    #endregion
}
