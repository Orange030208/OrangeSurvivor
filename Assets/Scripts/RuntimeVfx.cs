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

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float overrideLifetime = -1f)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, position, rotation, parent);
        VfxLifetime lifetime = instance.GetComponent<VfxLifetime>();
        if (lifetime != null)
        {
            lifetime.Activate(overrideLifetime);
            return instance;
        }

        float resolvedLifetime = overrideLifetime > 0f ? overrideLifetime : DefaultLifetime;
        Object.Destroy(instance, Mathf.Max(MinimumLifetime, resolvedLifetime));
        return instance;
    }
}
