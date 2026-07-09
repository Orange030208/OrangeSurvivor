using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 爆炸命中：接触目标、阻挡物或生命周期结束时，在范围内提交 Explosion 类型伤害。
/// </summary>
public sealed class ExplosiveProjectileImpact : ProjectileImpactBehaviour
{
    private const int HIT_BUFFER_SIZE = 64;

    [Header("爆炸命中")]
    [Tooltip("爆炸半径，使用属性点口径，会通过 PropValueUtility 转成世界单位。")]
    [SerializeField, Min(0f)] private float radiusPoints = 180f;
    [Tooltip("爆炸对发射上下文伤害的额外倍率。最终伤害仍会叠加 ProjectileDefinitionSO.DamageMultiplier。")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [Tooltip("爆炸最多影响的目标数，避免一次范围查询造成过高峰值开销。")]
    [SerializeField, Min(1)] private int maxTargets = 12;
    [Tooltip("爆炸击退强度倍率。")]
    [SerializeField, Min(0f)] private float knockbackMultiplier = 1f;
    [SerializeField] private bool explodeOnTargetContact = true;
    [SerializeField] private bool explodeOnObstacleContact = true;
    [SerializeField] private bool explodeOnLifetimeExpired = true;
    [Tooltip("爆炸专用特效。为空时由 ProjectileDefinitionSO.ImpactVfxPrefab 作为默认命中特效兜底。")]
    [SerializeField] private GameObject explosionVfxPrefab;

    private readonly Collider2D[] hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
    private readonly HashSet<Entity> processedTargets = new();
    private bool hasExploded;

    public override void ResetState()
    {
        hasExploded = false;
        processedTargets.Clear();
    }

    public override ProjectileImpactResult HandleTargetContact(in ProjectileContact contact)
    {
        return explodeOnTargetContact
            ? Explode(contact.ImpactPosition)
            : ProjectileImpactResult.None;
    }

    public override ProjectileImpactResult HandleObstacleContact(in ProjectileContact contact)
    {
        return explodeOnObstacleContact
            ? Explode(contact.ImpactPosition)
            : ProjectileImpactResult.Despawn(false, contact.ImpactPosition);
    }

    public override ProjectileImpactResult HandleLifetimeExpired(Vector2 position)
    {
        return explodeOnLifetimeExpired
            ? Explode(position)
            : ProjectileImpactResult.Despawn(false, position);
    }

    private ProjectileImpactResult Explode(Vector2 origin)
    {
        if (hasExploded)
        {
            return ProjectileImpactResult.Despawn(false, origin);
        }

        hasExploded = true;
        ApplyExplosionDamage(origin);
        bool usedCustomVfx = SpawnExplosionVfx(origin);
        return ProjectileImpactResult.Despawn(!usedCustomVfx, origin);
    }

    private void ApplyExplosionDamage(Vector2 origin)
    {
        float radius = PropValueUtility.DistancePointsToNonNegativeWorldUnits(radiusPoints);
        if (radius <= 0f)
        {
            return;
        }

        int hitCount = AreaHitQueryUtility.OverlapCircleNonAlloc(
            origin,
            radius,
            hitBuffer,
            RuntimeContext.TargetLayerMask);

        processedTargets.Clear();
        int appliedCount = 0;
        int safeMaxTargets = Mathf.Max(1, maxTargets);
        HitSpec hitSpec = BuildHitSpec(damageMultiplier, knockbackMultiplier);

        for (int i = 0; i < hitCount && appliedCount < safeMaxTargets; i++)
        {
            Entity target = ResolveTarget(hitBuffer[i]);
            if (target == null || target == RuntimeContext.LaunchContext.Source || !processedTargets.Add(target))
            {
                continue;
            }

            Vector2 hitPoint = target.GetClosestPointTo(origin);
            Vector2 knockbackDirection = target.Center - origin;
            ApplyHit(
                target,
                hitSpec,
                hitPoint,
                knockbackDirection,
                HitSourceKind.Explosion,
                origin);
            appliedCount++;
        }
    }

    private bool SpawnExplosionVfx(Vector2 origin)
    {
        if (explosionVfxPrefab == null)
        {
            return false;
        }

        Quaternion rotation = RuntimeContext.Transform != null
            ? RuntimeContext.Transform.rotation
            : Quaternion.identity;
        RuntimeVfx.Spawn(explosionVfxPrefab, origin, rotation);
        return true;
    }

    private static Entity ResolveTarget(Collider2D collider)
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

    private void OnValidate()
    {
        radiusPoints = Mathf.Max(0f, radiusPoints);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        maxTargets = Mathf.Max(1, maxTargets);
        knockbackMultiplier = Mathf.Max(0f, knockbackMultiplier);
    }
}
