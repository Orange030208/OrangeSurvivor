using System;
using UnityEngine;

[Serializable]
public sealed class AddRandomWeaponCard : FeatureEffectBase
{
    private static ContentPoolRollService weaponRewardRollService = new();
    private static ContentPoolRuntimeState weaponRewardRuntimeState = new();

    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private ContentPoolSO weaponRewardPool;
    [SerializeField] private int level = WeaponLevelHelper.MinLevel;

    public AddRandomWeaponCard()
    {
    }

    public AddRandomWeaponCard(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = WeaponLevelHelper.ClampLevel(level);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        weaponRewardRollService = new ContentPoolRollService();
        weaponRewardRuntimeState = new ContentPoolRuntimeState();
    }

    public override string Description
    {
        get
        {
            int clampedLevel = WeaponLevelHelper.ClampLevel(level);
            return weaponData != null
                ? $"获得 1 把 {clampedLevel} 级{weaponData.ItemName}。"
                : $"获得 1 把随机 {clampedLevel} 级武器。";
        }
    }

    public override void OnInstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        WeaponDataSO selectedWeapon = weaponData != null ? weaponData : RollWeaponReward(weaponsHolder);
        if (selectedWeapon != null)
        {
            weaponsHolder.AddWeapon(selectedWeapon, WeaponLevelHelper.ClampLevel(level));
        }
    }

    private WeaponDataSO RollWeaponReward(WeaponsHolder weaponsHolder)
    {
        ContentPoolSO pool = ResolveWeaponRewardPool();
        if (pool == null)
        {
            Debug.LogError($"[{nameof(AddRandomWeaponCard)}] Missing weapon reward content pool.");
            return null;
        }

        Player player = Context?.OwnerEntity as Player;
        ContentFactSource factSource = player != null
            ? ContentFactSource.ForPlayer(player)
            : new ContentFactSource { WeaponsHolder = weaponsHolder };
        ContentRollResult result = weaponRewardRollService.Roll(
            pool,
            factSource,
            weaponRewardRuntimeState,
            1,
            entry => entry.Content is WeaponDataSO);
        if (!result.HasAny)
        {
            Debug.LogWarning($"[{nameof(AddRandomWeaponCard)}] No weapon could be rolled from {pool.name}.");
            return null;
        }

        ContentRollItem item = result.Items[0];
        weaponRewardRuntimeState.RecordPick(item);
        return item.Content as WeaponDataSO;
    }

    private ContentPoolSO ResolveWeaponRewardPool()
    {
        if (weaponRewardPool != null)
        {
            return weaponRewardPool;
        }

        if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            return provider.WeaponRewardPool;
        }

        return null;
    }
}
