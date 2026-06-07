using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ContentPoolModifier : FeatureBase, IContentPoolModifier
{
    [SerializeField] private int priority;
    [SerializeField] private string targetScopeId;
    [SerializeField] private bool affectAllScopes;

    public virtual int Priority => priority;

    public override void OnInstall()
    {
        ContentPoolModifierRegistry.Register(this);
    }

    public override void OnUninstall()
    {
        ContentPoolModifierRegistry.Unregister(this);
    }

    public virtual bool AffectsContext(ContentRollContext context)
    {
        if (affectAllScopes)
        {
            return true;
        }

        return string.Equals(
            ContentPoolScopeIds.Normalize(context?.ScopeId),
            ContentPoolScopeIds.Normalize(targetScopeId),
            StringComparison.Ordinal);
    }

    public abstract void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates);
}

[Serializable]
public sealed class UpgradeCardTagContentPoolWeightModifier : ContentPoolModifier
{
    [SerializeField] private CardTag targetTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float addedWeight;

    public override string Description => "调整匹配升级卡标签内容的出现权重。";

    public override void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Content is not RewardCardSO card ||
                !ContentTagMatchUtility.Matches(card.Tags, targetTags, matchMode))
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + addedWeight);
        }
    }
}

[Serializable]
public sealed class AssetContentPoolWeightModifier : ContentPoolModifier
{
    [SerializeField] private UnityEngine.Object targetAsset;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float addedWeight;

    public override string Description => "调整指定内容的出现权重。";

    public override void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null || targetAsset == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Content != targetAsset)
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + addedWeight);
        }
    }
}

[Serializable]
public sealed class UpgradeCardTagContentPoolMetadataModifier : ContentPoolModifier
{
    [SerializeField] private CardTag targetTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private bool overrideLevelRange;
    [SerializeField, Min(0)] private int minLevel;
    [SerializeField, Min(0)] private int maxLevel;
    [SerializeField] private bool overrideQualityValue;
    [SerializeField] private int qualityValue;
    [SerializeField] private bool multiplyPrice;
    [SerializeField] private float priceMultiplier = 1f;

    public override string Description => "调整匹配升级卡标签内容的等级、品质或价格元数据。";

    public override void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Content is not RewardCardSO card ||
                !ContentTagMatchUtility.Matches(card.Tags, targetTags, matchMode))
            {
                continue;
            }

            ApplyMetadata(candidate);
        }
    }

    private void ApplyMetadata(ContentPoolCandidate candidate)
    {
        if (overrideLevelRange)
        {
            candidate.ConfigureLevelRange(minLevel, maxLevel);
        }

        if (overrideQualityValue)
        {
            candidate.ConfigureQualityValue(qualityValue);
        }

        if (multiplyPrice)
        {
            candidate.ConfigurePriceMultiplier(candidate.GetPriceMultiplier() * Mathf.Max(0f, priceMultiplier));
        }
    }
}

[Serializable]
public sealed class AssetContentPoolMetadataModifier : ContentPoolModifier
{
    [SerializeField] private UnityEngine.Object targetAsset;
    [SerializeField] private bool overrideLevelRange;
    [SerializeField, Min(0)] private int minLevel;
    [SerializeField, Min(0)] private int maxLevel;
    [SerializeField] private bool overrideQualityValue;
    [SerializeField] private int qualityValue;
    [SerializeField] private bool multiplyPrice;
    [SerializeField] private float priceMultiplier = 1f;

    public override string Description => "调整指定内容的等级、品质或价格元数据。";

    public override void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null || targetAsset == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Content != targetAsset)
            {
                continue;
            }

            ApplyMetadata(candidate);
        }
    }

    private void ApplyMetadata(ContentPoolCandidate candidate)
    {
        if (overrideLevelRange)
        {
            candidate.ConfigureLevelRange(minLevel, maxLevel);
        }

        if (overrideQualityValue)
        {
            candidate.ConfigureQualityValue(qualityValue);
        }

        if (multiplyPrice)
        {
            candidate.ConfigurePriceMultiplier(candidate.GetPriceMultiplier() * Mathf.Max(0f, priceMultiplier));
        }
    }
}
