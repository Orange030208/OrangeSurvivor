using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pool Catalog", menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Pooling/Pool Catalog")]
public sealed class PoolCatalogSO : ScriptableObject
{
    [SerializeField] private PoolDefinition[] definitions = Array.Empty<PoolDefinition>();

    public IReadOnlyList<PoolDefinition> Definitions => definitions ?? Array.Empty<PoolDefinition>();

    public bool TryGetDefinition(GameObject prefab, out PoolDefinition definition)
    {
        definition = null;
        if (prefab == null)
        {
            return false;
        }

        IReadOnlyList<PoolDefinition> items = Definitions;
        for (int i = 0; i < items.Count; i++)
        {
            PoolDefinition item = items[i];
            if (item == null || item.Prefab != prefab)
            {
                continue;
            }

            definition = item;
            return true;
        }

        return false;
    }

    public bool TryGetDefinition(string poolId, out PoolDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(poolId))
        {
            return false;
        }

        IReadOnlyList<PoolDefinition> items = Definitions;
        for (int i = 0; i < items.Count; i++)
        {
            PoolDefinition item = items[i];
            if (item == null || !string.Equals(item.PoolId, poolId, StringComparison.Ordinal))
            {
                continue;
            }

            definition = item;
            return true;
        }

        return false;
    }
}
