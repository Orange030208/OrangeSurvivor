using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ContentWeightRule : IContentFactDefinitionProvider
{
    public abstract float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry);

    public virtual void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
    }
}

[Serializable]
public sealed class AddWeightContentRule : ContentWeightRule
{
    [SerializeField] private float addedWeight;

    public AddWeightContentRule()
    {
    }

    public AddWeightContentRule(float addedWeight)
    {
        this.addedWeight = addedWeight;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        return currentWeight + addedWeight;
    }
}

[Serializable]
public sealed class MultiplyWeightContentRule : ContentWeightRule
{
    [SerializeField] private float multiplier = 1f;

    public MultiplyWeightContentRule()
    {
    }

    public MultiplyWeightContentRule(float multiplier)
    {
        this.multiplier = multiplier;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        return currentWeight * Mathf.Max(0f, multiplier);
    }
}

[Serializable]
public sealed class PreviousRollWeightContentRule : ContentWeightRule
{
    [SerializeField] private float multiplier = 1f;

    public PreviousRollWeightContentRule()
    {
    }

    public PreviousRollWeightContentRule(float multiplier)
    {
        this.multiplier = multiplier;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (entry == null || context.RuntimeState == null || !context.RuntimeState.WasPreviouslyRolled(entry.EntryId))
        {
            return currentWeight;
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }
}

[Serializable]
public sealed class FactScaleWeightContentRule : ContentWeightRule
{
    [SerializeField] private FactDefinitionSO factDefinition;
    [SerializeField] private float weightPerFactPoint = 0.01f;
    [SerializeField] private float minMultiplier;
    [SerializeField] private float maxMultiplier = 10f;

    public FactScaleWeightContentRule()
    {
    }

    public FactScaleWeightContentRule(
        FactDefinitionSO factDefinition,
        float weightPerFactPoint,
        float minMultiplier = 0f,
        float maxMultiplier = 10f)
    {
        this.factDefinition = factDefinition;
        this.weightPerFactPoint = weightPerFactPoint;
        this.minMultiplier = minMultiplier;
        this.maxMultiplier = maxMultiplier;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (context.Facts == null || !context.Facts.TryGet(factDefinition, out ContentFactValue factValue) ||
            !factValue.TryGetNumber(out float factNumber))
        {
            return currentWeight;
        }

        float multiplier = 1f + factNumber * weightPerFactPoint;
        if (maxMultiplier > minMultiplier)
        {
            multiplier = Mathf.Clamp(multiplier, minMultiplier, maxMultiplier);
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }

    public override void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        if (factDefinition != null && results != null && !results.Contains(factDefinition))
        {
            results.Add(factDefinition);
        }
    }
}

[Serializable]
public sealed class TagWeightContentRule : ContentWeightRule
{
    [SerializeField] private ContentTagSO targetTag;
    [SerializeField] private float multiplier = 1f;
    [SerializeField] private float addedWeight;

    public TagWeightContentRule()
    {
    }

    public TagWeightContentRule(ContentTagSO targetTag, float multiplier, float addedWeight = 0f)
    {
        this.targetTag = targetTag;
        this.multiplier = multiplier;
        this.addedWeight = addedWeight;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (entry == null || !entry.HasTag(targetTag))
        {
            return currentWeight;
        }

        return currentWeight * Mathf.Max(0f, multiplier) + addedWeight;
    }
}

[Serializable]
public sealed class FactDrivenCandidateTagWeightContentRule : ContentWeightRule
{
    [SerializeField] private FactDefinitionSO factDefinition;
    [SerializeField] private ContentTagSO targetTag;
    [SerializeField] private float multiplierPerFactPoint = 0.15f;
    [SerializeField] private float maxMultiplier = 10f;

    public FactDrivenCandidateTagWeightContentRule()
    {
    }

    public FactDrivenCandidateTagWeightContentRule(
        FactDefinitionSO factDefinition,
        ContentTagSO targetTag,
        float multiplierPerFactPoint,
        float maxMultiplier = 10f)
    {
        this.factDefinition = factDefinition;
        this.targetTag = targetTag;
        this.multiplierPerFactPoint = multiplierPerFactPoint;
        this.maxMultiplier = maxMultiplier;
    }

    public override float ModifyWeight(float currentWeight, ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (entry == null || !entry.HasTag(targetTag) ||
            context.Facts == null || !context.Facts.TryGet(factDefinition, out ContentFactValue factValue) ||
            !factValue.TryGetNumber(out float factNumber))
        {
            return currentWeight;
        }

        float multiplier = 1f + Mathf.Max(0f, factNumber) * multiplierPerFactPoint;
        if (maxMultiplier > 0f)
        {
            multiplier = Mathf.Min(multiplier, maxMultiplier);
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }

    public override void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        if (factDefinition != null && results != null && !results.Contains(factDefinition))
        {
            results.Add(factDefinition);
        }
    }
}
