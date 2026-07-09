using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 普通直击命中：沿用原有子弹语义，按穿透次数逐个目标造成 Projectile 类型伤害。
/// </summary>
public sealed class DirectProjectileImpact : ProjectileImpactBehaviour
{
    [Header("直接命中")]
    [Tooltip("弹体基础命中次数，会再叠加发射上下文中的 PierceCount。")]
    [SerializeField, Min(1)] private int baseMaxHitCount = 1;

    private readonly HashSet<HealthComponent> hitTargets = new();
    private int currentHitCount;
    private int currentMaxHitCount;

    public override void ResetState()
    {
        currentHitCount = 0;
        currentMaxHitCount = Mathf.Max(1, baseMaxHitCount + RuntimeContext.LaunchContext.PierceCount);
        hitTargets.Clear();
    }

    public override ProjectileImpactResult HandleTargetContact(in ProjectileContact contact)
    {
        if (currentHitCount >= currentMaxHitCount ||
            contact.HealthComponent == null ||
            !hitTargets.Add(contact.HealthComponent) ||
            !TryResolveTarget(contact.HealthComponent, out Entity target))
        {
            return ProjectileImpactResult.None;
        }

        currentHitCount++;
        ApplyHit(
            target,
            BuildHitSpec(),
            contact.HealthComponent.transform.position,
            RuntimeContext.LaunchContext.Direction,
            HitSourceKind.Projectile,
            RuntimeContext.LaunchContext.SpawnPosition);

        bool shouldDespawn = currentHitCount >= currentMaxHitCount;
        return shouldDespawn
            ? ProjectileImpactResult.Despawn(true, contact.ImpactPosition)
            : ProjectileImpactResult.KeepAlive(true, contact.ImpactPosition);
    }

    public override ProjectileImpactResult HandleObstacleContact(in ProjectileContact contact)
    {
        return ProjectileImpactResult.Despawn(true, contact.ImpactPosition);
    }

    public override ProjectileImpactResult HandleLifetimeExpired(Vector2 position)
    {
        return ProjectileImpactResult.Despawn(false, position);
    }

    private void OnValidate()
    {
        baseMaxHitCount = Mathf.Max(1, baseMaxHitCount);
    }
}
