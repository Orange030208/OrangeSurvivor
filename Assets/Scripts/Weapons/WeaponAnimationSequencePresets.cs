using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponAnimationSequencePresets
{
    private static readonly WeaponAnimationSequencePresetDefinition[] All =
    {
        new(WeaponAnimationSequencePresetId.LancePiercingDrive, "Lance Piercing Drive", "近战倾向：长枪戳刺 / 归一化前送",
            BuildLancePiercingDrive),
        new(WeaponAnimationSequencePresetId.SaberSweepingCut, "Saber Sweeping Cut", "近战倾向：中型挥舞击打 / 归一化横扫",
            BuildSaberSweepingCut),
        new(WeaponAnimationSequencePresetId.RifleReactorKick, "Rifle Reactor Kick", "远程倾向：单发枪械射击",
            BuildRifleReactorKick),
        new(WeaponAnimationSequencePresetId.TitanMaulOverheadBreak, "Titan Maul Overhead Break", "近战倾向：重型挥舞击打 / 归一化重砸",
            BuildTitanMaulOverheadBreak),
        new(WeaponAnimationSequencePresetId.BrotatoStickWhack, "Brotato Stick Whack", "近战倾向：木棍横扫 / 归一化横扫（Brotato 风格）",
            BuildBrotatoStickWhack),
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
        if (target == null) return;
        WeaponAnimationSequencePresetData data = GetData(id);
        target.Overwrite(data.Duration, true, data.MotionKeyframes, data.EventKeyframes);
    }

    private static WeaponAnimationSequencePresetData GetData(WeaponAnimationSequencePresetId id)
    {
        foreach (var preset in All)
            if (preset.Id == id)
                return preset.Builder();
        return All[0].Builder();
    }

    // ==================== 原有预设保留 ====================

    private static WeaponAnimationSequencePresetData BuildLancePiercingDrive() => D(
        "Lance Piercing Drive", 1.02f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),
            K(0.08f, -0.03f, -0.08f, 8f, 3),
            K(0.16f, -0.05f, -0.16f, 18f, 5),
            K(0.24f, -0.06f, -0.22f, 28f, 7),
            KDClamp(0.34f, 0.01f, 0.14f, 18f, 10, 0.12f, 0.24f),
            KDClamp(0.46f, 0.03f, 0.30f, 8f, 11, 0.16f, 0.40f),
            KDClamp(0.58f, 0.03f, 0.50f, -6f, 12, 0.18f, 0.62f),
            KDClamp(0.70f, 0.02f, 0.76f, -28f, 13, 0.20f, 0.84f),
            KDClamp(0.80f, 0.02f, 0.98f, -52f, 14, 0.18f, 1.00f),
            KDClamp(0.90f, 0.01f, 0.56f, -24f, 6, 0.16f, 0.58f),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.52f), C(0.80f), S(0.52f), V(0.62f) });

    private static WeaponAnimationSequencePresetData BuildSaberSweepingCut() => D(
        "Saber Sweeping Cut", 1.00f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),
            K(0.08f, -0.06f, -0.04f, 10f, 3),
            K(0.16f, -0.14f, -0.02f, 24f, 5),
            K(0.24f, -0.24f, 0.02f, 58f, 7),
            KDClamp(0.34f, -0.22f, 0.14f, 86f, 10, 0.18f, 0.32f),
            KDClamp(0.46f, -0.10f, 0.28f, 108f, 11, 0.22f, 0.44f),
            KDClamp(0.58f, 0.10f, 0.40f, 38f, 12, 0.30f, 0.56f),
            KDClamp(0.70f, 0.34f, 0.34f, -28f, 13, 0.40f, 0.66f),
            KDClamp(0.80f, 0.46f, 0.22f, -76f, 14, 0.48f, 0.78f),
            K(0.90f, 0.24f, 0.10f, -52f, 6),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.56f), C(0.80f), S(0.54f), V(0.62f) });

    private static WeaponAnimationSequencePresetData BuildRifleReactorKick() => D(
        "Rifle Reactor Kick", 0.94f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),
            K(0.07f, -0.01f, 0.05f, 6f, 3),
            K(0.14f, -0.02f, 0.11f, 12f, 5),
            K(0.22f, -0.03f, 0.17f, 20f, 7),
            K(0.30f, -0.04f, 0.22f, 30f, 10),
            K(0.38f, 0.01f, 0.02f, 4f, 11),
            K(0.44f, 0.05f, -0.18f, -24f, 12),
            K(0.52f, 0.07f, -0.30f, -48f, 13),
            K(0.64f, 0.05f, -0.18f, -24f, 9),
            K(0.78f, 0.01f, 0.02f, 4f, 5),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { P(0.36f, ProjectileSpawnPayload.Default), S(0.36f), V(0.37f) });

    private static WeaponAnimationSequencePresetData BuildTitanMaulOverheadBreak() => D(
        "Titan Maul Overhead Break", 1.24f,
        new()
        {
            K(0f, 0f, 0f, 0f, 0),
            K(0.08f, 0.03f, -0.05f, -8f, 1),
            K(0.16f, 0.06f, -0.12f, -18f, 3),
            K(0.26f, 0.11f, -0.22f, -38f, 5),
            K(0.38f, 0.18f, -0.30f, -78f, 7),
            KDClamp(0.50f, 0.14f, 0.10f, -42f, 10, 0.22f, 0.38f),
            KDClamp(0.60f, 0.04f, 0.28f, -6f, 11, 0.28f, 0.50f),
            KDClamp(0.70f, -0.08f, 0.52f, 30f, 12, 0.34f, 0.66f),
            KDClamp(0.80f, -0.14f, 0.78f, 72f, 13, 0.46f, 0.86f),
            KDClamp(0.90f, -0.08f, 0.42f, 96f, 9, 0.30f, 0.58f),
            K(1f, 0f, 0f, 0f, 3)
        },
        new() { O(0.62f), C(0.84f), S(0.46f), V(0.66f) });

    // 2. 在原有 BuildTitanMaulOverheadBreak() 方法之后、辅助方法之前，插入以下新的构建函数
//     （建议直接复制粘贴到 // ==================== 原有预设保留 ==================== 区域的末尾）

private static WeaponAnimationSequencePresetData BuildBrotatoStickWhack() => D(
    "Brotato Stick Whack", 1.08f,  // 总时长微增到 1.08s（增加 0.13s 呼吸空间），保证多帧也能保持“脆快”手感
    new()
    {
        // 0. 起始姿态（完全归零）
        K(0f, 0f, 0f, 0f, 0),

        // ==================== 预备阶段（更明显的后拉蓄力，共 4 帧，增加平滑度） ====================
        K(0.05f, -0.22f, 0.04f, 18f, 4),      // 轻微后拉
        K(0.12f, -0.41f, 0.11f, 47f, 7),      // 后拉加深（x 幅度已大幅提升）
        K(0.19f, -0.58f, 0.19f, 79f, 9),      // 后拉峰值（x=-0.58，为后续大横扫预留空间）
        K(0.26f, -0.49f, 0.26f, 98f, 8),      // 蓄力顶点，略微上抬，准备爆发

        // ==================== 核心横扫爆发阶段（共 6 帧，大幅增加 x 轴幅度 + 平滑过渡） ====================
        // 目标：x 轴最大值 2.18（比上一版 1.05 翻倍以上），横扫弧线极具冲击力
        KDClamp(0.34f, -0.12f, 0.31f, 134f, 10, 0.16f, 0.39f),   // 爆发起点（左→右快速启动）

        KDClamp(0.42f, 0.71f, 0.33f, 92f, 11, 0.28f, 0.61f),   // 中段加速，x 已大幅拉伸

        // 命中瞬间“鞭打过冲”关键帧（新增第 3 帧专门强化“打中怪物”弹性）
        KDClamp(0.51f, 1.46f, 0.24f, 41f, 12, 0.33f, 0.69f),   // x=1.46，过冲开始

        KDClamp(0.61f, 2.18f, 0.09f, -27f, 13, 0.44f, 0.72f),         // 峰值横扫！x=2.18（翻倍达成）

        // 继续横扫右侧跟进（两帧平滑过渡）
        KDClamp(0.72f, 1.89f, -0.06f, -68f, 14, 0.51f, 0.81f),
        KDClamp(0.81f, 1.37f, -0.14f, -103f, 11, 0.48f, 0.76f),

        // ==================== 回收阶段（共 3 帧，快速但平滑回中立） ====================
        K(0.90f, 0.62f, -0.09f, -61f, 7),
        K(0.98f, 0.21f, -0.03f, -29f, 5),
        K(1f, 0f, 0f, 0f, 3)
    },
    new() { O(0.48f), C(0.79f), S(0.47f), V(0.58f) });   // 命中窗口跟随新爆发节奏略微后移，特效更贴合鞭打峰值


    // ==================== 辅助方法 ====================

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
        keyframe.positionMode = WeaponMotionPositionMode.DynamicFromTarget;
        keyframe.dynamicPositionStrategy = WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius;
        keyframe.dynamicMinNormalizedReach = Mathf.Clamp01(Mathf.Min(minReach, maxReach));
        keyframe.dynamicMaxNormalizedReach = Mathf.Clamp01(Mathf.Max(minReach, maxReach));
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

    private static WeaponSequenceEventKeyframe P(float time, ProjectileSpawnPayload payload)
        => WeaponSequenceEventKeyframe.CreateProjectileEvent(time, payload);
}

public enum WeaponAnimationSequencePresetId
{
    LancePiercingDrive,
    SaberSweepingCut,
    RifleReactorKick,
    TitanMaulOverheadBreak,
    BrotatoStickWhack // 新增
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