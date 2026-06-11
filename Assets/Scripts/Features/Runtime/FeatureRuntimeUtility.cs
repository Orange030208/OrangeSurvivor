using UnityEngine;

internal static class FeatureRuntimeUtility
{
    public static bool AllowsSourceKind(HitSourceKind sourceKind, HitSourceKind[] allowedSourceKinds)
    {
        if (allowedSourceKinds == null || allowedSourceKinds.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < allowedSourceKinds.Length; i++)
        {
            if (allowedSourceKinds[i] == sourceKind)
            {
                return true;
            }
        }

        return false;
    }

    public static Entity ResolveEntity(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        if (collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return healthComponent.GetComponent<Entity>();
        }

        return collider.GetComponent<Entity>();
    }

    public static void ApplyBuff(
        Entity target,
        BuffDataSO buffData,
        bool overrideDuration,
        BuffDurationPolicy durationPolicy,
        float durationSeconds)
    {
        if (target == null || buffData == null)
        {
            return;
        }

        BuffApplyRequest request = overrideDuration
            ? new BuffApplyRequest(buffData, durationPolicy, Mathf.Max(0f, durationSeconds))
            : new BuffApplyRequest(buffData);

        // Buff 请求直接落到目标实体的 BuffController，避免再经事件总线转发。
        if (target.TryGetComponent(out BuffController buffController))
        {
            buffController.ApplyBuff(request);
        }
    }
}
