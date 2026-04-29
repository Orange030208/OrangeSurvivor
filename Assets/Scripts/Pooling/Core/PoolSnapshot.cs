using System;
using UnityEngine;

[Serializable]
public readonly struct PoolSnapshot
{
    public readonly string PoolId;
    public readonly GameObject Prefab;
    public readonly int ActiveCount;
    public readonly int InactiveCount;
    public readonly int MaxActiveCount;
    public readonly int MaxInactiveCount;

    public PoolSnapshot(
        string poolId,
        GameObject prefab,
        int activeCount,
        int inactiveCount,
        int maxActiveCount,
        int maxInactiveCount)
    {
        PoolId = poolId;
        Prefab = prefab;
        ActiveCount = activeCount;
        InactiveCount = inactiveCount;
        MaxActiveCount = maxActiveCount;
        MaxInactiveCount = maxInactiveCount;
    }
}
