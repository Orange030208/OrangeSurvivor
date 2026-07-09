using UnityEngine;

public sealed class DistanceProjectileLifetime : ProjectileLifetimeBehaviour
{
    [Header("生命周期")]
    [Tooltip("未收到最远飞行距离时的兜底存活时间，会再乘以 ProjectileDefinitionSO.LifetimeMultiplier。")]
    [SerializeField, Min(0f)] private float fallbackMaxLifetime = 5f;

    private float traveledDistance;
    private float elapsedTime;
    private Vector2 lastPosition;

    public override void ResetState()
    {
        traveledDistance = 0f;
        elapsedTime = 0f;
        lastPosition = RuntimeContext.Transform != null
            ? (Vector2)RuntimeContext.Transform.position
            : Vector2.zero;
    }

    public override ProjectileLifetimeResult Tick(float deltaTime)
    {
        Transform runtimeTransform = RuntimeContext.Transform;
        if (runtimeTransform == null)
        {
            return ProjectileLifetimeResult.Active;
        }

        Vector2 currentPosition = runtimeTransform.position;
        traveledDistance += Vector2.Distance(lastPosition, currentPosition);
        elapsedTime += Mathf.Max(0f, deltaTime);
        lastPosition = currentPosition;

        if (RuntimeContext.MaxTravelDistance > 0f)
        {
            return traveledDistance >= RuntimeContext.MaxTravelDistance
                ? ProjectileLifetimeResult.Expired(currentPosition)
                : ProjectileLifetimeResult.Active;
        }

        float resolvedLifetime = fallbackMaxLifetime * ResolveLifetimeMultiplier();
        return resolvedLifetime > 0f && elapsedTime >= resolvedLifetime
            ? ProjectileLifetimeResult.Expired(currentPosition)
            : ProjectileLifetimeResult.Active;
    }

    private float ResolveLifetimeMultiplier()
    {
        ProjectileDefinitionSO definition = RuntimeContext.Definition;
        return definition != null ? definition.LifetimeMultiplier : 1f;
    }

    private void OnValidate()
    {
        fallbackMaxLifetime = Mathf.Max(0f, fallbackMaxLifetime);
    }
}
