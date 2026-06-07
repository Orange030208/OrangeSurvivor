using System;
using UnityEngine;

public enum ContentComparisonOperator
{
    Equal = 0,
    NotEqual = 1,
    Greater = 2,
    GreaterOrEqual = 3,
    Less = 4,
    LessOrEqual = 5
}

public enum ContentTagMatchMode
{
    Any = 0,
    All = 1,
    None = 2,
    Exact = 3
}

internal static class ContentConditionCompareUtility
{
    public static bool Compare(float left, float right, ContentComparisonOperator comparisonOperator)
    {
        return comparisonOperator switch
        {
            ContentComparisonOperator.Equal => Mathf.Approximately(left, right),
            ContentComparisonOperator.NotEqual => !Mathf.Approximately(left, right),
            ContentComparisonOperator.Greater => left > right,
            ContentComparisonOperator.GreaterOrEqual => left >= right,
            ContentComparisonOperator.Less => left < right,
            ContentComparisonOperator.LessOrEqual => left <= right,
            _ => false
        };
    }

    public static bool Compare(int left, int right, ContentComparisonOperator comparisonOperator)
    {
        return comparisonOperator switch
        {
            ContentComparisonOperator.Equal => left == right,
            ContentComparisonOperator.NotEqual => left != right,
            ContentComparisonOperator.Greater => left > right,
            ContentComparisonOperator.GreaterOrEqual => left >= right,
            ContentComparisonOperator.Less => left < right,
            ContentComparisonOperator.LessOrEqual => left <= right,
            _ => false
        };
    }
}

internal static class ContentTagMatchUtility
{
    public static bool Matches(CardTag candidateTags, CardTag requiredTags, ContentTagMatchMode matchMode)
    {
        return matchMode switch
        {
            ContentTagMatchMode.Any => requiredTags != CardTag.None && (candidateTags & requiredTags) != 0,
            ContentTagMatchMode.All => requiredTags != CardTag.None && (candidateTags & requiredTags) == requiredTags,
            ContentTagMatchMode.None => requiredTags == CardTag.None
                ? candidateTags == CardTag.None
                : (candidateTags & requiredTags) == 0,
            ContentTagMatchMode.Exact => candidateTags == requiredTags,
            _ => false
        };
    }
}

[Serializable]
public abstract class ContentCondition
{
    public abstract bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry);
}

[Serializable]
public sealed class AlwaysContentCondition : ContentCondition
{
    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        return true;
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

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
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

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool isMatch = entry != null && entry.Content == requiredAsset;
        return isMatch == required;
    }
}

[Serializable]
public sealed class CurrentWaveCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(1)] private int compareValue = 1;

    public CurrentWaveCondition()
    {
    }

    public CurrentWaveCondition(ContentComparisonOperator comparisonOperator, int compareValue)
    {
        this.comparisonOperator = comparisonOperator;
        this.compareValue = Mathf.Max(1, compareValue);
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int currentWave = context != null ? context.CurrentWaveNumber : 1;
        return ContentConditionCompareUtility.Compare(currentWave, Mathf.Max(1, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class WaveTrackCondition : ContentCondition
{
    [SerializeField] private string requiredTrackId;
    [SerializeField] private bool required = true;

    public WaveTrackCondition()
    {
    }

    public WaveTrackCondition(string requiredTrackId, bool required = true)
    {
        this.requiredTrackId = requiredTrackId;
        this.required = required;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool matches = context != null &&
                       string.Equals(
                           context.WaveTrackId ?? string.Empty,
                           requiredTrackId ?? string.Empty,
                           StringComparison.Ordinal);
        return matches == required;
    }
}

[Serializable]
public sealed class WaveIdCondition : ContentCondition
{
    [SerializeField] private string requiredWaveId;
    [SerializeField] private bool required = true;

    public WaveIdCondition()
    {
    }

    public WaveIdCondition(string requiredWaveId, bool required = true)
    {
        this.requiredWaveId = requiredWaveId;
        this.required = required;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool matches = context != null &&
                       string.Equals(
                           context.WaveId ?? string.Empty,
                           requiredWaveId ?? string.Empty,
                           StringComparison.Ordinal);
        return matches == required;
    }
}

[Serializable]
public sealed class WaveProgressCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Range(0f, 100f)] private float compareValue;

    public WaveProgressCondition()
    {
    }

    public WaveProgressCondition(ContentComparisonOperator comparisonOperator, float compareValue)
    {
        this.comparisonOperator = comparisonOperator;
        this.compareValue = Mathf.Clamp(compareValue, 0f, 100f);
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        float currentProgress = context != null ? Mathf.Clamp(context.WaveProgressPercent, 0f, 100f) : 0f;
        return ContentConditionCompareUtility.Compare(
            currentProgress,
            Mathf.Clamp(compareValue, 0f, 100f),
            comparisonOperator);
    }
}

[Serializable]
public sealed class RunProgressionValueCondition : ContentCondition
{
    [SerializeField] private RunProgressionValue value = RunProgressionValue.DifficultyCoefficient;
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField] private float compareValue;

    public RunProgressionValueCondition()
    {
    }

    public RunProgressionValueCondition(
        RunProgressionValue value,
        ContentComparisonOperator comparisonOperator,
        float compareValue)
    {
        this.value = value;
        this.comparisonOperator = comparisonOperator;
        this.compareValue = compareValue;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        return ContentConditionCompareUtility.Compare(
            ResolveValue(context != null ? context.ProgressionSnapshot : RunProgressionSnapshot.Default),
            compareValue,
            comparisonOperator);
    }

    private float ResolveValue(RunProgressionSnapshot snapshot)
    {
        return value switch
        {
            RunProgressionValue.WaveNumber => snapshot.WaveNumber,
            RunProgressionValue.TotalWaves => snapshot.TotalWaves,
            RunProgressionValue.RunMinutes => snapshot.RunMinutes,
            RunProgressionValue.EndlessLoop => snapshot.EndlessLoop,
            RunProgressionValue.DifficultyCoefficient => snapshot.DifficultyCoefficient,
            RunProgressionValue.EconomyCoefficient => snapshot.EconomyCoefficient,
            RunProgressionValue.ShopPriceMultiplier => snapshot.ShopPriceMultiplier,
            RunProgressionValue.DangerTier => snapshot.DangerTier,
            _ => 0f
        };
    }
}

public enum RunProgressionValue
{
    WaveNumber = 0,
    TotalWaves = 1,
    RunMinutes = 2,
    EndlessLoop = 3,
    DifficultyCoefficient = 4,
    EconomyCoefficient = 5,
    ShopPriceMultiplier = 6,
    DangerTier = 7
}

[Serializable]
public sealed class PlayerPropertyCondition : ContentCondition
{
    [SerializeField] private PropType propType;
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField] private float compareValue;

    public PlayerPropertyCondition()
    {
    }

    public PlayerPropertyCondition(
        PropType propType,
        ContentComparisonOperator comparisonOperator,
        float compareValue)
    {
        this.propType = propType;
        this.comparisonOperator = comparisonOperator;
        this.compareValue = compareValue;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        float value = context != null ? context.GetPropertyValue(propType) : 0f;
        return ContentConditionCompareUtility.Compare(value, compareValue, comparisonOperator);
    }
}

[Serializable]
public sealed class ShopRefreshCountCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(0)] private int compareValue;

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = context != null ? context.ShopRefreshCount : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class ShopRerollCountCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(0)] private int compareValue;

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = context != null ? context.ShopRerollCount : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class OwnedWeaponCountCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(0)] private int compareValue;

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = context != null ? context.GetOwnedWeaponCount() : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class OwnedWeaponTagCountCondition : ContentCondition
{
    [SerializeField] private WeaponTag weaponTag;
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(0)] private int compareValue;

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = context != null ? context.GetOwnedWeaponTagCount(weaponTag) : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class OwnedWeaponCondition : ContentCondition
{
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private bool required = true;

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool hasWeapon = context != null && context.HasOwnedWeapon(weaponData);
        return hasWeapon == required;
    }
}

[Serializable]
public sealed class AccessoryOwnedLimitCondition : ContentCondition
{
    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry?.Content is not AccessoryDataSO accessory || !accessory.HasOwnedLimit)
        {
            return true;
        }

        AccessoryManager accessoryManager = ResolveAccessoryManager(context);
        int ownedCount = accessoryManager != null ? accessoryManager.GetEquippedCount(accessory) : 0;
        int selectedCount = CountSelectedAccessories(context, accessory);
        return accessory.CanOwnMore(ownedCount + selectedCount);
    }

    private static AccessoryManager ResolveAccessoryManager(ContentRollContext context)
    {
        // 饰品持有限制属于饰品领域规则，推导逻辑留在条件内部，避免污染 ContentRollContext。
        if (context == null)
        {
            return null;
        }

        if (context.Player != null && context.Player.TryGetComponent(out AccessoryManager playerAccessoryManager))
        {
            return playerAccessoryManager;
        }

        return context.Source != null && context.Source.TryGetComponent(out AccessoryManager sourceAccessoryManager)
            ? sourceAccessoryManager
            : null;
    }

    private static int CountSelectedAccessories(ContentRollContext context, AccessoryDataSO candidate)
    {
        if (context?.SelectedEntries == null || candidate == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < context.SelectedEntries.Count; i++)
        {
            if (context.SelectedEntries[i]?.Content is AccessoryDataSO selectedAccessory &&
                IsSameAccessory(candidate, selectedAccessory))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsSameAccessory(AccessoryDataSO left, AccessoryDataSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string leftId = left.AccessoryId;
        string rightId = right.AccessoryId;
        if (!string.IsNullOrWhiteSpace(leftId) && !string.IsNullOrWhiteSpace(rightId))
        {
            return string.Equals(leftId, rightId, StringComparison.Ordinal);
        }

        return left == right;
    }
}

[Serializable]
public sealed class UpgradeCardTagCondition : ContentCondition
{
    [SerializeField] private CardTag requiredTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private bool required = true;

    public UpgradeCardTagCondition()
    {
    }

    public UpgradeCardTagCondition(CardTag requiredTags, bool required = true)
    {
        this.requiredTags = requiredTags;
        this.required = required;
    }

    public UpgradeCardTagCondition(
        CardTag requiredTags,
        ContentTagMatchMode matchMode,
        bool required = true)
    {
        this.requiredTags = requiredTags;
        this.matchMode = matchMode;
        this.required = required;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool matches = entry?.Content is RewardCardSO card &&
                       ContentTagMatchUtility.Matches(card.Tags, requiredTags, matchMode);
        return matches == required;
    }
}

[Serializable]
public sealed class UpgradeCardTagPickCountCondition : ContentCondition
{
    [SerializeField] private CardTag requiredTags;
    [SerializeField] private ContentTagMatchMode matchMode = ContentTagMatchMode.Any;
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.GreaterOrEqual;
    [SerializeField, Min(0)] private int compareValue;

    public UpgradeCardTagPickCountCondition()
    {
    }

    public UpgradeCardTagPickCountCondition(
        CardTag requiredTags,
        ContentComparisonOperator comparisonOperator,
        int compareValue)
        : this(requiredTags, ContentTagMatchMode.Any, comparisonOperator, compareValue)
    {
    }

    public UpgradeCardTagPickCountCondition(
        CardTag requiredTags,
        ContentTagMatchMode matchMode,
        ContentComparisonOperator comparisonOperator,
        int compareValue)
    {
        this.requiredTags = requiredTags;
        this.matchMode = matchMode;
        this.comparisonOperator = comparisonOperator;
        this.compareValue = Mathf.Max(0, compareValue);
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = context?.History != null
            ? context.History.GetUpgradeCardTagPickCount(context.HistoryScope, requiredTags, matchMode)
            : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class ContentPickCountCondition : ContentCondition
{
    [SerializeField] private ContentComparisonOperator comparisonOperator = ContentComparisonOperator.Less;
    [SerializeField, Min(0)] private int compareValue = 1;

    public ContentPickCountCondition()
    {
    }

    public ContentPickCountCondition(ContentComparisonOperator comparisonOperator, int compareValue)
    {
        this.comparisonOperator = comparisonOperator;
        this.compareValue = Mathf.Max(0, compareValue);
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        int count = entry != null ? context.GetPickCount(entry.EntryId) : 0;
        return ContentConditionCompareUtility.Compare(count, Mathf.Max(0, compareValue), comparisonOperator);
    }
}

[Serializable]
public sealed class UniqueUpgradeCardTagCondition : ContentCondition
{
    [SerializeField] private CardTag restrictedTags = CardTag.None;

    public UniqueUpgradeCardTagCondition()
    {
    }

    public UniqueUpgradeCardTagCondition(CardTag restrictedTags)
    {
        this.restrictedTags = restrictedTags;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry?.Content is not RewardCardSO candidateCard || context?.SelectedEntries == null)
        {
            return true;
        }

        CardTag candidateTags = ResolveComparedTags(candidateCard.Tags);
        if (candidateTags == CardTag.None)
        {
            return true;
        }

        for (int i = 0; i < context.SelectedEntries.Count; i++)
        {
            if (context.SelectedEntries[i]?.Content is not RewardCardSO selectedCard)
            {
                continue;
            }

            if ((candidateTags & ResolveComparedTags(selectedCard.Tags)) != 0)
            {
                return false;
            }
        }

        return true;
    }

    private CardTag ResolveComparedTags(CardTag sourceTags)
    {
        return restrictedTags == CardTag.None ? sourceTags : sourceTags & restrictedTags;
    }
}

[Serializable]
public sealed class UniqueContentTypeCondition : ContentCondition
{
    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry?.Content == null || context?.SelectedEntries == null)
        {
            return true;
        }

        Type candidateType = entry.Content.GetType();
        for (int i = 0; i < context.SelectedEntries.Count; i++)
        {
            if (context.SelectedEntries[i]?.Content != null &&
                context.SelectedEntries[i].Content.GetType() == candidateType)
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class UniqueAssetCondition : ContentCondition
{
    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry?.Content == null || context?.SelectedEntries == null)
        {
            return true;
        }

        for (int i = 0; i < context.SelectedEntries.Count; i++)
        {
            if (context.SelectedEntries[i]?.Content == entry.Content)
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class ContentOfferHistoryCondition : ContentCondition
{
    [SerializeField] private bool expectedPreviouslyOffered = true;

    public ContentOfferHistoryCondition()
    {
    }

    public ContentOfferHistoryCondition(bool expectedPreviouslyOffered)
    {
        this.expectedPreviouslyOffered = expectedPreviouslyOffered;
    }

    public override bool IsSatisfied(ContentRollContext context, ContentPoolEntry entry)
    {
        bool wasOffered = entry != null &&
                          context.History != null &&
                          context.History.WasPreviouslyOffered(context.HistoryScope, entry.EntryId);
        return wasOffered == expectedPreviouslyOffered;
    }
}
