using System.Collections.Generic;
using UnityEngine;

//定义一个来源能提供什么 feature
public interface IFeatureSource
{
    string FeatureSourceName { get; }
    Sprite FeatureSourceIcon { get; }
    IReadOnlyList<PropEntry> GetFeaturePropEntries();
    IReadOnlyList<IFeatureDefinition> GetSpecialFeatureDefinitions();
    IReadOnlyList<FeatureViewData> GetFeatureViewData();
    IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId);
}

public interface IFeatureDefinition
{
    string FeatureDescription { get; }
}

public enum FeatureCategory
{
    Property,
    Passive,
    Trigger,
    Utility,
    Drawback
}

public enum FeaturePolarity
{
    Positive,
    Neutral,
    Negative
}

public readonly struct FeatureViewData
{
    public readonly string Title;
    public readonly string Description;
    public readonly FeatureCategory Category;
    public readonly FeaturePolarity Polarity;

    public FeatureViewData(string title, string description, FeatureCategory category, FeaturePolarity polarity)
    {
        Title = title;
        Description = description;
        Category = category;
        Polarity = polarity;
    }
}
