using System;
using UnityEngine;

[Serializable]
public abstract class ContentWeightRule
{
    public abstract float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry);
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

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
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

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
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

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry == null || !context.WasPreviouslyRolled(entry.EntryId))
        {
            return currentWeight;
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }
}

[Serializable]
public sealed class PlayerPropertyScaleWeightRule : ContentWeightRule
{
    [SerializeField] private PropType propType = PropType.Luck;
    [SerializeField] private float weightPerPoint = 0.01f;
    [SerializeField] private float minMultiplier;
    [SerializeField] private float maxMultiplier = 10f;

    public PlayerPropertyScaleWeightRule()
    {
    }

    public PlayerPropertyScaleWeightRule(
        PropType propType,
        float weightPerPoint,
        float minMultiplier = 0f,
        float maxMultiplier = 10f)
    {
        this.propType = propType;
        this.weightPerPoint = weightPerPoint;
        this.minMultiplier = minMultiplier;
        this.maxMultiplier = maxMultiplier;
    }

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
    {
        float propertyValue = context != null ? context.GetPropertyValue(propType) : 0f;
        float multiplier = 1f + propertyValue * weightPerPoint;
        float resolvedMinMultiplier = Mathf.Max(0f, minMultiplier);
        if (maxMultiplier > resolvedMinMultiplier)
        {
            multiplier = Mathf.Clamp(multiplier, resolvedMinMultiplier, maxMultiplier);
        }
        else
        {
            multiplier = Mathf.Max(resolvedMinMultiplier, multiplier);
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }
}

[Serializable]
public sealed class CandidateUpgradeCardTagWeightRule : ContentWeightRule
{
    [SerializeField] private UpgradeCardTag targetTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private float multiplier = 1f;
    [SerializeField] private float addedWeight;

    public CandidateUpgradeCardTagWeightRule()
    {
    }

    public CandidateUpgradeCardTagWeightRule(
        UpgradeCardTag targetTags,
        float multiplier,
        float addedWeight = 0f,
        ContentTagMatchMode matchMode = ContentTagMatchMode.Any)
    {
        this.targetTags = targetTags;
        this.multiplier = multiplier;
        this.addedWeight = addedWeight;
        this.matchMode = matchMode;
    }

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry?.Content is not UpgradeCardSO card ||
            !ContentTagMatchUtility.Matches(card.Tags, targetTags, matchMode))
        {
            return currentWeight;
        }

        return currentWeight * Mathf.Max(0f, multiplier) + addedWeight;
    }
}

[Serializable]
public sealed class UpgradeCardTagPickCountWeightRule : ContentWeightRule
{
    [SerializeField] private UpgradeCardTag targetTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private float multiplierPerPick = 0.15f;
    [SerializeField] private float maxMultiplier = 10f;

    public UpgradeCardTagPickCountWeightRule()
    {
    }

    public UpgradeCardTagPickCountWeightRule(
        UpgradeCardTag targetTags,
        float multiplierPerPick,
        float maxMultiplier = 10f,
        ContentTagMatchMode matchMode = ContentTagMatchMode.Any)
    {
        this.targetTags = targetTags;
        this.multiplierPerPick = multiplierPerPick;
        this.maxMultiplier = maxMultiplier;
        this.matchMode = matchMode;
    }

    public override float ModifyWeight(float currentWeight, ContentRollContext context, ContentPoolEntry entry)
    {
        int pickCount = context?.History != null
            ? context.History.GetUpgradeCardTagPickCount(context.HistoryScope, targetTags, matchMode)
            : 0;
        if (pickCount <= 0)
        {
            return currentWeight;
        }

        float multiplier = 1f + pickCount * multiplierPerPick;
        if (maxMultiplier > 0f)
        {
            multiplier = Mathf.Min(multiplier, maxMultiplier);
        }

        return currentWeight * Mathf.Max(0f, multiplier);
    }
}
