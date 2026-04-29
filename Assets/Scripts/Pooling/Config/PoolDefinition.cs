using System;
using UnityEngine;

[Serializable]
public sealed class PoolDefinition
{
    [SerializeField] private string poolId;
    [SerializeField] private GameObject prefab;
    [Min(0)] [SerializeField] private int preloadCount;
    [Min(1)] [SerializeField] private int maxActiveCount = 64;
    [Min(0)] [SerializeField] private int maxInactiveCount = 64;
    [SerializeField] private bool recycleLeastRecentlyUsedActive = true;
    [SerializeField] private bool destroyOverflowInactive = true;

    public string PoolId => ResolvePoolId(poolId, prefab);
    public GameObject Prefab => prefab;
    public int PreloadCount => MaxInactiveCount > 0 ? Mathf.Clamp(preloadCount, 0, MaxInactiveCount) : 0;
    public int MaxActiveCount => Mathf.Max(1, maxActiveCount);
    public int MaxInactiveCount => Mathf.Max(0, maxInactiveCount);
    public bool RecycleLeastRecentlyUsedActive => recycleLeastRecentlyUsedActive;
    public bool DestroyOverflowInactive => destroyOverflowInactive;
    public bool IsValid => prefab != null && !string.IsNullOrWhiteSpace(PoolId);

    public static PoolDefinition CreateRuntime(
        GameObject prefab,
        int preloadCount,
        int maxActiveCount,
        int maxInactiveCount,
        bool recycleLeastRecentlyUsedActive,
        bool destroyOverflowInactive)
    {
        return new PoolDefinition
        {
            poolId = prefab != null ? prefab.name : string.Empty,
            prefab = prefab,
            preloadCount = preloadCount,
            maxActiveCount = maxActiveCount,
            maxInactiveCount = maxInactiveCount,
            recycleLeastRecentlyUsedActive = recycleLeastRecentlyUsedActive,
            destroyOverflowInactive = destroyOverflowInactive
        };
    }

    private static string ResolvePoolId(string configuredPoolId, GameObject configuredPrefab)
    {
        if (!string.IsNullOrWhiteSpace(configuredPoolId))
        {
            return configuredPoolId.Trim();
        }

        return configuredPrefab != null ? configuredPrefab.name : string.Empty;
    }
}
