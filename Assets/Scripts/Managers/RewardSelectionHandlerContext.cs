using UnityEngine;

public sealed class RewardSelectionHandlerContext
{
    public RewardSelectionHandlerContext(
        Player player,
        PlayerLevel playerLevel,
        AccessoryManager accessoryManager,
        WeaponsHolder weaponsHolder,
        ContentHistoryState contentHistoryState,
        int currentWaveNumber,
        ContentPoolSO upgradeCardPool,
        ContentPoolSO chestRewardPool,
        ContentPoolSO weaponRewardPool,
        Object logContext)
    {
        Player = player;
        PlayerLevel = playerLevel;
        AccessoryManager = accessoryManager;
        WeaponsHolder = weaponsHolder;
        ContentHistoryState = contentHistoryState;
        CurrentWaveNumber = Mathf.Max(1, currentWaveNumber);
        UpgradeCardPool = upgradeCardPool;
        ChestRewardPool = chestRewardPool;
        WeaponRewardPool = weaponRewardPool;
        LogContext = logContext;
    }

    public Player Player { get; }
    public PlayerLevel PlayerLevel { get; }
    public AccessoryManager AccessoryManager { get; }
    public WeaponsHolder WeaponsHolder { get; }
    public ContentHistoryState ContentHistoryState { get; }
    public int CurrentWaveNumber { get; }
    public ContentPoolSO UpgradeCardPool { get; }
    public ContentPoolSO ChestRewardPool { get; }
    public ContentPoolSO WeaponRewardPool { get; }
    public Object LogContext { get; }

    public ContentHistoryScope CreateHistoryScope(ContentPoolSO pool, string scopeId = ContentPoolScopeIds.Generic)
    {
        scopeId = ContentPoolScopeIds.Normalize(scopeId);
        string poolId = pool != null ? pool.name : scopeId;
        string ownerId = Player != null ? Player.GetInstanceID().ToString() : string.Empty;
        return new ContentHistoryScope(scopeId, poolId, ownerId);
    }

    public RunProgressionSnapshot CreateWaveProgressionSnapshot()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        if (snapshot.WaveNumber == CurrentWaveNumber)
        {
            return snapshot;
        }

        return new RunProgressionSnapshot(
            CurrentWaveNumber,
            snapshot.TotalWaves,
            snapshot.RunMinutes,
            snapshot.EndlessLoop,
            snapshot.DifficultyCoefficient,
            snapshot.EconomyCoefficient,
            snapshot.ShopPriceMultiplier,
            snapshot.DangerTier);
    }
}
