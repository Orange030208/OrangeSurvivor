/// <summary>
/// 语义化音效键：
/// - UI 与业务层只表达“发生了什么音效语义”；
/// - 具体映射到哪个分组与音频资源由 AudioBusSettingsSO 决定。
/// </summary>
public enum AudioSfxKey
{
    None = 0,

    #region UI音效

    UiCancel = 107,
    UiConfirm = 108,
    UiError = 109,

    #endregion

    #region 卡牌音效

    UpgradeCardReveal = 100,
    UpgradeCardCommonSelected = 114,
    UpgradeCardRareSelected = 104,
    UpgradeCardEpicSelected = 105,
    UpgradeCardLegendarySelected = 106,

    #endregion

    #region 商店与背包音效

    ShopPurchaseSucceeded = 110,
    ShopPurchaseFailed = 111,
    ShopRerolled = 112,
    ItemEquipped = 113,
    ItemSold = 115,
    WeaponMerged = 116,
    WeaponLevelUp = 117,

    #endregion

    #region 拾取音效

    CoinCollected = 200,
    ChestOpened = 201,

    #endregion

    #region 武器音效

    Swipe = 2,
    Slap = 3,
    GunshotLight = 300,
    GunshotHeavy = 301,
    EnergyShot = 303,
    SwordSlash = 304,
    SwordHeavySlash = 305,
    DaggerStab = 306,
    StaffSwing = 307,
    StaffProjectileLaunch = 308,
    GenericProjectileLaunch = 309,

    #endregion

    #region 敌人音效

    EnemyHurtGeneric = 400,
    EnemyHurtSkeleton = 401,
    EnemyHurtStone = 402,

    #endregion

    #region BOSS音效

    GolemMechaStoneBossPhaseChanged = 450,
    GolemMechaStoneBossMelee = 451,
    GolemMechaStoneBossShoot = 452,
    GolemMechaStoneBossLaser = 453,
    GolemMechaStoneBossShield = 454,

    #endregion

    #region 流程音效

    PlayerLevelUp = 500,
    WaveCountdownTick = 600,
    StageCompleted = 601,

    #endregion
}
