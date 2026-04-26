using System.Collections.Generic;

public interface IFeatureEffectsProvider
{
    IReadOnlyList<FeatureEffectBase> FeatureEffects { get; }
}