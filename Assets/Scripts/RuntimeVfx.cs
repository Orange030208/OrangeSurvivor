using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 运行时特效辅助：
/// - 统一实例化 VFX；
/// - 优先使用 prefab 上的 VfxLifetime 控制生命周期；
/// - 调用方也可以通过 overrideLifetime 直接覆盖。
/// </summary>
public static class RuntimeVfx
{
    private const float DefaultLifetime = 5f;
    private const float MinimumLifetime = 0.1f;
    private static readonly List<GameObject> activeInstances = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        activeInstances.Clear();
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float overrideLifetime = -1f)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, position, rotation, parent);
        TrackInstance(instance);
        VfxLifetime lifetime = instance.GetComponent<VfxLifetime>();
        if (lifetime != null)
        {
            lifetime.Activate(overrideLifetime);
            return instance;
        }

        float resolvedLifetime = overrideLifetime > 0f ? overrideLifetime : DefaultLifetime;
        if (Application.isPlaying)
        {
            Object.Destroy(instance, Mathf.Max(MinimumLifetime, resolvedLifetime));
        }

        return instance;
    }

    public static GameObject[] CreateActiveSnapshot()
    {
        PruneDestroyedInstances();
        return activeInstances.ToArray();
    }

    public static void ReleaseForWaveCleanup(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        activeInstances.Remove(instance);
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(instance);
            return;
        }

        if (instance.TryGetComponent(out VfxLifetime lifetime))
        {
            lifetime.ReleaseForWaveCleanup();
            return;
        }

        Object.Destroy(instance);
    }

    private static void TrackInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PruneDestroyedInstances();
        activeInstances.Add(instance);
    }

    private static void PruneDestroyedInstances()
    {
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            if (activeInstances[i] == null)
            {
                activeInstances.RemoveAt(i);
            }
        }
    }
}
