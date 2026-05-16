#if UNITY_EDITOR
using System.Collections.Generic;

public static class WeaponContentPoolTuningUtility
{
    private const float DefaultShopWeaponWeight = 1f;

    public static ContentPoolEntry CreateRewardEntry(WeaponDataSO weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        ContentPoolEntry entry = new(weapon, weapon.BaseWeight, weapon.WeaponId);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WeaponLevelRollMetadata(WeaponLevelHelper.MinLevel, WeaponLevelHelper.MaxLevel)
        });
        entry.ConfigureRuntimeRules(BuildAvailabilityConditions(weapon), null);
        return entry;
    }

    public static ContentPoolEntry CreateShopEntry(WeaponDataSO weapon, int level)
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
        entry.ConfigureRuntimeRules(BuildAvailabilityConditions(weapon), null);
        return entry;
    }

    public static List<ContentCondition> BuildAvailabilityConditions(WeaponDataSO weapon)
    {
        List<ContentCondition> conditions = new()
        {
            new CurrentWaveCondition(ContentComparisonOperator.GreaterOrEqual, weapon.OpenWave)
        };

        if (weapon.HasCloseWave)
        {
            conditions.Add(new CurrentWaveCondition(ContentComparisonOperator.LessOrEqual, weapon.CloseWave));
        }

        return conditions;
    }
}
#endif
