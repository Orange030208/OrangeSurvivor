using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade Card", menuName = ScriptableObjectMenuPaths.UPGRADE_CARD, order = 0)]
public class UpgradeCardSO : ScriptableObject, IDescribable
{
    public const int UNLIMITED_PICK_COUNT = 0;

    private const int MIN_PICK_COUNT = 1;
    private const int MIN_WEIGHT = 1;

    [Header("基础")]
    [SerializeField] private string cardId;
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;
    [SerializeField] private UpgradeCardRarity rarity = UpgradeCardRarity.Common;
    [SerializeField] private UpgradeCardTag[] tags = Array.Empty<UpgradeCardTag>();

    [Header("抽取")]
    [SerializeField] private int maxPickCount = MIN_PICK_COUNT;
    [SerializeField] private int baseWeight = 100;
    [SerializeField] private UpgradeCardOfferConditions offerConditions = UpgradeCardOfferConditions.Empty();

    [Header("描述")]
    [TextArea]
    [SerializeField] private string description;

    [Header("属性修饰")]
    [Tooltip("倍率统一使用 0~1 表示 0%~100%。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string CardId => cardId;
    public string Title => title;
    public Sprite Icon => icon;
    public string Description => BuildDescription();
    public UpgradeCardRarity Rarity => rarity;
    public IReadOnlyList<UpgradeCardTag> Tags => tags;
    public bool HasPickLimit => maxPickCount > UNLIMITED_PICK_COUNT;
    public int MaxPickCount => HasPickLimit ? Mathf.Max(MIN_PICK_COUNT, maxPickCount) : UNLIMITED_PICK_COUNT;
    public int BaseWeight => Mathf.Max(MIN_WEIGHT, baseWeight);
    public UpgradeCardOfferConditions OfferConditions => offerConditions;
    public IReadOnlyList<PropModifierData> PropertyModifiers => propertyModifiers;
    public IReadOnlyList<FeatureEffectBase> SpecialFeatures => specialFeatures;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            cardId = Guid.NewGuid().ToString("N")[..8];
        }

        maxPickCount = Mathf.Max(UNLIMITED_PICK_COUNT, maxPickCount);
        baseWeight = Mathf.Max(MIN_WEIGHT, baseWeight);
        tags ??= Array.Empty<UpgradeCardTag>();
        offerConditions ??= UpgradeCardOfferConditions.Empty();
        offerConditions.Validate();
        propertyModifiers ??= new List<PropModifierData>();
        specialFeatures ??= new List<FeatureEffectBase>();
    }

    public bool HasAnyEffect()
    {
        return propertyModifiers.Count > 0 || specialFeatures.Count > 0;
    }

    public void InitializeRuntime(
        string runtimeCardId,
        string runtimeTitle,
        UpgradeCardRarity runtimeRarity,
        int runtimeBaseWeight,
        IReadOnlyList<UpgradeCardTag> runtimeTags,
        string runtimeDescription,
        IReadOnlyList<PropModifierData> runtimePropertyModifiers)
    {
        cardId = string.IsNullOrWhiteSpace(runtimeCardId) ? Guid.NewGuid().ToString("N")[..8] : runtimeCardId;
        title = runtimeTitle;
        rarity = runtimeRarity;
        baseWeight = Mathf.Max(MIN_WEIGHT, runtimeBaseWeight);
        maxPickCount = MIN_PICK_COUNT;
        tags = runtimeTags != null ? ToArray(runtimeTags) : Array.Empty<UpgradeCardTag>();
        description = runtimeDescription;
        offerConditions = UpgradeCardOfferConditions.Empty();
        propertyModifiers = runtimePropertyModifiers != null
            ? new List<PropModifierData>(runtimePropertyModifiers)
            : new List<PropModifierData>();
        specialFeatures = new List<FeatureEffectBase>();
    }

    public void InitializeRuntime(
        string runtimeCardId,
        string runtimeTitle,
        UpgradeCardRarity runtimeRarity,
        int runtimeBaseWeight,
        IReadOnlyList<UpgradeCardTag> runtimeTags,
        string runtimeDescription,
        IReadOnlyList<PropModifierData> runtimePropertyModifiers,
        IReadOnlyList<FeatureEffectBase> runtimeSpecialFeatures)
    {
        InitializeRuntime(
            runtimeCardId,
            runtimeTitle,
            runtimeRarity,
            runtimeBaseWeight,
            runtimeTags,
            runtimeDescription,
            runtimePropertyModifiers);
        specialFeatures = runtimeSpecialFeatures != null
            ? new List<FeatureEffectBase>(runtimeSpecialFeatures)
            : new List<FeatureEffectBase>();
    }

    public void SetOfferConditions(UpgradeCardOfferConditions runtimeOfferConditions)
    {
        offerConditions = runtimeOfferConditions ?? UpgradeCardOfferConditions.Empty();
        offerConditions.Validate();
    }

    public void SetMaxPickCount(int runtimeMaxPickCount)
    {
        maxPickCount = Mathf.Max(UNLIMITED_PICK_COUNT, runtimeMaxPickCount);
    }

    public UpgradeCardOptionViewData CreateOptionViewData(UpgradeRunState runState)
    {
        int pickCount = runState != null ? runState.GetPickCount(CardId) : 0;
        return new UpgradeCardOptionViewData(
            CardId,
            Title,
            ResolveDisplayIcon(),
            BuildDescription(),
            Rarity,
            tags,
            pickCount,
            MaxPickCount,
            HasPickLimit);
    }

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return ItemDescriptionUtility.BuildDescriptorInfos(
            ShouldUseManualDescription() ? description : null,
            propertyModifiers,
            specialFeatures,
            BuildMetaInfos());
    }

    private string BuildDescription()
    {
        return ItemDescriptionUtility.BuildDetailedDescription(
            ShouldUseManualDescription() ? description : null,
            propertyModifiers,
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

        string tagText = ItemDescriptionUtility.JoinUpgradeCardTags(tags, tags != null ? tags.Length : 0);
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

        if (propertyModifiers.Count > 0)
        {
            return ResourcesManager.GetPropIcon(propertyModifiers[0].propType);
        }

        return null;
    }

    private static UpgradeCardTag[] ToArray(IReadOnlyList<UpgradeCardTag> source)
    {
        UpgradeCardTag[] result = new UpgradeCardTag[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            result[i] = source[i];
        }

        return result;
    }
}
