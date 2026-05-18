using System;
using UnityEngine;

[Serializable]
public sealed class SpawnLocationDefinition
{
    [SerializeField] private SpawnLocationResolverSettings resolverSettings = SpawnLocationResolverSettings.CreateDefault();
    [SerializeReference] private SpawnLocationStrategyModel strategy = new RandomInsideMapSpawnLocationStrategy();

    public SpawnLocationResolverSettings ResolverSettings => resolverSettings;
    public SpawnLocationStrategyModel Strategy => strategy;

    public SpawnLocationDefinition()
    {
    }

    public SpawnLocationDefinition(SpawnLocationResolverSettings resolverSettings, SpawnLocationStrategyModel strategy)
    {
        this.resolverSettings = resolverSettings;
        this.strategy = strategy;
        Validate();
    }

    public static SpawnLocationDefinition CreateDefault()
    {
        return new SpawnLocationDefinition();
    }

    public void Validate()
    {
        resolverSettings ??= SpawnLocationResolverSettings.CreateDefault();
        resolverSettings.Validate();
        strategy ??= new RandomInsideMapSpawnLocationStrategy();
    }
}
