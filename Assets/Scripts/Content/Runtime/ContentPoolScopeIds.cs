public static class ContentPoolScopeIds
{
    public const string Generic = "generic";
    public const string UpgradeCard = "upgrade_card";
    public const string ChestReward = "chest_reward";
    public const string Shop = "shop";
    public const string Drop = "drop";
    public const string WaveSpawn = "wave_spawn";
    public const string WeaponReward = "weapon_reward";

    public static string Normalize(string scopeId)
    {
        return string.IsNullOrWhiteSpace(scopeId) ? Generic : scopeId.Trim();
    }
}
