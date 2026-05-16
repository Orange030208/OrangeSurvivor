using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

public sealed class WeaponAssetTests
{
    private const string WeaponFolder = GameContentAssetPaths.WeaponsData;
    private const string WeaponRewardPoolPath = GameContentAssetPaths.WeaponRewardPool;

    [Test]
    public void WeaponJsonRowsAreReadableAndUnique()
    {
        IReadOnlyList<WeaponJsonWeapon> rows = WeaponJsonReader.ReadDefault();
        Assert.AreEqual(8, rows.Count);

        HashSet<string> weaponIds = new(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; i++)
        {
            WeaponJsonWeapon row = rows[i];
            Assert.IsTrue(weaponIds.Add(row.weaponId), $"Duplicated weaponId: {row.weaponId}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.itemName), row.weaponId);
            Assert.NotNull(row.tags, row.weaponId);
            Assert.NotNull(row.spawnPoints, row.weaponId);
            Assert.AreEqual(WeaponLevelHelper.MaxLevel - WeaponLevelHelper.MinLevel + 1, row.levelStats.Count, row.weaponId);
        }
    }

    [Test]
    public void WeaponAssetsMatchJsonRows()
    {
        IReadOnlyList<WeaponJsonWeapon> rows = WeaponJsonReader.ReadDefault();
        Dictionary<string, WeaponJsonWeapon> rowsById = ToRowsById(rows);
        WeaponDataSO[] weapons = LoadWeapons();

        Assert.AreEqual(rows.Count, weapons.Length);
        HashSet<string> weaponIds = new(StringComparer.Ordinal);
        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponDataSO weapon = weapons[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(weapon.WeaponId), weapon.name);
            Assert.IsTrue(weaponIds.Add(weapon.WeaponId), $"Duplicated weaponId: {weapon.WeaponId}");
            Assert.IsTrue(rowsById.TryGetValue(weapon.WeaponId, out WeaponJsonWeapon row), weapon.WeaponId);

            Assert.AreEqual(row.itemName, weapon.ItemName, weapon.WeaponId);
            Assert.AreEqual(row.itemPrice, weapon.ItemPrice, weapon.WeaponId);
            Assert.AreEqual(row.itemDescription, weapon.Description, weapon.WeaponId);
            Assert.AreEqual(ItemType.Weapon, weapon.ItemType, weapon.WeaponId);
            Assert.AreEqual(Math.Max(1, row.openWave), weapon.OpenWave, weapon.WeaponId);
            Assert.AreEqual(Math.Max(0, row.closeWave), weapon.CloseWave, weapon.WeaponId);
            Assert.That(weapon.BaseWeight, Is.EqualTo(Math.Max(0f, row.baseWeight)).Within(0.0001f), weapon.WeaponId);
            Assert.That(weapon.VisualForwardAngle, Is.EqualTo(row.visualForwardAngle).Within(0.0001f), weapon.WeaponId);
            Assert.AreEqual(row.holdAimWhenAttackReady, weapon.HoldAimWhenAttackReady, weapon.WeaponId);
            Assert.That(weapon.AttackSequenceOccupancy, Is.EqualTo(UnityEngine.Mathf.Clamp(row.attackSequenceOccupancy, 0.1f, 1f)).Within(0.0001f), weapon.WeaponId);
            Assert.AreEqual(ParseEnum<WeaponAttackTimingMode>(row.attackTimingMode), weapon.AttackTimingMode, weapon.WeaponId);
            Assert.AreEqual(ParseEnum<WeaponTargetingMode>(row.targetingMode), weapon.TargetingMode, weapon.WeaponId);
            Assert.AreEqual(row.enableHitBox, weapon.EnableHitBox, weapon.WeaponId);
            AssertTags(row, weapon);
            AssertSpawnPoints(row, weapon);
            AssertLevelStats(row, weapon);
        }
    }

    [Test]
    public void WeaponRewardPoolUsesWeaponIdsWeightsAndWaveConditions()
    {
        IReadOnlyList<WeaponJsonWeapon> rows = WeaponJsonReader.ReadDefault();
        Dictionary<string, WeaponJsonWeapon> rowsById = ToRowsById(rows);
        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(WeaponRewardPoolPath);

        Assert.NotNull(pool);
        Assert.AreEqual(1, pool.DefaultRollCount);
        Assert.IsFalse(pool.AllowDuplicateResults);
        Assert.AreEqual(rows.Count, pool.Entries.Count);

        for (int i = 0; i < pool.Entries.Count; i++)
        {
            ContentPoolEntry entry = pool.Entries[i];
            WeaponDataSO weapon = entry.Content as WeaponDataSO;
            Assert.NotNull(weapon, entry.EntryId);
            Assert.IsTrue(rowsById.TryGetValue(entry.EntryId, out WeaponJsonWeapon row), entry.EntryId);
            Assert.AreSame(weapon, entry.Content);
            Assert.AreEqual(weapon.WeaponId, entry.EntryId);
            Assert.That(entry.BaseWeight, Is.EqualTo(row.baseWeight).Within(0.0001f), entry.EntryId);
            Assert.IsTrue(entry.TryGetMetadata(out WeaponLevelRollMetadata levelMetadata), entry.EntryId);
            Assert.AreEqual(WeaponLevelHelper.MinLevel, levelMetadata.MinLevel, entry.EntryId);
            Assert.AreEqual(WeaponLevelHelper.MaxLevel, levelMetadata.MaxLevel, entry.EntryId);
        }

        ContentRollResult waveOneRoll = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, CreateWeaponRewardContext(1), rows.Count);
        HashSet<string> waveOneIds = ToRolledIds(waveOneRoll);
        Assert.IsTrue(waveOneIds.Contains("Weapon_ArcaneBolt"));
        Assert.IsTrue(waveOneIds.Contains("Weapon_HuntingRifle"));
        Assert.IsTrue(waveOneIds.Contains("Weapon_IronHammer"));
        Assert.IsTrue(waveOneIds.Contains("Weapon_RapidSMG"));
        Assert.IsFalse(waveOneIds.Contains("Weapon_DaggerRing"));
        Assert.IsFalse(waveOneIds.Contains("Weapon_FireWand"));
        Assert.IsFalse(waveOneIds.Contains("Weapon_SummonOrb"));
        Assert.IsFalse(waveOneIds.Contains("Weapon_GreatAxe"));

        ContentRollResult waveFiveRoll = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, CreateWeaponRewardContext(5), rows.Count);
        Assert.AreEqual(rows.Count, ToRolledIds(waveFiveRoll).Count);
    }

    private static ContentRollContext CreateWeaponRewardContext(int waveNumber)
    {
        return new ContentRollContext(
            ContentPoolScopeIds.WeaponReward,
            progressionSnapshot: new RunProgressionSnapshot(waveNumber, 20, 0f, 0, 1f, 1f, 1f, 0));
    }

    private static HashSet<string> ToRolledIds(ContentRollResult result)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < result.Items.Count; i++)
        {
            ids.Add(result.Items[i].EntryId);
        }

        return ids;
    }

    private static WeaponDataSO[] LoadWeapons()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDataSO", new[] { WeaponFolder });
        List<WeaponDataSO> weapons = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            WeaponDataSO weapon = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
            if (weapon != null)
            {
                weapons.Add(weapon);
            }
        }

        return weapons.ToArray();
    }

    private static Dictionary<string, WeaponJsonWeapon> ToRowsById(IReadOnlyList<WeaponJsonWeapon> rows)
    {
        Dictionary<string, WeaponJsonWeapon> result = new(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.IsTrue(result.TryAdd(rows[i].weaponId, rows[i]), $"Duplicated weapon JSON id: {rows[i].weaponId}");
        }

        return result;
    }

    private static void AssertTags(WeaponJsonWeapon row, WeaponDataSO weapon)
    {
        Assert.AreEqual(row.tags.Count, weapon.Tags.Count, weapon.WeaponId);
        for (int i = 0; i < row.tags.Count; i++)
        {
            Assert.AreEqual(ParseEnum<WeaponTag>(row.tags[i]), weapon.Tags[i], $"{weapon.WeaponId} tags[{i}]");
        }
    }

    private static void AssertSpawnPoints(WeaponJsonWeapon row, WeaponDataSO weapon)
    {
        Assert.AreEqual(row.spawnPoints.Count, weapon.SpawnPoints.Count, weapon.WeaponId);
        for (int i = 0; i < row.spawnPoints.Count; i++)
        {
            WeaponJsonSpawnPoint expected = row.spawnPoints[i];
            WeaponSpawnPointDefinition actual = weapon.SpawnPoints[i];
            Assert.AreEqual(expected.id, actual.Id, $"{weapon.WeaponId} spawnPoints[{i}]");
            Assert.That(actual.LocalPosition.x, Is.EqualTo(expected.localPosition.x).Within(0.0001f), $"{weapon.WeaponId} spawnPoints[{i}].x");
            Assert.That(actual.LocalPosition.y, Is.EqualTo(expected.localPosition.y).Within(0.0001f), $"{weapon.WeaponId} spawnPoints[{i}].y");
            Assert.That(actual.LocalRotationOffset, Is.EqualTo(expected.localRotationOffset).Within(0.0001f), $"{weapon.WeaponId} spawnPoints[{i}].rotation");
        }
    }

    private static void AssertLevelStats(WeaponJsonWeapon row, WeaponDataSO weapon)
    {
        Assert.AreEqual(row.levelStats.Count, weapon.LevelStats.Count, weapon.WeaponId);
        for (int i = 0; i < row.levelStats.Count; i++)
        {
            WeaponJsonLevelStat expected = row.levelStats[i];
            WeaponLevelStatData actual = weapon.GetLevelStats(expected.level);
            Assert.AreEqual(expected.level, actual.Level, weapon.WeaponId);
            Assert.That(actual.Attack, Is.EqualTo(expected.attack).Within(0.0001f), weapon.WeaponId);
            Assert.That(actual.AttackSpeed, Is.EqualTo(expected.attackSpeed).Within(0.0001f), weapon.WeaponId);
            Assert.That(actual.CriticalChance, Is.EqualTo(expected.criticalChance).Within(0.0001f), weapon.WeaponId);
            Assert.That(actual.CriticalPercent, Is.EqualTo(expected.criticalPercent).Within(0.0001f), weapon.WeaponId);
            Assert.That(actual.Range, Is.EqualTo(expected.range).Within(0.0001f), weapon.WeaponId);
            Assert.That(actual.KnockbackStrength, Is.EqualTo(expected.knockbackStrength).Within(0.0001f), weapon.WeaponId);
            AssertBenefit(expected.statBenefits, actual.StatBenefits, weapon.WeaponId);
            AssertModifiers(expected.holderModifiers, actual.HolderModifiers, weapon.WeaponId);
        }
    }

    private static void AssertBenefit(WeaponJsonBenefit expected, WeaponBenefitData actual, string context)
    {
        Assert.That(actual.AttackSpeedBenefitPercent, Is.EqualTo(expected.attackSpeedBenefitPercent).Within(0.0001f), context);
        Assert.That(actual.CriticalChanceBenefitPercent, Is.EqualTo(expected.criticalChanceBenefitPercent).Within(0.0001f), context);
        Assert.That(actual.CriticalPercentBenefitPercent, Is.EqualTo(expected.criticalPercentBenefitPercent).Within(0.0001f), context);
        Assert.That(actual.RangeBenefitPercent, Is.EqualTo(expected.rangeBenefitPercent).Within(0.0001f), context);
        Assert.That(actual.KnockbackStrengthBenefitPercent, Is.EqualTo(expected.knockbackStrengthBenefitPercent).Within(0.0001f), context);
        Assert.That(actual.MeleeAttackUsagePercent, Is.EqualTo(expected.meleeAttackUsagePercent).Within(0.0001f), context);
        Assert.That(actual.RangedAttackUsagePercent, Is.EqualTo(expected.rangedAttackUsagePercent).Within(0.0001f), context);
        Assert.That(actual.MagicAttackUsagePercent, Is.EqualTo(expected.magicAttackUsagePercent).Within(0.0001f), context);
        Assert.That(actual.SummonAttackUsagePercent, Is.EqualTo(expected.summonAttackUsagePercent).Within(0.0001f), context);
    }

    private static void AssertModifiers(
        IReadOnlyList<WeaponJsonPropModifier> expected,
        IReadOnlyList<PropModifierData> actual,
        string context)
    {
        Assert.AreEqual(expected.Count, actual.Count, context);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(ParseEnum<PropType>(expected[i].propType), actual[i].propType, context);
            Assert.AreEqual(ParseEnum<PropModifierType>(expected[i].modifierType), actual[i].modifierType, context);
            Assert.That(actual[i].value, Is.EqualTo(expected[i].value).Within(0.0001f), context);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct
    {
        Assert.IsTrue(Enum.TryParse(value, true, out TEnum result), $"Cannot parse '{value}' as {typeof(TEnum).Name}.");
        return result;
    }
}
