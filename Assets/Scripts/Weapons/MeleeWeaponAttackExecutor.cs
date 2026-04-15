using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 近战攻击执行器：
/// 在命中窗口打开期间，持续用 OverlapBox 检测敌人，
/// 并借助 hitTargets 防止同一窗口内重复命中同一目标。
/// 
/// 运行时可变配置约定：
/// - 命中目标层由武器持有器在装配后注入；
/// - 因此不要在构造时缓存 LayerMask，而是在执行命中检测时传入当前值。
/// </summary>
public sealed class MeleeWeaponAttackExecutor
{
    private readonly Transform hitOrigin;
    private readonly float innerCompensationRadius;

    public MeleeWeaponAttackExecutor(Transform hitOrigin, float innerCompensationRadius = 1.1f)
    {
        this.hitOrigin = hitOrigin;
        this.innerCompensationRadius = Mathf.Max(0.05f, innerCompensationRadius);
    }

    /// <summary>
    /// 检测命中盒从上一帧姿态扫到当前帧姿态所覆盖到的所有敌人，
    /// 并对本窗口内尚未命中过的目标结算伤害。
    /// </summary>
    public void ExecuteAttack(in WeaponAttackContext context, Vector2 hitBoxSize, HashSet<HealthComponent> hitTargets,
        LayerMask targetLayerMask, in MeleeHitDetectionPose fromPose, in MeleeHitDetectionPose toPose)
    {
        if (hitOrigin == null || hitTargets == null)
        {
            return;
        }

        int sampleCount = CalculateSampleCount(hitBoxSize, fromPose, toPose);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 1f : i / (sampleCount - 1f);
            Vector2 sampledPosition = Vector2.Lerp(fromPose.Position, toPose.Position, t);
            float sampledAngle = Mathf.LerpAngle(fromPose.RotationZ, toPose.RotationZ, t);
            Collider2D[] colliders = Physics2D.OverlapBoxAll(sampledPosition, hitBoxSize, sampledAngle, targetLayerMask);
            ApplyDamage(colliders, context, hitTargets);
        }
    }

    public MeleeHitDetectionPose CaptureCurrentPose()
    {
        return hitOrigin == null
            ? default
            : new MeleeHitDetectionPose(hitOrigin.position, hitOrigin.eulerAngles.z);
    }

    private int CalculateSampleCount(Vector2 hitBoxSize, in MeleeHitDetectionPose fromPose, in MeleeHitDetectionPose toPose)
    {
        float positionDelta = Vector2.Distance(fromPose.Position, toPose.Position);
        float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(fromPose.RotationZ, toPose.RotationZ));
        float minHitExtent = Mathf.Max(0.05f, Mathf.Min(hitBoxSize.x, hitBoxSize.y) * 0.5f);
        float positionStep = Mathf.Max(0.05f, minHitExtent / innerCompensationRadius);
        int positionSamples = Mathf.Max(1, Mathf.CeilToInt(positionDelta / positionStep) + 1);
        int rotationSamples = Mathf.Max(1, Mathf.CeilToInt(rotationDelta / 12f) + 1);
        return Mathf.Max(positionSamples, rotationSamples);
    }

    private static void ApplyDamage(Collider2D[] colliders, in WeaponAttackContext context, HashSet<HealthComponent> hitTargets)
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

            hitTargets.Add(healthComponent);
            healthComponent.TakeDamage(context.Hit.ToDamageInfo(healthComponent.transform.position));
        }
    }
}

public readonly struct MeleeHitDetectionPose
{
    public Vector2 Position { get; }
    public float RotationZ { get; }

    public MeleeHitDetectionPose(Vector2 position, float rotationZ)
    {
        Position = position;
        RotationZ = rotationZ;
    }
}

