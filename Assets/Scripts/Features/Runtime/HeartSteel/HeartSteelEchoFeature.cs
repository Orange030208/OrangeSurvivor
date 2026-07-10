using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HeartSteelEchoFeature : FeatureBase, IHeartSteelStackGainHandler
{
    private const int HIT_BUFFER_SIZE = 64;

    [SerializeField] private string targetWeaponId = "Weapon_NeonShield";
    [SerializeField, Min(0f)] private float radiusPoints = 180f;
    [SerializeField, Min(0f)] private float maxHealthDamagePercent = 6f;
    [SerializeField, Min(1)] private int maxTargets = 12;
    [SerializeField, Min(0f)] private float knockbackStrength;

    private readonly Collider2D[] hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
    private readonly List<Entity> processedTargets = new();

    public override string Title => "心钢回响";
    public override string Description => BuildDescription();

    private float Radius => PropValueUtility.DistancePointsToNonNegativeWorldUnits(radiusPoints);
    private float MaxHealthDamageRatio => PropValueUtility.PercentPointsToNonNegativeRatio(maxHealthDamagePercent);
    private int MaxTargets => Mathf.Max(1, maxTargets);
    private float KnockbackStrength => Mathf.Max(0f, knockbackStrength);

    public bool AppliesTo(string weaponId)
    {
        return string.IsNullOrWhiteSpace(targetWeaponId) ||
               string.Equals(targetWeaponId, weaponId, StringComparison.Ordinal);
    }

    public void OnHeartSteelStacksGained(HeartSteelStackGainContext context)
    {
        Entity owner = Context?.OwnerEntity;
        if (owner == null ||
            context.Owner != owner ||
            context.TriggerWeapon == null ||
            context.GainedStacks <= 0 ||
            !AppliesTo(context.WeaponId))
        {
            return;
        }

        float damage = CalculateDamage(context);
        if (damage <= 0f)
        {
            return;
        }

        Vector2 origin = ResolveOrigin(context);
        ApplyEcho(owner, context.TriggerWeapon, origin, damage);
    }

    private float CalculateDamage(HeartSteelStackGainContext context)
    {
        return context.CurrentMaxHealth * MaxHealthDamageRatio * context.GainedStacks;
    }

    private static Vector2 ResolveOrigin(HeartSteelStackGainContext context)
    {
        if (context.TriggerTarget != null)
        {
            return context.TriggerTarget.Center;
        }

        return context.TriggerHitResult.HitPoint;
    }

    private void ApplyEcho(Entity owner, Weapon triggerWeapon, Vector2 origin, float damage)
    {
        processedTargets.Clear();
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            origin,
            Radius,
            hitBuffer,
            triggerWeapon.TargetLayerMask);
        int appliedCount = 0;

        for (int i = 0; i < hitCount && appliedCount < MaxTargets; i++)
        {
            Entity target = FeatureRuntimeUtility.ResolveEntity(hitBuffer[i]);
            if (target == null || target == owner || processedTargets.Contains(target))
            {
                continue;
            }

            processedTargets.Add(target);
            ApplyEchoDamage(owner, target, origin, damage);
            appliedCount++;
        }
    }

    private void ApplyEchoDamage(Entity owner, Entity target, Vector2 origin, float damage)
    {
        Vector2 hitPoint = target.GetClosestPointTo(origin);
        Vector2 knockbackDirection = target.Center - origin;
        HitSpec hitSpec = new(damage, 0f, 1f, KnockbackStrength);
        HitRequest request = KnockbackStrength > 0f
            ? new HitRequest(owner, target, hitSpec, hitPoint, knockbackDirection, HitSourceKind.Explosion, origin)
            : new HitRequest(owner, target, hitSpec, hitPoint, HitSourceKind.Explosion, origin);

        HitService.Apply(request);
    }

    private string BuildDescription()
    {
        return $"获得心钢层数时，对目标周围最多 {MaxTargets} 个敌人造成最大生命 {maxHealthDamagePercent:0.##}% 的范围伤害。";
    }
}
