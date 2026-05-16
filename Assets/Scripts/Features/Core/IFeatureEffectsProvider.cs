using System.Collections.Generic;

public interface IFeatureEffectsProvider
{
    IReadOnlyList<FeatureBase> FeatureEffects { get; }
}