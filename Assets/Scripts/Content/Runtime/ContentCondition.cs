using System;
using System.Collections.Generic;
using UnityEngine;

public enum ContentFactComparisonOperator
{
    Equal = 0,
    NotEqual = 1,
    Greater = 2,
    GreaterOrEqual = 3,
    Less = 4,
    LessOrEqual = 5
}

[Serializable]
public abstract class ContentCondition : IContentFactDefinitionProvider
{
    public abstract bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry);

    public virtual void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
    }
}

[Serializable]
public sealed class AlwaysContentCondition : ContentCondition
{
    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        return true;
    }
}

[Serializable]
public sealed class FactExistsContentCondition : ContentCondition
{
    [SerializeField] private FactDefinitionSO factDefinition;
    [SerializeField] private bool expectedExists = true;

    public FactExistsContentCondition()
    {
    }

    public FactExistsContentCondition(FactDefinitionSO factDefinition, bool expectedExists = true)
    {
        this.factDefinition = factDefinition;
        this.expectedExists = expectedExists;
    }

    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        bool exists = context.Facts != null && context.Facts.Has(factDefinition);
        return exists == expectedExists;
    }

    public override void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        AddFact(results, factDefinition);
    }

    private static void AddFact(List<FactDefinitionSO> results, FactDefinitionSO fact)
    {
        if (fact != null && results != null && !results.Contains(fact))
        {
            results.Add(fact);
        }
    }
}

[Serializable]
public sealed class FactCompareContentCondition : ContentCondition
{
    [SerializeField] private FactDefinitionSO factDefinition;
    [SerializeField] private ContentFactComparisonOperator comparisonOperator = ContentFactComparisonOperator.GreaterOrEqual;
    [SerializeField] private ContentFactValue compareValue;

    public FactCompareContentCondition()
    {
    }

    public FactCompareContentCondition(
        FactDefinitionSO factDefinition,
        ContentFactComparisonOperator comparisonOperator,
        ContentFactValue compareValue)
    {
        this.factDefinition = factDefinition;
        this.comparisonOperator = comparisonOperator;
        this.compareValue = compareValue;
    }

    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (context.Facts == null || !context.Facts.TryGet(factDefinition, out ContentFactValue factValue))
        {
            return false;
        }

        return Compare(factValue, compareValue, comparisonOperator);
    }

    public override void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        if (factDefinition != null && results != null && !results.Contains(factDefinition))
        {
            results.Add(factDefinition);
        }
    }

    private static bool Compare(
        ContentFactValue left,
        ContentFactValue right,
        ContentFactComparisonOperator comparisonOperator)
    {
        if (left.TryGetNumber(out float leftNumber) && right.TryGetNumber(out float rightNumber))
        {
            return comparisonOperator switch
            {
                ContentFactComparisonOperator.Equal => Mathf.Approximately(leftNumber, rightNumber),
                ContentFactComparisonOperator.NotEqual => !Mathf.Approximately(leftNumber, rightNumber),
                ContentFactComparisonOperator.Greater => leftNumber > rightNumber,
                ContentFactComparisonOperator.GreaterOrEqual => leftNumber >= rightNumber,
                ContentFactComparisonOperator.Less => leftNumber < rightNumber,
                ContentFactComparisonOperator.LessOrEqual => leftNumber <= rightNumber,
                _ => false
            };
        }

        bool equals = left.EqualsValue(right);
        return comparisonOperator switch
        {
            ContentFactComparisonOperator.Equal => equals,
            ContentFactComparisonOperator.NotEqual => !equals,
            _ => false
        };
    }
}

[Serializable]
public sealed class CandidateTypeContentCondition : ContentCondition
{
    [SerializeField] private string requiredTypeName;

    public CandidateTypeContentCondition()
    {
    }

    public CandidateTypeContentCondition(string requiredTypeName)
    {
        this.requiredTypeName = requiredTypeName;
    }

    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (entry?.Content == null || string.IsNullOrWhiteSpace(requiredTypeName))
        {
            return false;
        }

        Type contentType = entry.Content.GetType();
        while (contentType != null)
        {
            if (string.Equals(contentType.Name, requiredTypeName, StringComparison.Ordinal) ||
                string.Equals(contentType.FullName, requiredTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            contentType = contentType.BaseType;
        }

        return false;
    }
}

[Serializable]
public sealed class CandidateTagContentCondition : ContentCondition
{
    [SerializeField] private ContentTagSO requiredTag;
    [SerializeField] private bool required = true;

    public CandidateTagContentCondition()
    {
    }

    public CandidateTagContentCondition(ContentTagSO requiredTag, bool required = true)
    {
        this.requiredTag = requiredTag;
        this.required = required;
    }

    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        bool hasTag = entry != null && entry.HasTag(requiredTag);
        return hasTag == required;
    }
}

[Serializable]
public sealed class CandidateAssetContentCondition : ContentCondition
{
    [SerializeField] private UnityEngine.Object requiredAsset;
    [SerializeField] private bool required = true;

    public CandidateAssetContentCondition()
    {
    }

    public CandidateAssetContentCondition(UnityEngine.Object requiredAsset, bool required = true)
    {
        this.requiredAsset = requiredAsset;
        this.required = required;
    }

    public override bool IsSatisfied(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        bool isMatch = entry != null && entry.Content == requiredAsset;
        return isMatch == required;
    }
}
