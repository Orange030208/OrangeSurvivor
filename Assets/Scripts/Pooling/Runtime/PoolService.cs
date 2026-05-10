using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PoolService : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private PoolCatalogSO catalog;
    [SerializeField] private Transform inactiveRoot;
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool registerAsDefaultService = true;
    [SerializeField] private bool createPoolsForUnregisteredPrefabs = true;

    [Header("运行时默认值")]
    [Min(0)] [SerializeField] private int runtimePreloadCount;
    [Min(1)] [SerializeField] private int runtimeMaxActiveCount = 64;
    [Min(0)] [SerializeField] private int runtimeMaxInactiveCount = 64;
    [SerializeField] private bool runtimeRecycleLeastRecentlyUsedActive = true;
    [SerializeField] private bool runtimeDestroyOverflowInactive = true;

    private readonly Dictionary<GameObject, UnityLruPool> poolsByPrefab = new();
    private readonly Dictionary<string, UnityLruPool> poolsById = new(StringComparer.Ordinal);
    private bool isInitialized;

    public static PoolService Default { get; private set; }
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        if (registerAsDefaultService)
        {
            RegisterDefaultService();
        }

        if (initializeOnAwake)
        {
            Initialize();
        }
    }

    private void OnDestroy()
    {
        if (Default == this)
        {
            Default = null;
        }

        ClearAll(PoolReleaseReason.ServiceDestroyed);
    }

    public static bool TryGetDefault(out PoolService service)
    {
        service = Default;
        return service != null;
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        EnsureInactiveRoot();
        RegisterCatalogDefinitions();
        isInitialized = true;
    }

    public UnityLruPool RegisterPool(PoolDefinition definition, bool preload = true)
    {
        if (definition == null || !definition.IsValid)
        {
            Debug.LogWarning($"{nameof(PoolService)} ignored an invalid pool definition.", this);
            return null;
        }

        EnsureInactiveRoot();

        if (poolsByPrefab.TryGetValue(definition.Prefab, out UnityLruPool existingPool))
        {
            return existingPool;
        }

        UnityLruPool pool = new UnityLruPool(definition, inactiveRoot);
        poolsByPrefab.Add(definition.Prefab, pool);
        RegisterPoolId(definition.PoolId, pool);

        if (preload)
        {
            pool.Preload();
        }

        return pool;
    }

    public bool TryGetPool(GameObject prefab, out UnityLruPool pool)
    {
        EnsureInitialized();
        if (prefab == null)
        {
            pool = null;
            return false;
        }

        return poolsByPrefab.TryGetValue(prefab, out pool);
    }

    public bool TryGetPool(string poolId, out UnityLruPool pool)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(poolId))
        {
            pool = null;
            return false;
        }

        return poolsById.TryGetValue(poolId, out pool);
    }

    public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(PoolService)} cannot rent a null prefab.", this);
            return null;
        }

        EnsureInitialized();
        UnityLruPool pool = GetOrCreatePool(prefab);
        return pool != null ? pool.Rent(position, rotation, parent) : null;
    }

    public T Rent<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(PoolService)} cannot rent a null prefab component.", this);
            return null;
        }

        GameObject instance = Rent(prefab.gameObject, position, rotation, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public GameObject Rent(string poolId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        EnsureInitialized();
        if (!TryGetPool(poolId, out UnityLruPool pool))
        {
            Debug.LogWarning($"{nameof(PoolService)} cannot rent from unknown pool '{poolId}'.", this);
            return null;
        }

        return pool.Rent(position, rotation, parent);
    }

    public bool Return(GameObject instance)
    {
        if (instance == null)
        {
            return false;
        }

        PooledObjectHandle handle = instance.GetComponent<PooledObjectHandle>();
        if (handle == null || handle.Owner == null)
        {
            Debug.LogWarning($"{nameof(PoolService)} cannot return {instance.name}: no pool handle is bound.", instance);
            return false;
        }

        return handle.Owner.Return(instance, PoolReleaseReason.Manual);
    }

    public bool Return(Component instance)
    {
        return instance != null && Return(instance.gameObject);
    }

    public bool Touch(GameObject instance)
    {
        if (instance == null)
        {
            return false;
        }

        PooledObjectHandle handle = instance.GetComponent<PooledObjectHandle>();
        return handle != null && handle.Owner != null && handle.Owner.Touch(instance);
    }

    public int ReturnAllActive(PoolReleaseReason reason = PoolReleaseReason.Manual)
    {
        int returnedCount = 0;
        foreach (UnityLruPool pool in poolsByPrefab.Values)
        {
            returnedCount += pool.ReturnAllActive(reason);
        }

        return returnedCount;
    }

    public int ClearInactive(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        int clearedCount = 0;
        foreach (UnityLruPool pool in poolsByPrefab.Values)
        {
            clearedCount += pool.ClearInactive(reason);
        }

        return clearedCount;
    }

    public int ClearAll(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        int clearedCount = 0;
        foreach (UnityLruPool pool in poolsByPrefab.Values)
        {
            clearedCount += pool.ClearAll(reason);
        }

        poolsByPrefab.Clear();
        poolsById.Clear();
        return clearedCount;
    }

    public PoolSnapshot[] GetSnapshots()
    {
        EnsureInitialized();
        PoolSnapshot[] snapshots = new PoolSnapshot[poolsByPrefab.Count];
        int index = 0;
        foreach (UnityLruPool pool in poolsByPrefab.Values)
        {
            snapshots[index] = pool.Snapshot;
            index++;
        }

        return snapshots;
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void RegisterDefaultService()
    {
        if (Default != null && Default != this)
        {
            Debug.LogWarning($"{nameof(PoolService)} found multiple default services. Keeping {Default.name} as the default.", this);
            return;
        }

        Default = this;
    }

    private void RegisterCatalogDefinitions()
    {
        if (catalog == null)
        {
            return;
        }

        IReadOnlyList<PoolDefinition> definitions = catalog.Definitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            RegisterPool(definitions[i]);
        }
    }

    private UnityLruPool GetOrCreatePool(GameObject prefab)
    {
        if (poolsByPrefab.TryGetValue(prefab, out UnityLruPool existingPool))
        {
            return existingPool;
        }

        if (catalog != null && catalog.TryGetDefinition(prefab, out PoolDefinition catalogDefinition))
        {
            return RegisterPool(catalogDefinition);
        }

        if (!createPoolsForUnregisteredPrefabs)
        {
            Debug.LogWarning($"{nameof(PoolService)} has no pool definition for prefab '{prefab.name}'.", this);
            return null;
        }

        PoolDefinition runtimeDefinition = PoolDefinition.CreateRuntime(
            prefab,
            runtimePreloadCount,
            runtimeMaxActiveCount,
            runtimeMaxInactiveCount,
            runtimeRecycleLeastRecentlyUsedActive,
            runtimeDestroyOverflowInactive);

        return RegisterPool(runtimeDefinition);
    }

    private void RegisterPoolId(string poolId, UnityLruPool pool)
    {
        if (string.IsNullOrWhiteSpace(poolId))
        {
            return;
        }

        if (poolsById.TryGetValue(poolId, out UnityLruPool existingPool))
        {
            Debug.LogWarning(
                $"{nameof(PoolService)} ignored duplicate pool id '{poolId}'. Existing prefab: {existingPool.Prefab.name}, duplicate prefab: {pool.Prefab.name}.",
                this);
            return;
        }

        poolsById.Add(poolId, pool);
    }

    private void EnsureInactiveRoot()
    {
        if (inactiveRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("Pooled Objects");
        root.transform.SetParent(transform, false);
        inactiveRoot = root.transform;
    }
}
