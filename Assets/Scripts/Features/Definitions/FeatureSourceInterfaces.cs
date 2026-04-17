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
