/// <summary>
/// 运行时内建事实的稳定 ID。用于条件、权重规则和 Modifier 在不直接持有 FactDefinitionSO 时读取同一份事实。
/// </summary>
public static class ContentFactIds
{
    public const string CurrentWave = "current_wave";
    public const string Luck = "luck";
    public const string ShopRefreshCount = "shop_refresh_count";
    public const string ShopRerollCount = "shop_reroll_count";
    public const string Character = "character";
    public const string OwnedWeaponCount = "owned_weapon_count";
    public const string WaveId = "wave_id";
    public const string WaveTrackId = "wave_track_id";
    public const string WaveProgressPercent = "wave_progress_percent";
    public const string DifficultyCoefficient = "difficulty_coefficient";
    public const string EconomyCoefficient = "economy_coefficient";
    public const string ShopPriceMultiplier = "shop_price_multiplier";
    public const string EndlessLoop = "endless_loop";
    public const string EndlessWave = "endless_wave";
    public const string DangerTier = "danger_tier";
}
