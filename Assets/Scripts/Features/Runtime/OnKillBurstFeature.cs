using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class OnKillBurstFeature : FeatureBase
{
    private const int HIT_BUFFER_SIZE = 64;

    [SerializeField, Min(0f)] private float radiusPoints = 100f;
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0f)] private float knockbackStrength;
    [SerializeField, Min(1)] private int maxTargets = 8;
    [SerializeField, Min(0f)] private float cooldownSeconds;
    [SerializeField] private bool allowChainReaction;

    private readonly Collider2D[] hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
    private readonly List<Entity> processedTargets = new();
    private float cooldownRemaining;
    private bool isApplyingBurst;

    public override string Title => "击杀爆发";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        cooldownRemaining = 0f;
        isApplyingBurst = false;
        processedTargets.Clear();
        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied);
        cooldownRemaining = 0f;
        isApplyingBurst = false;
        processedTargets.Clear();
    }

    public override void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f || cooldownRemaining <= 0f)
        {
            return;
        }

        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        Entity owner = Context?.OwnerEntity;
        if (owner == null ||
            eventData.Source != owner ||
            eventData.Reason != EntityDeathReason.Combat ||
            cooldownRemaining > 0f ||
            (!allowChainReaction && isApplyingBurst))
        {
            return;
        }

        ApplyBurst(owner, eventData.Position);
    }

    private void ApplyBurst(Entity owner, Vector2 origin)
    {
        float radius = PropValueUtility.DistancePointsToNonNegativeWorldUnits(radiusPoints);
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, hitBuffer, targetLayerMask);
        int appliedCount = 0;
        int safeMaxTargets = Mathf.Max(1, maxTargets);

        processedTargets.Clear();
        isApplyingBurst = true;
        cooldownRemaining = Mathf.Max(0f, cooldownSeconds);
        try
        {
            for (int i = 0; i < hitCount && appliedCount < safeMaxTargets; i++)
            {
                Entity target = FeatureRuntimeUtility.ResolveEntity(hitBuffer[i]);
                if (target == null || target == owner || processedTargets.Contains(target))
                {
                    continue;
                }

                processedTargets.Add(target);
                ApplyBurstDamage(owner, target, origin);
                appliedCount++;
            }
        }
        finally
        {
            isApplyingBurst = false;
        }
    }

    private void ApplyBurstDamage(Entity owner, Entity target, Vector2 origin)
    {
        if (damage <= 0f)
        {
            return;
        }

        Vector2 hitPoint = target.GetClosestPointTo(origin);
        Vector2 knockbackDirection = target.Center - origin;
        HitRequest request = knockbackStrength > 0f
            ? new HitRequest(
                owner,
                target,
                new HitSpec(damage, 0f, 1f, knockbackStrength),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Explosion,
                origin)
            : new HitRequest(
                owner,
                target,
                new HitSpec(damage, 0f, 1f),
                hitPoint,
                HitSourceKind.Explosion,
                origin);
        HitService.Apply(request);
    }

    private string BuildDescription()
    {
        return $"击杀敌人后对范围内最多 {Mathf.Max(1, maxTargets)} 个目标造成 {Mathf.Max(0f, damage):0.##} 点爆发伤害。";
    }
}
