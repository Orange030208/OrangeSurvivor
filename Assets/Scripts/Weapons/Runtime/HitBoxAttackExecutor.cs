using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HitBoxDetectionPose
{
    public Vector2 Position { get; }
    public float RotationZ { get; }

    public HitBoxDetectionPose(Vector2 position, float rotationZ)
    {
        Position = position;
        RotationZ = rotationZ;
    }
}

public readonly struct HitBoxDebugSample
{
    public HitBoxDetectionPose Pose { get; }
    public Vector2 Size { get; }
    public float Time { get; }

    public HitBoxDebugSample(in HitBoxDetectionPose pose, Vector2 size, float time)
    {
        Pose = pose;
        Size = size;
        Time = time;
    }
}

public sealed class HitBoxAttackExecutor
{
    private readonly float innerCompensationRadius;
    private readonly Action<Vector2> hitVfxCallback;

    public HitBoxAttackExecutor(Action<Vector2> hitVfxCallback, float innerCompensationRadius = 1.1f)
    {
        this.hitVfxCallback = hitVfxCallback;
        this.innerCompensationRadius = Mathf.Max(0.05f, innerCompensationRadius);
    }

    public void ExecuteAttack(
        Weapon weapon,
        Entity sourceEntity,
        HitSpec hitSpec,
        Vector2 hitBoxSize,
        HashSet<HealthComponent> hitTargets,
        LayerMask targetLayerMask,
        in HitBoxDetectionPose fromPose,
        in HitBoxDetectionPose toPose,
        Action<HitBoxDetectionPose> hitBoxDebugCallback = null)
    {
        if (hitTargets == null)
        {
            return;
        }

        int sampleCount = CalculateSampleCount(hitBoxSize, fromPose, toPose);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 1f : i / (sampleCount - 1f);
            Vector2 sampledPosition = Vector2.Lerp(fromPose.Position, toPose.Position, t);
            float sampledAngle = Mathf.LerpAngle(fromPose.RotationZ, toPose.RotationZ, t);
            hitBoxDebugCallback?.Invoke(new HitBoxDetectionPose(sampledPosition, sampledAngle));
            Collider2D[] colliders = Physics2D.OverlapBoxAll(sampledPosition, hitBoxSize, sampledAngle, targetLayerMask);
            ApplyDamage(colliders, weapon, sourceEntity, hitSpec, hitTargets, sampledPosition, hitVfxCallback);
        }
    }

    private int CalculateSampleCount(Vector2 hitBoxSize, in HitBoxDetectionPose fromPose, in HitBoxDetectionPose toPose)
    {
        float positionDelta = Vector2.Distance(fromPose.Position, toPose.Position);
        float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(fromPose.RotationZ, toPose.RotationZ));
        float minHitExtent = Mathf.Max(0.05f, Mathf.Min(hitBoxSize.x, hitBoxSize.y) * 0.5f);
        float positionStep = Mathf.Max(0.05f, minHitExtent / innerCompensationRadius);
        int positionSamples = Mathf.Max(1, Mathf.CeilToInt(positionDelta / positionStep) + 1);
        int rotationSamples = Mathf.Max(1, Mathf.CeilToInt(rotationDelta / 12f) + 1);
        return Mathf.Max(positionSamples, rotationSamples);
    }

    private static void ApplyDamage(
        Collider2D[] colliders,
        Weapon weapon,
        Entity sourceEntity,
        HitSpec hitSpec,
        HashSet<HealthComponent> hitTargets,
        Vector2 sourcePosition,
        Action<Vector2> hitVfxCallback)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].TryGetComponent(out HealthComponent healthComponent))
            {
                continue;
            }

            if (hitTargets.Contains(healthComponent))
            {
                continue;
            }

            Entity target = healthComponent.GetComponent<Entity>();
            if (target == null)
            {
                continue;
            }

            hitTargets.Add(healthComponent);
            Vector2 knockbackDirection = sourceEntity != null
                ? target.Center - sourceEntity.Center
                : target.Center - (Vector2)healthComponent.transform.position;
            HitRequest request = new HitRequest(
                sourceEntity,
                target,
                hitSpec,
                healthComponent.transform.position,
                knockbackDirection,
                HitSourceKind.Weapon,
                sourcePosition: sourcePosition,
                sourceWeapon: weapon);
            HitResult hitResult = weapon.ApplyHit(request);
            if (!hitResult.IsCancelled && !hitResult.IsDodged && !hitResult.IsBlocked && hitResult.FinalDamage > 0f)
            {
                hitVfxCallback?.Invoke(hitResult.HitPoint);
            }
        }
    }
}
