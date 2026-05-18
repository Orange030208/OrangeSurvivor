#if UNITY_EDITOR
using System.Collections.Generic;

public static class WeaponContentPoolTuningUtility
{
    public const int DefaultOpenWave = 1;
    public const int DefaultCloseWave = 0;
    public const float DefaultRewardWeaponWeight = 1f;
    private const float DefaultShopWeaponWeight = 1f;

    public static ContentPoolEntry CreateRewardEntry(
        WeaponDataSO weapon,
        float baseWeight = DefaultRewardWeaponWeight,
        int openWave = DefaultOpenWave,
        int closeWave = DefaultCloseWave)
    {
        if (weapon == null)
        {
            return null;
        }

        ContentPoolEntry entry = new(weapon, baseWeight, weapon.WeaponId);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WeaponLevelRollMetadata(WeaponLevelHelper.MinLevel, WeaponLevelHelper.MaxLevel)
        });
        entry.ConfigureRuntimeRules(BuildAvailabilityConditions(openWave, closeWave), null);
        return entry;
    }

    public static ContentPoolEntry CreateShopEntry(
        WeaponDataSO weapon,
        int level,
        int openWave = DefaultOpenWave,
        int closeWave = DefaultCloseWave)
    {
        if (weapon == null)
        {
            return null;
        }

        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        ContentPoolEntry entry = new(weapon, DefaultShopWeaponWeight, $"{weapon.WeaponId}_Lv{clampedLevel}");
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WeaponLevelRollMetadata(clampedLevel, clampedLevel),
            new ShopPricingMetadata(1f)
        });
        entry.ConfigureRuntimeRules(BuildAvailabilityConditions(openWave, closeWave), null);
        return entry;
    }

    public static List<ContentCondition> BuildAvailabilityConditions(int openWave, int closeWave)
    {
        int clampedOpenWave = UnityEngine.Mathf.Max(DefaultOpenWave, openWave);
        int clampedCloseWave = UnityEngine.Mathf.Max(DefaultCloseWave, closeWave);
        List<ContentCondition> conditions = new()
        {
            new CurrentWaveCondition(ContentComparisonOperator.GreaterOrEqual, clampedOpenWave)
        };

        if (clampedCloseWave > 0)
        {
            conditions.Add(new CurrentWaveCondition(ContentComparisonOperator.LessOrEqual, clampedCloseWave));
        }

        return conditions;
    }
}
#endif
