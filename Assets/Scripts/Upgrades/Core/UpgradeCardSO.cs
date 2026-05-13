using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade Card", menuName = ScriptableObjectMenuPaths.UPGRADE_CARD, order = 0)]
public class UpgradeCardSO : ScriptableObject, IDescribable
{
    public const int UNLIMITED_PICK_COUNT = 0;

    [Header("基础")]
    [SerializeField] private string cardId;
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;
    [SerializeField] private UpgradeCardRarity rarity = UpgradeCardRarity.Common;
    [SerializeField] private UpgradeCardTag tags = UpgradeCardTag.None;

    [Header("描述")]
    [TextArea]
    [SerializeField] private string description;

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string CardId => cardId;
    public string Title => title;
    public Sprite Icon => icon;
    public string Description => BuildDescription();
    public UpgradeCardRarity Rarity => rarity;
    public UpgradeCardTag Tags => tags;
    public UpgradeCardTag[] TagList => ToTagArray(tags);
    public IReadOnlyList<FeatureEffectBase> SpecialFeatures => specialFeatures;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            cardId = Guid.NewGuid().ToString("N")[..8];
        }

        specialFeatures ??= new List<FeatureEffectBase>();
    }

    public bool HasAnyEffect()
    {
        return specialFeatures.Count > 0;
    }

    public void InitializeRuntime(
        string runtimeCardId,
        string runtimeTitle,
        UpgradeCardRarity runtimeRarity,
        IReadOnlyList<UpgradeCardTag> runtimeTags,
        string runtimeDescription,
        IReadOnlyList<FeatureEffectBase> runtimeSpecialFeatures = null)
    {
        cardId = string.IsNullOrWhiteSpace(runtimeCardId) ? Guid.NewGuid().ToString("N")[..8] : runtimeCardId;
        title = runtimeTitle;
        rarity = runtimeRarity;
        tags = ToTagMask(runtimeTags);
        description = runtimeDescription;
        specialFeatures = runtimeSpecialFeatures != null
            ? new List<FeatureEffectBase>(runtimeSpecialFeatures)
            : new List<FeatureEffectBase>();
    }

    public bool HasTag(UpgradeCardTag tag)
    {
        return tag != UpgradeCardTag.None && (tags & tag) != 0;
    }

    public UpgradeCardOptionViewData CreateOptionViewData(int pickCount, int maxPickCount)
    {
        bool hasPickLimit = maxPickCount > UNLIMITED_PICK_COUNT;
        return new UpgradeCardOptionViewData(
            CardId,
            Title,
            ResolveDisplayIcon(),
            BuildDescription(),
            Rarity,
            TagList,
            pickCount,
            maxPickCount,
            hasPickLimit);
    }

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return ItemDescriptionUtility.BuildDescriptorInfos(
            ShouldUseManualDescription() ? description : null,
            null,
            specialFeatures,
            BuildMetaInfos());
    }

    private string BuildDescription()
    {
        return ItemDescriptionUtility.BuildDetailedDescription(
            ShouldUseManualDescription() ? description : null,
            null,
            specialFeatures,
            null,
            "获得一项升级。");
    }

    private bool ShouldUseManualDescription()
    {
        return !HasAnyEffect() && !string.IsNullOrWhiteSpace(description);
    }

    private IEnumerable<DescriptorInfo> BuildMetaInfos()
    {
        yield return new DescriptorInfo("品质", ItemDescriptionUtility.FormatRarity(rarity));

        string tagText = ItemDescriptionUtility.JoinUpgradeCardTags(tags, int.MaxValue);
        if (!string.IsNullOrWhiteSpace(tagText))
        {
            yield return new DescriptorInfo("标签", tagText);
        }
    }

    private Sprite ResolveDisplayIcon()
    {
        if (icon != null)
        {
            return icon;
        }

        PropertyModifierFeature firstPropertyFeature = ResolveFirstPropertyFeature();
        if (firstPropertyFeature != null)
        {
            return GameContentRuntime.GetPropIcon(firstPropertyFeature.Modifier.propType);
        }

        return null;
    }

    private PropertyModifierFeature ResolveFirstPropertyFeature()
    {
        if (specialFeatures == null)
        {
            return null;
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            if (specialFeatures[i] is PropertyModifierFeature propertyFeature)
            {
                return propertyFeature;
            }
        }

        return null;
    }

    private static UpgradeCardTag ToTagMask(IReadOnlyList<UpgradeCardTag> source)
    {
        UpgradeCardTag mask = UpgradeCardTag.None;
        if (source == null)
        {
            return mask;
        }

        for (int i = 0; i < source.Count; i++)
        {
            mask |= source[i];
        }

        return mask;
    }

    private static UpgradeCardTag[] ToTagArray(UpgradeCardTag mask)
    {
        if (mask == UpgradeCardTag.None)
        {
            return Array.Empty<UpgradeCardTag>();
        }

        List<UpgradeCardTag> result = new();
        foreach (UpgradeCardTag tag in Enum.GetValues(typeof(UpgradeCardTag)))
        {
            if (tag == UpgradeCardTag.None || (mask & tag) == 0)
            {
                continue;
            }

            result.Add(tag);
        }

        return result.ToArray();
    }

}
