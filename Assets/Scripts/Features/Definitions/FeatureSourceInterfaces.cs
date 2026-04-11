using System.Collections.Generic;

public interface IDescriptionSource
{
    IReadOnlyList<string> GetDescriptions();
}

public interface IFeatureDefinition
{
    string FeatureDescription { get; }
}

public interface IRuntimeFeatureSource
{
    IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId);
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
