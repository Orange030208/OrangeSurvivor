using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ContentPoolModifierEffect : FeatureEffectBase, IContentPoolModifier, IContentFactDefinitionProvider
{
    [SerializeField] private int priority;
    [SerializeField] private ContentPoolPurpose targetPurpose = ContentPoolPurpose.Generic;
    [SerializeField] private bool affectAllPurposes;

    public virtual int Priority => priority;

    public override void OnInstall()
    {
        ContentPoolModifierRegistry.Register(this);
    }

    public override void OnUninstall()
    {
        ContentPoolModifierRegistry.Unregister(this);
    }

    public virtual bool AffectsPurpose(ContentPoolPurpose purpose)
    {
        return affectAllPurposes || targetPurpose == ContentPoolPurpose.Generic || purpose == targetPurpose;
    }

    public virtual void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
    }

    public abstract void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates);
}

[Serializable]
public sealed class TagContentPoolWeightModifierEffect : ContentPoolModifierEffect
{
    [SerializeField] private ContentTagSO targetTag;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float addedWeight;

    public override string Description => "调整匹配标签内容的出现权重。";

    public override void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Entry == null || !candidate.Entry.HasTag(targetTag))
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + addedWeight);
        }
    }
}

[Serializable]
public sealed class AssetContentPoolWeightModifierEffect : ContentPoolModifierEffect
{
    [SerializeField] private UnityEngine.Object targetAsset;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float addedWeight;

    public override string Description => "调整指定内容的出现权重。";

    public override void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
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
public sealed class TagContentPoolMetadataModifierEffect : ContentPoolModifierEffect
{
    [SerializeField] private ContentTagSO targetTag;
    [SerializeField] private bool overrideLevelRange;
    [SerializeField, Min(0)] private int minLevel;
    [SerializeField, Min(0)] private int maxLevel;
    [SerializeField] private bool overrideQualityValue;
    [SerializeField] private int qualityValue;
    [SerializeField] private bool multiplyPrice;
    [SerializeField] private float priceMultiplier = 1f;

    public override string Description => "调整匹配标签内容的等级、品质或价格元数据。";

    public override void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate?.Entry == null || !candidate.Entry.HasTag(targetTag))
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
            candidate.ConfigurePriceMultiplier(candidate.PriceMultiplier * Mathf.Max(0f, priceMultiplier));
        }
    }
}

[Serializable]
public sealed class AssetContentPoolMetadataModifierEffect : ContentPoolModifierEffect
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

    public override void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
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
            candidate.ConfigurePriceMultiplier(candidate.PriceMultiplier * Mathf.Max(0f, priceMultiplier));
        }
    }
}
