using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponAnimationSequencePresets
{
    private static readonly WeaponAnimationSequencePresetDefinition[] All =
    {
        new(WeaponAnimationSequencePresetId.MeleeLightHorizontalSlash, "Melee Light Horizontal Slash", "轻度横砍：短蓄力、快速横向切过、快速回收。", BuildMeleeLightHorizontalSlash),
        new(WeaponAnimationSequencePresetId.MeleeHeavyHorizontalSlash, "Melee Heavy Horizontal Slash", "重度横砍：更深后摆、更长蓄力、更重的横向扫击。", BuildMeleeHeavyHorizontalSlash),
        new(WeaponAnimationSequencePresetId.MeleeDirectThrust, "Melee Direct Thrust", "直接戳刺：轻微后撤后直线前刺，命中窗口集中。", BuildMeleeDirectThrust),
        new(WeaponAnimationSequencePresetId.MeleeChargedThrust, "Melee Charged Thrust", "蓄力戳刺：明显后拉蓄力后高速前突。", BuildMeleeChargedThrust),
        new(WeaponAnimationSequencePresetId.RangedGunfireShot, "Ranged Gunfire Shot", "枪械射击：抬枪、开火后坐、回正。", BuildRangedGunfireShot),
        new(WeaponAnimationSequencePresetId.RangedStaffSpellcast, "Ranged Staff Spellcast", "法杖施法：引导摆动、前送释放、回收。", BuildRangedStaffSpellcast),
    };

    public static IReadOnlyList<WeaponAnimationSequencePresetDefinition> GetAllPresets() => All;

    public static AttackSequenceDefinitionSO CreatePreset(WeaponAnimationSequencePresetId id)
    {
        WeaponAnimationSequencePresetData data = GetData(id);
        AttackSequenceDefinitionSO sequence =
            AttackSequenceDefinitionSO.CreateRuntimeSequence($"Runtime {data.Name}", data.Duration);
        sequence.Overwrite(data.Duration, true, data.MotionKeyframes, data.EventKeyframes);
        sequence.ConfigureTargetOffsetMode(data.TargetOffsetMode);
        sequence.ConfigureRetargeting(data.ReferenceTargetOffset, data.RetargetScaleWeight, data.OppositeDirectionRetargetWeight);
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
        target.ConfigureTargetOffsetMode(data.TargetOffsetMode);
        target.ConfigureRetargeting(data.ReferenceTargetOffset, data.RetargetScaleWeight, data.OppositeDirectionRetargetWeight);
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

    private static WeaponAnimationSequencePresetData BuildMeleeLightHorizontalSlash() => Build(
        "Weapon Sequence - Melee Light Horizontal Slash",
        0.82f,
        34,
        t =>
        {
            if (t < 0.24f)
            {
                float p = Smooth(t / 0.24f);
                return K(t, Mathf.Lerp(0f, -1.36f, p), Mathf.Lerp(0f, 0.18f, p), Mathf.Lerp(0f, 42f, p), EaseFor(t, 0.24f, 0.60f));
            }

            if (t < 0.60f)
            {
                float p = Smooth((t - 0.24f) / 0.36f);
                return K(t, Mathf.Lerp(-1.36f, 1.42f, p), 0.34f + 0.14f * Mathf.Sin(Mathf.PI * p), Mathf.Lerp(42f, -48f, p), EaseFor(t, 0.24f, 0.60f));
            }

            float recover = Smooth((t - 0.60f) / 0.40f);
            return K(t, Mathf.Lerp(1.42f, 0f, recover), Mathf.Lerp(0.34f, 0f, recover), Mathf.Lerp(-48f, 0f, recover), EaseFor(t, 0.24f, 0.60f));
        },
        new() { O(0.28f), C(0.58f), S(0.41f), V(0.43f) },
        WeaponSequenceTargetOffsetMode.ActualTarget);

    private static WeaponAnimationSequencePresetData BuildMeleeHeavyHorizontalSlash() => Build(
        "Weapon Sequence - Melee Heavy Horizontal Slash",
        1.18f,
        38,
        t =>
        {
            if (t < 0.34f)
            {
                float p = Smooth(t / 0.34f);
                return K(t, Mathf.Lerp(0f, -2.15f, p), Mathf.Lerp(0f, 0.24f, p), Mathf.Lerp(0f, 76f, p), EaseFor(t, 0.47f, 0.76f));
            }

            if (t < 0.47f)
            {
                float p = (t - 0.34f) / 0.13f;
                return K(t, -2.15f + 0.08f * Mathf.Sin(Mathf.PI * p), 0.24f + 0.03f * Mathf.Sin(Mathf.PI * p), 76f + 4f * Mathf.Sin(Mathf.PI * p), EaseFor(t, 0.47f, 0.76f));
            }

            if (t < 0.76f)
            {
                float p = Smooth((t - 0.47f) / 0.29f);
                return K(t, Mathf.Lerp(-2.05f, 2.25f, p), 0.48f + 0.18f * Mathf.Sin(Mathf.PI * p), Mathf.Lerp(74f, -82f, p), EaseFor(t, 0.47f, 0.76f));
            }

            float recover = Smooth((t - 0.76f) / 0.24f);
            return K(t, Mathf.Lerp(2.25f, 0f, recover), Mathf.Lerp(0.48f, 0f, recover), Mathf.Lerp(-82f, 0f, recover), EaseFor(t, 0.47f, 0.76f));
        },
        new() { O(0.49f), C(0.82f), S(0.61f), V(0.65f) },
        WeaponSequenceTargetOffsetMode.ActualTarget);

    private static WeaponAnimationSequencePresetData BuildMeleeDirectThrust() => Build(
        "Weapon Sequence - Melee Direct Thrust",
        0.88f,
        34,
        t =>
        {
            if (t < 0.22f)
            {
                float p = Smooth(t / 0.22f);
                return K(t, Mathf.Lerp(0f, -0.18f, p), Mathf.Lerp(0f, -0.30f, p), Mathf.Lerp(0f, 8f, p), EaseFor(t, 0.22f, 0.50f));
            }

            if (t < 0.50f)
            {
                float p = Smooth((t - 0.22f) / 0.28f);
                return K(t, Mathf.Lerp(-0.18f, 0.05f, p), Mathf.Lerp(-0.30f, 1.86f, p), Mathf.Lerp(8f, -4f, p), EaseFor(t, 0.22f, 0.50f));
            }

            if (t < 0.62f)
            {
                float p = (t - 0.50f) / 0.12f;
                return K(t, 0.05f + 0.03f * Mathf.Sin(Mathf.PI * p * 3f), 1.86f - 0.04f * p, -4f + 2f * Mathf.Sin(Mathf.PI * p), EaseFor(t, 0.22f, 0.50f));
            }

            float recover = Smooth((t - 0.62f) / 0.38f);
            return K(t, Mathf.Lerp(0.05f, 0f, recover), Mathf.Lerp(1.82f, 0f, recover), Mathf.Lerp(-4f, 0f, recover), EaseFor(t, 0.22f, 0.50f));
        },
        new() { O(0.30f), C(0.60f), S(0.42f), V(0.45f) },
        WeaponSequenceTargetOffsetMode.MaxRangeAlongAimDirection);

    private static WeaponAnimationSequencePresetData BuildMeleeChargedThrust() => Build(
        "Weapon Sequence - Melee Charged Thrust",
        1.22f,
        40,
        t =>
        {
            if (t < 0.28f)
            {
                float p = Smooth(t / 0.28f);
                return K(t, Mathf.Lerp(0f, -0.34f, p), Mathf.Lerp(0f, -0.66f, p), Mathf.Lerp(0f, 18f, p), EaseFor(t, 0.48f, 0.68f));
            }

            if (t < 0.48f)
            {
                float p = (t - 0.28f) / 0.20f;
                return K(t, -0.34f + 0.07f * Mathf.Sin(Mathf.PI * p * 2f), -0.66f + 0.05f * Mathf.Sin(Mathf.PI * p), 18f + 4f * Mathf.Sin(Mathf.PI * p), EaseFor(t, 0.48f, 0.68f));
            }

            if (t < 0.68f)
            {
                float p = Smooth((t - 0.48f) / 0.20f);
                return K(t, Mathf.Lerp(-0.32f, 0.08f, p), Mathf.Lerp(-0.62f, 2.45f, p), Mathf.Lerp(18f, -8f, p), EaseFor(t, 0.48f, 0.68f));
            }

            if (t < 0.78f)
            {
                float p = (t - 0.68f) / 0.10f;
                return K(t, 0.08f + 0.04f * Mathf.Sin(Mathf.PI * p * 4f), 2.45f - 0.08f * p, -8f + 3f * Mathf.Sin(Mathf.PI * p), EaseFor(t, 0.48f, 0.68f));
            }

            float recover = Smooth((t - 0.78f) / 0.22f);
            return K(t, Mathf.Lerp(0.08f, 0f, recover), Mathf.Lerp(2.37f, 0f, recover), Mathf.Lerp(-8f, 0f, recover), EaseFor(t, 0.48f, 0.68f));
        },
        new() { O(0.52f), C(0.78f), S(0.62f), V(0.66f) },
        WeaponSequenceTargetOffsetMode.MaxRangeAlongAimDirection);

    private static WeaponAnimationSequencePresetData BuildRangedGunfireShot() => Build(
        "Weapon Sequence - Gunfire Shot",
        0.92f,
        32,
        t =>
        {
            if (t < 0.24f)
            {
                float p = Smooth(t / 0.24f);
                return K(t, Mathf.Lerp(0f, 0.02f, p), Mathf.Lerp(0f, 0.22f, p), Mathf.Lerp(0f, 5f, p), EaseFor(t, 0.24f, 0.38f));
            }

            if (t < 0.38f)
            {
                float p = Smooth((t - 0.24f) / 0.14f);
                return K(t, Mathf.Lerp(0.02f, -0.08f, p), Mathf.Lerp(0.22f, -0.18f, p), Mathf.Lerp(5f, -16f, p), EaseFor(t, 0.24f, 0.38f));
            }

            if (t < 0.55f)
            {
                float p = Smooth((t - 0.38f) / 0.17f);
                return K(t, Mathf.Lerp(-0.08f, 0.04f, p), Mathf.Lerp(-0.18f, 0.08f, p), Mathf.Lerp(-16f, 7f, p), EaseFor(t, 0.24f, 0.38f));
            }

            float recover = Smooth((t - 0.55f) / 0.45f);
            return K(t, Mathf.Lerp(0.04f, 0f, recover), Mathf.Lerp(0.08f, 0f, recover), Mathf.Lerp(7f, 0f, recover), EaseFor(t, 0.24f, 0.38f));
        },
        new() { P(0.32f, 0), S(0.32f), V(0.33f) },
        WeaponSequenceTargetOffsetMode.ActualTarget);

    private static WeaponAnimationSequencePresetData BuildRangedStaffSpellcast() => Build(
        "Weapon Sequence - Staff Spellcast",
        1.08f,
        36,
        t =>
        {
            if (t < 0.30f)
            {
                float p = Smooth(t / 0.30f);
                return K(t, 0.18f * Mathf.Sin(Mathf.PI * p), Mathf.Lerp(0f, 0.58f, p), Mathf.Lerp(0f, 28f, p), EaseFor(t, 0.58f, 0.72f));
            }

            if (t < 0.58f)
            {
                float p = (t - 0.30f) / 0.28f;
                return K(t, 0.22f * Mathf.Sin(Mathf.PI * 2f * p), 0.58f + 0.20f * Mathf.Sin(Mathf.PI * p), Mathf.Lerp(28f, -24f, Smooth(p)), EaseFor(t, 0.58f, 0.72f));
            }

            if (t < 0.72f)
            {
                float p = Smooth((t - 0.58f) / 0.14f);
                return K(t, Mathf.Lerp(0f, 0.04f, p), Mathf.Lerp(0.58f, 1.08f, p), Mathf.Lerp(-24f, 0f, p), EaseFor(t, 0.58f, 0.72f));
            }

            float recover = Smooth((t - 0.72f) / 0.28f);
            return K(t, Mathf.Lerp(0.04f, 0f, recover), Mathf.Lerp(1.08f, 0f, recover), 0f, EaseFor(t, 0.58f, 0.72f));
        },
        new() { P(0.62f, 0), S(0.60f), V(0.63f) },
        WeaponSequenceTargetOffsetMode.ActualTarget);

    private static WeaponAnimationSequencePresetData Build(string name, float duration, int frameCount,
        Func<float, WeaponMotionKeyframe> sampler,
        List<WeaponSequenceEventKeyframe> events,
        WeaponSequenceTargetOffsetMode targetOffsetMode)
    {
        List<WeaponMotionKeyframe> motions = new(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)(frameCount - 1);
            motions.Add(sampler(time));
        }

        motions[0] = K(0f, 0f, 0f, 0f, (int)WeaponMotionEase.Linear);
        motions[^1] = K(1f, 0f, 0f, 0f, (int)WeaponMotionEase.InOutSine);
        return new WeaponAnimationSequencePresetData(
            name,
            duration,
            motions,
            events,
            targetOffsetMode,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f));
    }

    private static WeaponMotionKeyframe K(float time, float x, float y, float z, int ease)
        => new(time, new Vector3(x, y, 0f), new Vector3(0f, 0f, z), (WeaponMotionEase)ease);

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

    private static float Smooth(float value) => value * value * (3f - 2f * value);

    private static int EaseFor(float time, float releaseStart, float impactEnd)
        => time < releaseStart ? (int)WeaponMotionEase.OutQuad : time < impactEnd ? (int)WeaponMotionEase.OutCubic : (int)WeaponMotionEase.InOutSine;
}

public enum WeaponAnimationSequencePresetId
{
    MeleeLightHorizontalSlash,
    MeleeHeavyHorizontalSlash,
    MeleeDirectThrust,
    MeleeChargedThrust,
    RangedGunfireShot,
    RangedStaffSpellcast,
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
    public WeaponSequenceTargetOffsetMode TargetOffsetMode { get; }
    public Vector2 ReferenceTargetOffset { get; }
    public Vector2 RetargetScaleWeight { get; }
    public Vector2 OppositeDirectionRetargetWeight { get; }
    public int MotionFrameCount => MotionKeyframes?.Count ?? 0;
    public int EventCount => EventKeyframes?.Count ?? 0;

    public WeaponAnimationSequencePresetData(string name, float duration,
        IReadOnlyList<WeaponMotionKeyframe> motionKeyframes, IReadOnlyList<WeaponSequenceEventKeyframe> eventKeyframes,
        WeaponSequenceTargetOffsetMode targetOffsetMode,
        Vector2 referenceTargetOffset,
        Vector2 retargetScaleWeight,
        Vector2 oppositeDirectionRetargetWeight)
    {
        Name = name;
        Duration = duration;
        MotionKeyframes = motionKeyframes;
        EventKeyframes = eventKeyframes;
        TargetOffsetMode = targetOffsetMode;
        ReferenceTargetOffset = referenceTargetOffset;
        RetargetScaleWeight = retargetScaleWeight;
        OppositeDirectionRetargetWeight = oppositeDirectionRetargetWeight;
    }
}
