using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class UnityLruPool
{
    private readonly PoolDefinition definition;
    private readonly string poolId;
    private readonly GameObject prefab;
    private readonly Transform poolContainer;
    private readonly LruObjectPool<GameObject> pool;
    private readonly List<MonoBehaviour> callbackBuffer = new();

    public string PoolId => poolId;
    public GameObject Prefab => prefab;
    public int ActiveCount => pool.ActiveCount;
    public int InactiveCount => pool.InactiveCount;
    public int MaxActiveCount => pool.MaxActiveCount;
    public int MaxInactiveCount => pool.MaxInactiveCount;
    public PoolSnapshot Snapshot => new(
        poolId,
        prefab,
        ActiveCount,
        InactiveCount,
        MaxActiveCount,
        MaxInactiveCount);

    public UnityLruPool(PoolDefinition definition, Transform inactiveRoot)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (!definition.IsValid)
        {
            throw new ArgumentException($"{nameof(UnityLruPool)} requires a valid pool definition.", nameof(definition));
        }

        this.definition = definition;
        poolId = definition.PoolId;
        prefab = definition.Prefab;
        poolContainer = CreatePoolContainer(inactiveRoot, poolId);
        pool = new LruObjectPool<GameObject>(
            CreateInstance,
            definition.MaxActiveCount,
            definition.MaxInactiveCount,
            definition.RecycleLeastRecentlyUsedActive,
            definition.DestroyOverflowInactive,
            null,
            OnCoreRent,
            OnCoreReturn,
            OnCoreDiscard);
    }

    public int Preload()
    {
        return pool.Preload(definition.PreloadCount);
    }

    public GameObject Rent(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject instance = pool.Rent();
        if (instance == null)
        {
            Debug.LogWarning($"{nameof(UnityLruPool)} '{poolId}' cannot rent an instance because the active limit has been reached.");
            return null;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, true);
        instanceTransform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        NotifyRent(instance);
        return instance;
    }

    public bool Return(GameObject instance, PoolReleaseReason reason = PoolReleaseReason.Manual)
    {
        if (instance == null)
        {
            return false;
        }

        PooledObjectHandle handle = instance.GetComponent<PooledObjectHandle>();
        if (handle == null || handle.Owner != this)
        {
            Debug.LogWarning($"{nameof(UnityLruPool)} '{poolId}' rejected return for {instance.name}: instance belongs to another pool.", instance);
            return false;
        }

        return pool.Return(instance, reason);
    }

    public bool Touch(GameObject instance)
    {
        return pool.Touch(instance);
    }

    public int ReturnAllActive(PoolReleaseReason reason = PoolReleaseReason.Manual)
    {
        return pool.ReturnAllActive(reason);
    }

    public int ClearInactive(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        return pool.ClearInactive(reason);
    }

    public int ClearAll(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        int clearedCount = pool.ClearAll(reason);
        if (poolContainer != null)
        {
            DestroyObject(poolContainer.gameObject);
        }

        return clearedCount;
    }

    private GameObject CreateInstance()
    {
        GameObject instance = Object.Instantiate(prefab, poolContainer);
        instance.name = prefab.name;
        EnsureHandle(instance);
        return instance;
    }

    private void OnCoreRent(GameObject instance)
    {
        EnsureHandle(instance).MarkRented();
    }

    private void OnCoreReturn(GameObject instance, PoolReleaseReason reason)
    {
        if (instance == null)
        {
            return;
        }

        if (reason != PoolReleaseReason.Prewarm)
        {
            NotifyReturn(instance);
        }

        PooledObjectHandle handle = EnsureHandle(instance);
        handle.MarkReturned();
        instance.SetActive(false);
        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(poolContainer, false);
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
    }

    private void OnCoreDiscard(GameObject instance, PoolReleaseReason reason)
    {
        if (instance == null)
        {
            return;
        }

        NotifyDiscard(instance);
        DestroyObject(instance);
    }

    private PooledObjectHandle EnsureHandle(GameObject instance)
    {
        PooledObjectHandle handle = instance.GetComponent<PooledObjectHandle>();
        if (handle == null)
        {
            handle = instance.AddComponent<PooledObjectHandle>();
        }

        handle.Bind(this, poolId, prefab);
        return handle;
    }

    private void NotifyRent(GameObject instance)
    {
        NotifyPoolables(instance, poolable => poolable.OnRentFromPool());
    }

    private void NotifyReturn(GameObject instance)
    {
        NotifyPoolables(instance, poolable => poolable.OnReturnToPool());
    }

    private void NotifyDiscard(GameObject instance)
    {
        NotifyPoolables(instance, poolable => poolable.OnDiscardFromPool());
    }

    private void NotifyPoolables(GameObject instance, Action<IPoolable> notify)
    {
        callbackBuffer.Clear();
        instance.GetComponentsInChildren(true, callbackBuffer);
        for (int i = 0; i < callbackBuffer.Count; i++)
        {
            MonoBehaviour behaviour = callbackBuffer[i];
            if (behaviour is not IPoolable poolable)
            {
                continue;
            }

            try
            {
                notify.Invoke(poolable);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, behaviour);
            }
        }

        callbackBuffer.Clear();
    }

    private static Transform CreatePoolContainer(Transform inactiveRoot, string poolId)
    {
        string safePoolId = string.IsNullOrWhiteSpace(poolId) ? "Unnamed" : poolId.Replace('/', '_');
        GameObject container = new GameObject($"Pool - {safePoolId}");
        if (inactiveRoot != null)
        {
            container.transform.SetParent(inactiveRoot, false);
        }

        return container.transform;
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
            return;
        }

        Object.DestroyImmediate(target);
    }
}
