using System.Collections.Generic;

public interface IRuntimeFeatureSource
{
    IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId);
}
