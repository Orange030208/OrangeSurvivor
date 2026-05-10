#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UpgradeCardContentPoolTuningUtility
{
    private const float DefaultPreviousOfferMultiplier = 0.5f;
    private const string TagFolder = GameContentAssetPaths.UpgradeCardTags;

    public static ContentPoolEntry CreateEntry(UpgradeCardSO card)
    {
        if (card == null)
        {
            return null;
        }

        ContentPoolEntry entry = new(card, GetBaseWeight(card.Rarity), card.CardId);
        entry.ConfigureRuntimeLimits(0, UpgradeCardSO.UNLIMITED_PICK_COUNT, null);
        entry.ConfigureRuntimeMetadata(0, 0, (int)card.Rarity, 1f);
        entry.ConfigureRuntimeTags(BuildUpgradeCardTags(card.Tags));
        entry.ConfigureRuntimeRules(null, BuildUpgradeCardWeightRules());
        return entry;
    }

    private static float GetBaseWeight(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => 100f,
            UpgradeCardRarity.Rare => 45f,
            UpgradeCardRarity.Epic => 12f,
            UpgradeCardRarity.Legendary => 3f,
            _ => 0f
        };
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

    private static List<ContentWeightRule> BuildUpgradeCardWeightRules()
    {
        return new List<ContentWeightRule>
        {
            new PreviousRollWeightContentRule(DefaultPreviousOfferMultiplier)
        };
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
}
#endif
