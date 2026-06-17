using System;
using UnityEngine;

[Serializable]
public sealed class SpawnLocationDefinition
{
    [SerializeField] private SpawnLocationResolverSettings resolverSettings = SpawnLocationResolverSettings.CreateDefault();

    public SpawnLocationResolverSettings ResolverSettings => resolverSettings;

    public SpawnLocationDefinition()
    {
    }

    public SpawnLocationDefinition(SpawnLocationResolverSettings resolverSettings)
    {
        this.resolverSettings = resolverSettings;
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
    }
}
