using UnityEngine;

/// <summary>
/// 弹射物命中模块基类，负责把接触事件转换成具体命中语义。
/// 普通子弹和火箭弹的差异主要落在这里。
/// </summary>
public abstract class ProjectileImpactBehaviour : MonoBehaviour
{
    protected ProjectileRuntimeContext RuntimeContext { get; private set; }

    public virtual void Initialize(in ProjectileRuntimeContext context)
    {
        RuntimeContext = context;
    }

    public virtual void ResetState()
    {
    }

    public abstract ProjectileImpactResult HandleTargetContact(in ProjectileContact contact);
    public abstract ProjectileImpactResult HandleObstacleContact(in ProjectileContact contact);
    public abstract ProjectileImpactResult HandleLifetimeExpired(Vector2 position);

    protected bool TryResolveTarget(HealthComponent healthComponent, out Entity target)
    {
        target = healthComponent != null ? healthComponent.GetComponent<Entity>() : null;
        return target != null;
    }

    protected HitSpec BuildHitSpec(float extraDamageMultiplier = 1f, float knockbackMultiplier = 1f)
    {
        ProjectileLaunchContext launchContext = RuntimeContext.LaunchContext;
        float definitionDamageMultiplier = RuntimeContext.Definition != null
            ? RuntimeContext.Definition.DamageMultiplier
            : 1f;

        return new HitSpec(
            launchContext.HitSpec.BaseDamage * definitionDamageMultiplier * Mathf.Max(0f, extraDamageMultiplier),
            launchContext.HitSpec.CritChance,
            launchContext.HitSpec.CritMultiplier,
            launchContext.HitSpec.KnockbackStrength * Mathf.Max(0f, knockbackMultiplier));
    }

    protected HitResult ApplyHit(
        Entity target,
        HitSpec hitSpec,
        Vector2 hitPoint,
        Vector2 knockbackDirection,
        HitSourceKind sourceKind,
        Vector2 sourcePosition)
    {
        ProjectileLaunchContext launchContext = RuntimeContext.LaunchContext;
        HitRequest request = hitSpec.KnockbackStrength > 0f
            ? new HitRequest(
                launchContext.Source,
                target,
                hitSpec,
                hitPoint,
                knockbackDirection,
                sourceKind,
                sourcePosition,
                launchContext.DamageSource)
            : new HitRequest(
                launchContext.Source,
                target,
                hitSpec,
                hitPoint,
                sourceKind,
                sourcePosition,
                launchContext.DamageSource);

        return HitService.Apply(request);
    }
}
