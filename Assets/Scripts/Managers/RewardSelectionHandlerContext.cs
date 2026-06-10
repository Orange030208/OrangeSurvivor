using System.Collections.Generic;
using UnityEngine;

public sealed class RewardSelectionHandlerContext
{
    public RewardSelectionHandlerContext(
        Player player,
        PlayerLevel playerLevel,
        AccessoryManager accessoryManager,
        WeaponsHolder weaponsHolder,
        int currentWaveNumber,
        IReadOnlyList<RewardCardSO> rewardCards,
        IReadOnlyList<AccessoryDataSO> accessories,
        IReadOnlyList<WeaponDataSO> weapons,
        ContentTierWeightProfileSO tierWeightProfile,
        Object logContext)
    {
        Player = player;
        PlayerLevel = playerLevel;
        AccessoryManager = accessoryManager;
        WeaponsHolder = weaponsHolder;
        CurrentWaveNumber = Mathf.Max(1, currentWaveNumber);
        RewardCards = rewardCards ?? System.Array.Empty<RewardCardSO>();
        Accessories = accessories ?? System.Array.Empty<AccessoryDataSO>();
        Weapons = weapons ?? System.Array.Empty<WeaponDataSO>();
        TierWeightProfile = tierWeightProfile;
        LogContext = logContext;
    }

    public Player Player { get; }
    public PlayerLevel PlayerLevel { get; }
    public AccessoryManager AccessoryManager { get; }
    public WeaponsHolder WeaponsHolder { get; }
    public int CurrentWaveNumber { get; }
    public IReadOnlyList<RewardCardSO> RewardCards { get; }
    public IReadOnlyList<AccessoryDataSO> Accessories { get; }
    public IReadOnlyList<WeaponDataSO> Weapons { get; }
    public ContentTierWeightProfileSO TierWeightProfile { get; }
    public Object LogContext { get; }

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
            snapshot.ShopRerollBasePrice,
            snapshot.ShopRerollStepPrice,
            snapshot.DangerTier);
    }
}
