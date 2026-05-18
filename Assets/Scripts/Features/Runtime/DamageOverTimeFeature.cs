using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class DamageOverTimeFeature : FeatureBase
{
    private const float MinTickIntervalSeconds = 0.01f;

    [SerializeField, Min(0f)] private float damagePerSecond;
    [SerializeField, Min(MinTickIntervalSeconds)] private float tickIntervalSeconds = 1f;

    private float tickTimer;
    private float damageBuffer;

    public DamageOverTimeFeature()
    {
    }

    public DamageOverTimeFeature(float damagePerSecond, float tickIntervalSeconds)
    {
        this.damagePerSecond = Mathf.Max(0f, damagePerSecond);
        this.tickIntervalSeconds = Mathf.Max(MinTickIntervalSeconds, tickIntervalSeconds);
    }

    public float DamagePerSecond => Mathf.Max(0f, damagePerSecond);
    public float TickIntervalSeconds => Mathf.Max(MinTickIntervalSeconds, tickIntervalSeconds);
    public override string Title => "持续伤害";
    public override string Description => $"每秒造成 {DamagePerSecond:0.##} 点伤害";

    public override void OnInstall()
    {
        tickTimer = 0f;
        damageBuffer = 0f;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (Context?.OwnerEntity == null || Context.HealthComponent == null || DamagePerSecond <= 0f || deltaTime <= 0f)
        {
            return;
        }

        tickTimer += deltaTime;
        damageBuffer += DamagePerSecond * deltaTime;
        float interval = TickIntervalSeconds;
        if (tickTimer < interval)
        {
            return;
        }

        int tickCount = Mathf.FloorToInt(tickTimer / interval);
        tickTimer -= tickCount * interval;
        float damage = Mathf.Floor(damageBuffer);
        if (damage <= 0f)
        {
            return;
        }

        damageBuffer -= damage;
        Entity target = Context.OwnerEntity;
        Vector2 hitPoint = target.Center;
        HitResult result = new(
            null,
            target,
            damage,
            hitPoint,
            false,
            false,
            false,
            false,
            HitSourceKind.Buff,
            hitPoint);
        Context.HealthComponent.ApplyHitResult(result);
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        yield return new DescriptorInfo("持续伤害", $"{DamagePerSecond:0.##}/秒");
    }
}
