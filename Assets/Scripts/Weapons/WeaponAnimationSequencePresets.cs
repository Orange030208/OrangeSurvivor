using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponAnimationSequencePresets
{
    private static readonly WeaponAnimationSequencePresetDefinition[] All =
    {
        new(WeaponAnimationSequencePresetId.MeleeHeavySwing, "Melee Heavy Swing", "近战倾向：重型横甩 / 蓄力后爆发", BuildMeleeHeavySwing),
        new(WeaponAnimationSequencePresetId.MeleeArcSweepChargedWide, "Melee Arc Sweep Charged Wide", "近战倾向：蓄力大横扫 / 左侧重蓄后强力甩出", BuildMeleeArcSweepChargedWide),
        new(WeaponAnimationSequencePresetId.MeleeArcSweepHalfMoon, "Melee Arc Sweep Half Moon", "近战倾向：完整半圆弧 / 左右摆幅更对称", BuildMeleeArcSweepHalfMoon),
        new(WeaponAnimationSequencePresetId.MeleeArcSweep, "Melee Arc Sweep", "近战倾向：弧形扫击 / 连续圆弧轨迹", BuildMeleeArcSweep),
        new(WeaponAnimationSequencePresetId.RangedRifleKick, "Ranged Rifle Kick", "远程倾向：单发枪械射击", BuildRangedRifleKick),
    };

    public static IReadOnlyList<WeaponAnimationSequencePresetDefinition> GetAllPresets() => All;

    public static AttackSequenceDefinitionSO CreatePreset(WeaponAnimationSequencePresetId id)
    {
        WeaponAnimationSequencePresetData data = GetData(id);
        AttackSequenceDefinitionSO sequence =
            AttackSequenceDefinitionSO.CreateRuntimeSequence($"Runtime {data.Name}", data.Duration);
        sequence.Overwrite(data.Duration, true, data.MotionKeyframes, data.EventKeyframes);
        return sequence;
    }

    public static void ApplyPreset(AttackSequenceDefinitionSO target, WeaponAnimationSequencePresetId id)
    {
        if (target == null)
        {
            return;
        }

        WeaponAnimationSequencePresetData data = GetData(id);
        target.Overwrite(data.Duration, true, data.MotionKeyframes, data.EventKeyframes);
    }

    private static WeaponAnimationSequencePresetData GetData(WeaponAnimationSequencePresetId id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
            {
                return All[i].Builder();
            }
        }

        return All[0].Builder();
    }

    private static WeaponAnimationSequencePresetData BuildMeleeHeavySwing() => D(
        "Melee Heavy Swing", 1.14f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),

            // 左拉蓄力阶段：峰值更高，整体停留更久，强化“先拉满再砍”
            K(0.06f, -0.86f, 0.05f, 18f, 4),
            K(0.13f, -1.68f, 0.13f, 42f, 7),
            K(0.20f, -2.42f, 0.22f, 72f, 9),
            K(0.27f, -2.96f, 0.31f, 98f, 10),

            // 蓄力停顿：在左侧极限姿态保持更久
            K(0.36f, -2.98f, 0.33f, 104f, 2),
            K(0.45f, -2.92f, 0.32f, 102f, 2),
            K(0.52f, -2.78f, 0.30f, 96f, 2),

            // 快速挥砍：缩短总挥砍时间，但保留多帧以维持平滑感
            KDClamp(0.58f, -1.64f, 0.38f, 70f, 11, 0.05f, 0.46f),
            KDClamp(0.64f, -0.34f, 0.54f, 24f, 12, 0.05f, 0.62f),
            KDClamp(0.70f, 0.82f, 0.74f, -22f, 13, 0.05f, 0.84f),
            KDClamp(0.75f, 1.42f, 0.94f, -58f, 14, 0.05f, 1.00f),

            // 命中后小停顿，然后快速收回
            KDClamp(0.80f, 1.34f, 0.86f, -62f, 2, 0.05f, 0.90f),
            K(0.88f, 0.76f, 0.34f, -38f, 8),
            K(0.95f, 0.28f, 0.09f, -16f, 6),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.58f), C(0.84f), S(0.73f), V(0.77f) });

    private static WeaponAnimationSequencePresetData BuildMeleeArcSweepChargedWide() => D(
        "Melee Arc Sweep Charged Wide", 1.04f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),

            // 更强调左侧蓄力停顿：左拉接近 -3，但右侧不追求完全对称，突出“蓄力后甩出”
            K(0.06f, -0.64f, 0.08f, 16f, 4),
            K(0.12f, -1.46f, 0.18f, 38f, 7),
            K(0.19f, -2.24f, 0.28f, 66f, 9),
            K(0.26f, -2.82f, 0.38f, 90f, 10),
            K(0.34f, -3.02f, 0.46f, 104f, 2),
            K(0.42f, -2.96f, 0.48f, 102f, 2),

            // 中后段快速横扫出去，右侧保留力量感但不做完整对称半圆
            KDClamp(0.50f, -2.18f, 0.64f, 82f, 11, 0.05f, 0.58f),
            KDClamp(0.58f, -1.10f, 0.86f, 38f, 11, 0.05f, 0.76f),
            KDClamp(0.66f, 0.24f, 1.02f, -8f, 11, 0.05f, 0.90f),
            KDClamp(0.73f, 1.18f, 1.00f, -34f, 13, 0.05f, 0.88f),
            KDClamp(0.79f, 1.86f, 0.88f, -58f, 13, 0.05f, 0.80f),

            // 末端收势偏快，让它更像一记爆发横扫
            K(0.86f, 1.42f, 0.44f, -46f, 8),
            K(0.92f, 0.72f, 0.16f, -22f, 8),
            K(0.97f, 0.24f, 0.04f, -8f, 6),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.50f), C(0.83f), S(0.64f), V(0.69f) });

    private static WeaponAnimationSequencePresetData BuildMeleeArcSweepHalfMoon() => D(
        "Melee Arc Sweep Half Moon", 1.00f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),

            // 左侧起势同样拉满，但随后向右侧走出更完整、更对称的半圆轨迹
            K(0.06f, -0.62f, 0.08f, 16f, 4),
            K(0.12f, -1.36f, 0.18f, 36f, 7),
            K(0.18f, -2.10f, 0.30f, 60f, 9),
            K(0.25f, -2.72f, 0.42f, 84f, 10),
            K(0.32f, -2.98f, 0.50f, 102f, 2),

            // 左右摆幅更对称，强调完整半月感
            KDClamp(0.40f, -2.58f, 0.66f, 86f, 11, 0.05f, 0.60f),
            KDClamp(0.48f, -1.58f, 0.86f, 52f, 11, 0.05f, 0.74f),
            KDClamp(0.56f, -0.26f, 1.02f, 14f, 11, 0.05f, 0.86f),
            KDClamp(0.64f, 1.12f, 1.06f, -24f, 11, 0.05f, 0.94f),
            KDClamp(0.72f, 2.18f, 0.98f, -54f, 13, 0.05f, 0.88f),
            KDClamp(0.79f, 2.94f, 0.82f, -78f, 13, 0.05f, 0.76f),

            // 右侧也保留更明显尾迹，形成完整半圆收尾
            K(0.86f, 2.32f, 0.46f, -62f, 8),
            K(0.92f, 1.20f, 0.18f, -34f, 8),
            K(0.97f, 0.36f, 0.04f, -10f, 6),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.40f), C(0.83f), S(0.60f), V(0.66f) });

    private static WeaponAnimationSequencePresetData BuildMeleeArcSweep() => D(
        "Melee Arc Sweep", 0.96f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),

            // 起手抬刀并向左后侧收势，为后续圆弧扫击蓄势
            K(0.06f, -0.58f, 0.08f, 14f, 4),
            K(0.12f, -1.28f, 0.18f, 34f, 7),
            K(0.18f, -2.02f, 0.30f, 58f, 9),
            K(0.24f, -2.62f, 0.42f, 82f, 10),
            K(0.30f, -2.98f, 0.52f, 100f, 2),

            // 开始沿弧线前送，保持横向扫击感，同时让前伸按目标距离动态变化
            KDClamp(0.38f, -2.72f, 0.64f, 88f, 3, 0.05f, 0.56f),
            KDClamp(0.46f, -1.86f, 0.82f, 62f, 11, 0.05f, 0.70f),
            KDClamp(0.54f, -0.72f, 0.98f, 24f, 11, 0.05f, 0.82f),
            KDClamp(0.62f, 0.74f, 1.06f, -12f, 11, 0.05f, 0.92f),
            KDClamp(0.70f, 1.78f, 0.98f, -42f, 13, 0.05f, 0.88f),
            KDClamp(0.77f, 2.34f, 0.84f, -66f, 13, 0.05f, 0.78f),

            // 弧线末端略停顿，再顺势回收到待机
            K(0.84f, 1.86f, 0.46f, -52f, 8),
            K(0.90f, 1.02f, 0.18f, -28f, 8),
            K(0.96f, 0.34f, 0.05f, -10f, 6),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.38f), C(0.81f), S(0.56f), V(0.62f) });

    private static WeaponAnimationSequencePresetData BuildRangedRifleKick() => D(
        "Ranged Rifle Kick", 0.94f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),
            K(0.07f, -0.01f, 0.03f, 6f, 3),
            K(0.14f, -0.02f, 0.07f, 12f, 5),
            K(0.22f, -0.03f, 0.11f, 18f, 7),
            K(0.30f, -0.04f, 0.24f, 20f, 10),
            K(0.38f, 0.01f, 0.02f, 4f, 11),
            K(0.44f, 0.05f, -0.06f, -12f, 12),
            K(0.52f, 0.07f, -0.12f, -18f, 13),
            K(0.64f, 0.05f, -0.06f, -12f, 9),
            K(0.78f, 0.01f, 0.02f, 4f, 5),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { P(0.36f, 0), S(0.36f), V(0.37f) });

    private static WeaponAnimationSequencePresetData D(string name, float duration,
        List<WeaponMotionKeyframe> motions, List<WeaponSequenceEventKeyframe> events)
        => new(name, duration, motions, events);

    // 坐标约定：武器模型默认是“竖着放”的。
    // 也就是待机姿态下，武器沿 local +Y / transform.up 指向前方（从下到上是一根竖直武器）。
    // 因此这里的 x 表示横向偏移，y 表示沿武器朝向的前后伸缩；编写预设时不要把 x 当成前伸轴。
    private static WeaponMotionKeyframe K(float time, float x, float y, float z, int ease)
        => new(time, new Vector3(x, y, 0f), new Vector3(0f, 0f, z), (WeaponMotionEase)ease);

    private static WeaponMotionKeyframe KDClamp(float time, float x, float y, float z, int ease, float minReach,
        float maxReach)
    {
        var keyframe = K(time, x, y, z, ease);
        keyframe.xPositionMode = WeaponMotionPositionMode.Fixed;
        keyframe.yPositionMode = WeaponMotionPositionMode.DynamicFromTarget;
        keyframe.dynamicPositionStrategy = WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius;
        keyframe.yDynamicMinNormalizedReach = Mathf.Clamp01(Mathf.Min(minReach, maxReach));
        keyframe.yDynamicMaxNormalizedReach = Mathf.Clamp01(Mathf.Max(minReach, maxReach));
        return keyframe;
    }

    private static WeaponSequenceEventKeyframe O(float time)
        => WeaponSequenceEventKeyframe.CreateWindowEvent(time, WeaponSequenceEventType.OpenHitWindow, 0);

    private static WeaponSequenceEventKeyframe C(float time)
        => WeaponSequenceEventKeyframe.CreateWindowEvent(time, WeaponSequenceEventType.CloseHitWindow, 0);

    private static WeaponSequenceEventKeyframe S(float time)
        => WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.PlaySfx);

    private static WeaponSequenceEventKeyframe V(float time)
        => WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.PlayVfx);

    private static WeaponSequenceEventKeyframe P(float time, int eventKey)
        => WeaponSequenceEventKeyframe.CreateSimpleEvent(time, WeaponSequenceEventType.SpawnProjectile, eventKey);
}

public enum WeaponAnimationSequencePresetId
{
    MeleeHeavySwing,
    MeleeArcSweepChargedWide,
    MeleeArcSweepHalfMoon,
    MeleeArcSweep,
    RangedRifleKick,
}

public readonly struct WeaponAnimationSequencePresetDefinition
{
    public WeaponAnimationSequencePresetId Id { get; }
    public string DisplayName { get; }
    public string TendencySummary { get; }
    public Func<WeaponAnimationSequencePresetData> Builder { get; }

    public WeaponAnimationSequencePresetDefinition(WeaponAnimationSequencePresetId id, string displayName,
        string tendencySummary, Func<WeaponAnimationSequencePresetData> builder)
    {
        Id = id;
        DisplayName = displayName;
        TendencySummary = tendencySummary;
        Builder = builder;
    }
}

public readonly struct WeaponAnimationSequencePresetData
{
    public string Name { get; }
    public float Duration { get; }
    public IReadOnlyList<WeaponMotionKeyframe> MotionKeyframes { get; }
    public IReadOnlyList<WeaponSequenceEventKeyframe> EventKeyframes { get; }
    public int MotionFrameCount => MotionKeyframes?.Count ?? 0;
    public int EventCount => EventKeyframes?.Count ?? 0;

    public WeaponAnimationSequencePresetData(string name, float duration,
        IReadOnlyList<WeaponMotionKeyframe> motionKeyframes, IReadOnlyList<WeaponSequenceEventKeyframe> eventKeyframes)
    {
        Name = name;
        Duration = duration;
        MotionKeyframes = motionKeyframes;
        EventKeyframes = eventKeyframes;
    }
}
