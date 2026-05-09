#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UpgradeCardContentPoolTuningUtility
{
    private const float DefaultPreviousOfferMultiplier = 0.5f;
    private const float DefaultMatchingTagWeightBonus = 0.15f;
    private const string TagFolder = "Assets/ScriptableObjects/Content/Tags/Upgrade Cards";
    private const string FactFolder = "Assets/ScriptableObjects/Content/Facts";

    public static ContentPoolEntry CreateEntry(UpgradeCardSO card)
    {
        if (card == null)
        {
            return null;
        }

        UpgradeCardPoolTuning tuning = GetTuning(card.CardId);
        ContentPoolEntry entry = new(card, tuning.BaseWeight, card.CardId);
        entry.ConfigureRuntimeLimits(
            0,
            tuning.MaxPickCount,
            tuning.MutuallyExclusiveCardIds);
        entry.ConfigureRuntimeMetadata(0, 0, (int)card.Rarity, 1f);
        entry.ConfigureRuntimeTags(BuildUpgradeCardTags(card.Tags));
        entry.ConfigureRuntimeRules(
            tuning.BuildConditions(),
            BuildUpgradeCardWeightRules(card.Tags));
        return entry;
    }

    private static UpgradeCardPoolTuning GetTuning(string cardId)
    {
        UpgradeCardPoolTuning tuning = new();
        switch (cardId)
        {
            case "attack_training":
                tuning.BaseWeight = 120;
                break;
            case "quick_strike":
                tuning.BaseWeight = 115;
                break;
            case "tough_body":
                tuning.BaseWeight = 110;
                tuning.Exclude("glass_cannon");
                break;
            case "light_steps":
                tuning.BaseWeight = 95;
                break;
            case "eagle_sense":
                tuning.BaseWeight = 90;
                break;
            case "battle_scavenger":
                tuning.BaseWeight = 90;
                break;
            case "armor_reinforcement":
                tuning.BaseWeight = 90;
                break;
            case "life_recovery":
                tuning.BaseWeight = 75;
                break;
            case "critical_basics":
                tuning.BaseWeight = 80;
                break;
            case "field_supplies":
                tuning.BaseWeight = 70;
                tuning.MaxPickCount = 4;
                break;
            case "lucky_stipend":
                tuning.BaseWeight = 68;
                tuning.MaxPickCount = 3;
                break;
            case "heavy_critical":
                tuning.BaseWeight = 70;
                tuning.RequireTag(UpgradeCardTag.Critical, 1);
                break;
            case "lifesteal_instinct":
                tuning.BaseWeight = 65;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.Attack, 1);
                break;
            case "learning_curve":
                tuning.BaseWeight = 92;
                break;
            case "magnetic_belt":
                tuning.BaseWeight = 88;
                break;
            case "steady_breath":
                tuning.BaseWeight = 84;
                break;
            case "patched_armor":
                tuning.BaseWeight = 82;
                break;
            case "weapon_focus":
                tuning.BaseWeight = 75;
                tuning.MaxPickCount = 3;
                tuning.RequireWeapon(LoadWeapon("RangerSaber"));
                break;
            case "battle_frenzy":
                tuning.BaseWeight = 60;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.AttackSpeed, 1);
                break;
            case "swift_start":
                tuning.BaseWeight = 55;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.MoveSpeed, 1);
                break;
            case "first_aid_protocol":
                tuning.BaseWeight = 50;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.Recovery, 1);
                break;
            case "bloodthirst_dose":
                tuning.BaseWeight = 44;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 3;
                tuning.RequireTag(UpgradeCardTag.Recovery, 1);
                tuning.RequireTag(UpgradeCardTag.Attack, 1);
                break;
            case "bargain_instinct":
                tuning.BaseWeight = 55;
                tuning.MaxPickCount = 2;
                tuning.RequireTag(UpgradeCardTag.Economy, 1);
                break;
            case "reroll_coupon":
                tuning.BaseWeight = 52;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.Economy, 1);
                break;
            case "long_barrel":
                tuning.BaseWeight = 66;
                tuning.MinWave = 2;
                tuning.RequireWeaponTag(WeaponTag.Projectile, 1);
                break;
            case "close_quarters":
                tuning.BaseWeight = 62;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.Defense, 1);
                tuning.RequireWeaponTag(WeaponTag.Melee, 1);
                break;
            case "harvest_route":
                tuning.BaseWeight = 56;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.Pickup, 1);
                break;
            case "momentum_engine":
                tuning.BaseWeight = 58;
                tuning.MinWave = 2;
                tuning.RequireTag(UpgradeCardTag.AttackSpeed, 1);
                tuning.RequireTag(UpgradeCardTag.MoveSpeed, 1);
                break;
            case "glass_cannon":
                tuning.BaseWeight = 40;
                tuning.MaxPickCount = 1;
                tuning.Exclude("tough_body");
                break;
            case "sniper_stance":
                tuning.BaseWeight = 36;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 3;
                tuning.RequireTag(UpgradeCardTag.Critical, 1);
                tuning.RequireWeaponTag(WeaponTag.Ranged, 1);
                break;
            case "overloaded_magazine":
                tuning.BaseWeight = 32;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Projectile, 1);
                tuning.RequireWeaponTag(WeaponTag.Projectile, 1);
                break;
            case "blood_pact":
                tuning.BaseWeight = 34;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Attack, 2);
                tuning.Exclude("guardian_oath");
                break;
            case "guardian_oath":
                tuning.BaseWeight = 34;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Defense, 2);
                tuning.Exclude("blood_pact");
                tuning.Exclude("glass_cannon");
                break;
            case "king_ransom":
                tuning.BaseWeight = 30;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 3;
                tuning.RequireTag(UpgradeCardTag.Economy, 2);
                break;
            case "gold_contract":
                tuning.BaseWeight = 35;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 3;
                tuning.RequireTag(UpgradeCardTag.Economy, 1);
                break;
            case "slaughter_rhythm":
                tuning.BaseWeight = 28;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 5;
                tuning.RequireTag(UpgradeCardTag.Attack, 2);
                tuning.RequireTag(UpgradeCardTag.AttackSpeed, 1);
                break;
            case "emergency_core":
                tuning.BaseWeight = 26;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Defense, 1);
                break;
            case "new_weapon_cache":
                tuning.BaseWeight = 30;
                tuning.MaxPickCount = 2;
                tuning.MinWave = 2;
                break;
            case "sun_scepter_cache":
                tuning.BaseWeight = 22;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Ranged, 1);
                break;
            case "weapon_overclock":
                tuning.BaseWeight = 26;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 4;
                tuning.RequireTag(UpgradeCardTag.Weapon, 1);
                break;
            case "arsenal_drop":
                tuning.BaseWeight = 9;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 5;
                tuning.RequireTag(UpgradeCardTag.Weapon, 2);
                break;
            case "duelist_blade":
                tuning.BaseWeight = 48;
                tuning.MaxPickCount = 1;
                break;
            case "sure_critical":
                tuning.BaseWeight = 10;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 6;
                tuning.RequireTag(UpgradeCardTag.Attack, 2);
                tuning.RequireTag(UpgradeCardTag.Critical, 2);
                tuning.RequireWeapon(LoadWeapon("RangerSaber"));
                break;
            case "immortal_second_wind":
                tuning.BaseWeight = 8;
                tuning.MaxPickCount = 1;
                tuning.MinWave = 6;
                tuning.RequireTag(UpgradeCardTag.Defense, 2);
                tuning.RequireTag(UpgradeCardTag.LowHealth, 1);
                break;
        }

        return tuning;
    }

    private static List<ContentTagSO> BuildUpgradeCardTags(IReadOnlyList<UpgradeCardTag> upgradeTags)
    {
        List<ContentTagSO> tags = new();
        if (upgradeTags == null)
        {
            return tags;
        }

        for (int i = 0; i < upgradeTags.Count; i++)
        {
            tags.Add(GetOrCreateContentTag(
                TagFolder,
                $"Upgrade Card {upgradeTags[i]}.asset",
                $"upgrade_card.{upgradeTags[i]}"));
        }

        return tags;
    }

    private static List<ContentWeightRule> BuildUpgradeCardWeightRules(IReadOnlyList<UpgradeCardTag> tags)
    {
        List<ContentWeightRule> rules = new()
        {
            new PreviousRollWeightContentRule(DefaultPreviousOfferMultiplier)
        };

        for (int i = 0; tags != null && i < tags.Count; i++)
        {
            rules.Add(new FactScaleWeightContentRule(
                LoadFact($"Upgrade Card Tag Pick Count {tags[i]}.asset"),
                DefaultMatchingTagWeightBonus,
                0f,
                10f));
        }

        return rules;
    }

    private static ContentTagSO GetOrCreateContentTag(string folderPath, string fileName, string tagId)
    {
        EnsureFolder(folderPath);
        string path = $"{folderPath}/{fileName}";
        ContentTagSO tag = AssetDatabase.LoadAssetAtPath<ContentTagSO>(path);
        if (tag == null)
        {
            tag = ScriptableObject.CreateInstance<ContentTagSO>();
            AssetDatabase.CreateAsset(tag, path);
        }

        tag.InitializeRuntime(tagId);
        EditorUtility.SetDirty(tag);
        return tag;
    }

    private static FactDefinitionSO GetOrCreateOwnedWeaponFact(WeaponDataSO weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        EnsureFolder(FactFolder);
        string stableName = !string.IsNullOrWhiteSpace(weapon.ItemName) ? weapon.ItemName : weapon.name;
        string path = $"{FactFolder}/Owned Weapon {SanitizeFileName(stableName)}.asset";
        FactDefinitionSO fact = AssetDatabase.LoadAssetAtPath<FactDefinitionSO>(path);
        if (fact == null)
        {
            fact = ScriptableObject.CreateInstance<FactDefinitionSO>();
            AssetDatabase.CreateAsset(fact, path);
        }

        SerializedObject serializedObject = new(fact);
        serializedObject.FindProperty("factId").stringValue = $"owned_weapon.{stableName}";
        serializedObject.FindProperty("displayName").stringValue = $"Owned Weapon/{stableName}";
        serializedObject.FindProperty("valueType").enumValueIndex = (int)FactValueType.Bool;
        serializedObject.FindProperty("builtInKind").enumValueIndex = (int)FactDefinitionBuiltInKind.OwnedWeapon;
        serializedObject.FindProperty("weaponData").objectReferenceValue = weapon;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fact);
        return fact;
    }

    private static FactDefinitionSO LoadFact(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<FactDefinitionSO>($"{FactFolder}/{fileName}");
    }

    private static WeaponDataSO LoadWeapon(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<WeaponDataSO>($"Assets/ScriptableObjects/Content/Weapons/{assetName}.asset");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static string SanitizeFileName(string value)
    {
        string result = value;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            result = result.Replace(invalidChars[i], '_');
        }

        return string.IsNullOrWhiteSpace(result) ? "Weapon" : result;
    }

    private sealed class UpgradeCardPoolTuning
    {
        private readonly List<UpgradeCardTagCountRequirement> requiredTagPickCounts = new();
        private readonly List<WeaponTagCountRequirement> requiredWeaponTags = new();
        private readonly List<WeaponDataSO> requiredWeapons = new();
        private readonly List<string> mutuallyExclusiveCardIds = new();

        public int MinWave { get; set; } = 1;
        public float BaseWeight { get; set; } = 100f;
        public int MaxPickCount { get; set; }
        public IReadOnlyList<string> MutuallyExclusiveCardIds => mutuallyExclusiveCardIds;

        public void RequireTag(UpgradeCardTag tag, int minPickCount)
        {
            requiredTagPickCounts.Add(new UpgradeCardTagCountRequirement(tag, minPickCount));
        }

        public void RequireWeaponTag(WeaponTag tag, int minOwnedCount)
        {
            requiredWeaponTags.Add(new WeaponTagCountRequirement(tag, minOwnedCount));
        }

        public void RequireWeapon(WeaponDataSO weapon)
        {
            if (weapon != null)
            {
                requiredWeapons.Add(weapon);
            }
        }

        public void Exclude(string cardId)
        {
            if (!string.IsNullOrWhiteSpace(cardId))
            {
                mutuallyExclusiveCardIds.Add(cardId);
            }
        }

        public List<ContentCondition> BuildConditions()
        {
            List<ContentCondition> conditions = new();
            FactDefinitionSO currentWaveFact = LoadFact("Current Wave.asset");
            if (MinWave > 1)
            {
                conditions.Add(new FactCompareContentCondition(
                    currentWaveFact,
                    ContentFactComparisonOperator.GreaterOrEqual,
                    ContentFactValue.FromInt(MinWave)));
            }

            for (int i = 0; i < requiredTagPickCounts.Count; i++)
            {
                UpgradeCardTagCountRequirement requirement = requiredTagPickCounts[i];
                conditions.Add(new FactCompareContentCondition(
                    LoadFact($"Upgrade Card Tag Pick Count {requirement.Tag}.asset"),
                    ContentFactComparisonOperator.GreaterOrEqual,
                    ContentFactValue.FromInt(requirement.MinPickCount)));
            }

            for (int i = 0; i < requiredWeaponTags.Count; i++)
            {
                WeaponTagCountRequirement requirement = requiredWeaponTags[i];
                conditions.Add(new FactCompareContentCondition(
                    LoadFact($"Owned Weapon Tag Count {requirement.Tag}.asset"),
                    ContentFactComparisonOperator.GreaterOrEqual,
                    ContentFactValue.FromInt(requirement.MinOwnedCount)));
            }

            for (int i = 0; i < requiredWeapons.Count; i++)
            {
                FactDefinitionSO fact = GetOrCreateOwnedWeaponFact(requiredWeapons[i]);
                if (fact == null)
                {
                    continue;
                }

                conditions.Add(new FactCompareContentCondition(
                    fact,
                    ContentFactComparisonOperator.Equal,
                    ContentFactValue.FromBool(true)));
            }

            return conditions;
        }
    }

    private readonly struct UpgradeCardTagCountRequirement
    {
        public UpgradeCardTagCountRequirement(UpgradeCardTag tag, int minPickCount)
        {
            Tag = tag;
            MinPickCount = Mathf.Max(1, minPickCount);
        }

        public UpgradeCardTag Tag { get; }
        public int MinPickCount { get; }
    }

    private readonly struct WeaponTagCountRequirement
    {
        public WeaponTagCountRequirement(WeaponTag tag, int minOwnedCount)
        {
            Tag = tag;
            MinOwnedCount = Mathf.Max(1, minOwnedCount);
        }

        public WeaponTag Tag { get; }
        public int MinOwnedCount { get; }
    }
}
#endif
