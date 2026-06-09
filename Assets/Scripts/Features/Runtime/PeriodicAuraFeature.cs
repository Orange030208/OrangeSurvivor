using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PeriodicAuraFeature : FeatureBase
{
    private const int HIT_BUFFER_SIZE = 64;
    private const float MIN_TICK_INTERVAL_SECONDS = 0.01f;

    [SerializeField, Min(0f)] private float radiusPoints = 100f;
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField, Min(1)] private int maxTargetsPerTick = 8;
    [SerializeField, Min(0f)] private float damagePerTick;
    [SerializeField, Min(MIN_TICK_INTERVAL_SECONDS)] private float tickIntervalSeconds = 1f;
    [SerializeField] private BuffDataSO buffData;
    [SerializeField] private bool overrideBuffDuration;
    [SerializeField] private BuffDurationPolicy buffDurationPolicy = BuffDurationPolicy.Timed;
    [SerializeField, Min(0f)] private float buffDurationSeconds = 5f;

    private readonly Collider2D[] hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
    private readonly List<Entity> processedTargets = new();
    private float tickTimer;

    public override string Title => "周期光环";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        tickTimer = 0f;
        processedTargets.Clear();
    }

    public override void OnUninstall()
    {
        tickTimer = 0f;
        processedTargets.Clear();
    }

    public override void OnUpdate(float deltaTime)
    {
        if (deltaTime <= 0f || Context?.OwnerEntity == null)
        {
            return;
        }

        float interval = Mathf.Max(MIN_TICK_INTERVAL_SECONDS, tickIntervalSeconds);
        tickTimer += deltaTime;
        if (tickTimer < interval)
        {
            return;
        }

        int elapsedTickCount = Mathf.FloorToInt(tickTimer / interval);
        tickTimer -= elapsedTickCount * interval;
        ApplyAuraTick();
    }

    private void ApplyAuraTick()
    {
        Entity owner = Context?.OwnerEntity;
        if (owner == null)
        {
            return;
        }

        processedTargets.Clear();
        Vector2 origin = owner.Center;
        float radius = PropValueUtility.DistancePointsToNonNegativeWorldUnits(radiusPoints);
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, hitBuffer, targetLayerMask);
        int appliedCount = 0;
        int safeMaxTargets = Mathf.Max(1, maxTargetsPerTick);

        for (int i = 0; i < hitCount && appliedCount < safeMaxTargets; i++)
        {
            Entity target = FeatureRuntimeUtility.ResolveEntity(hitBuffer[i]);
            if (target == null || target == owner || processedTargets.Contains(target))
            {
                continue;
            }

            processedTargets.Add(target);
            ApplyToTarget(owner, target, origin);
            appliedCount++;
        }
    }

    private void ApplyToTarget(Entity owner, Entity target, Vector2 origin)
    {
        if (damagePerTick > 0f)
        {
            Vector2 hitPoint = target.GetClosestPointTo(origin);
            HitRequest request = new(
                owner,
                target,
                new HitSpec(damagePerTick, 0f, 1f),
                hitPoint,
                HitSourceKind.Feature,
                origin);
            HitService.Apply(request);
        }

        FeatureRuntimeUtility.ApplyBuff(target, buffData, overrideBuffDuration, buffDurationPolicy, buffDurationSeconds);
    }

    private string BuildDescription()
    {
        List<string> parts = new();
        if (damagePerTick > 0f)
        {
            parts.Add($"每次造成 {damagePerTick:0.##} 点伤害");
        }

        if (buffData != null)
        {
            parts.Add($"施加 {buffData.DisplayName}");
        }

        string effectText = parts.Count > 0 ? string.Join("并", parts) : "未配置伤害或 Buff";
        return $"每 {Mathf.Max(MIN_TICK_INTERVAL_SECONDS, tickIntervalSeconds):0.##} 秒对范围内最多 {Mathf.Max(1, maxTargetsPerTick)} 个目标{effectText}。";
    }
}
