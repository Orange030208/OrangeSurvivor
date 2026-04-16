/// <summary>
/// 语义化音效键：
/// - UI 与业务层只表达“发生了什么音效语义”；
/// - 具体映射到哪个 cueId 由 AudioSfxCatalogSO 决定。
/// </summary>
public enum AudioSfxKey
{
    None = 0,
    UiClick = 1,
    UiBack = 2,
    UiConfirm = 3,
    ShopPurchaseSuccess = 4,
    ShopPurchaseFailed = 5,
    EnemyHit = 6,
    EnemyCriticalHit = 7,
    DropPickup = 8,
    WoodenButtonClicked
}
