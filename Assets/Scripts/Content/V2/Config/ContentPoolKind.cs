public enum ContentPoolKind
{
    Generic = 0,
    UpgradeCard = 1,
    ChestReward = 2,
    Shop = 3,
    Drop = 4,
    WaveSpawn = 5,
    WeaponReward = 6
}

public static class ContentPoolKindUtility
{
    public static string ToScopeId(ContentPoolKind kind)
    {
        return kind switch
        {
            ContentPoolKind.UpgradeCard => ContentPoolScopeIds.UpgradeCard,
            ContentPoolKind.ChestReward => ContentPoolScopeIds.ChestReward,
            ContentPoolKind.Shop => ContentPoolScopeIds.Shop,
            ContentPoolKind.Drop => ContentPoolScopeIds.Drop,
            ContentPoolKind.WaveSpawn => ContentPoolScopeIds.WaveSpawn,
            ContentPoolKind.WeaponReward => ContentPoolScopeIds.WeaponReward,
            _ => ContentPoolScopeIds.Generic
        };
    }
}
