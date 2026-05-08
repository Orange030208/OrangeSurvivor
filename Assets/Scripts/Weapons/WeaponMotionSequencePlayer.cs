using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a weapon motion sequence and applies target-offset retargeting at sample time.
/// </summary>
public sealed class WeaponMotionSequencePlayer
{
    private readonly Transform animatedTransform;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalEulerAngles;
    private AttackSequenceDefinitionSO currentSequence;
    private Vector2 currentTargetLocalOffset;
    private bool hasRetargetTarget;
    private float elapsed;
    private float playbackDuration;
    private int nextEventIndex;

    public bool IsPlaying { get; private set; }
    public event Action<WeaponSequenceEventType, int> EventTriggered;
    public event Action Completed;

    public WeaponMotionSequencePlayer(Transform animatedTransform)
    {
        this.animatedTransform = animatedTransform;
        CacheDefaultPose();
    }

    public void CacheDefaultPose()
    {
        if (animatedTransform == null)
        {
            return;
        }

        defaultLocalPosition = animatedTransform.localPosition;
        defaultLocalEulerAngles = animatedTransform.localEulerAngles;
    }

    public void Play(AttackSequenceDefinitionSO sequence, float durationOverride = -1f)
    {
        Play(sequence, default, false, durationOverride);
    }

    public void Play(AttackSequenceDefinitionSO sequence, Vector2 targetLocalOffset, float durationOverride = -1f)
    {
        Play(sequence, targetLocalOffset, true, durationOverride);
    }

    private void Play(AttackSequenceDefinitionSO sequence, Vector2 targetLocalOffset, bool retarget, float durationOverride)
    {
        currentSequence = sequence;
        currentTargetLocalOffset = targetLocalOffset;
        hasRetargetTarget = retarget;
        elapsed = 0f;
        nextEventIndex = 0;
        IsPlaying = currentSequence != null;
        playbackDuration = currentSequence != null
            ? Mathf.Max(0.01f, durationOverride > 0f ? durationOverride : currentSequence.Duration)
            : 0f;

        if (!IsPlaying)
        {
            RestoreDefaultPose();
            return;
        }

        SampleAndApplyPose(0f);
        FlushEvents(0f);
    }

    public void Tick(float deltaTime)
    {
        if (!IsPlaying || currentSequence == null)
        {
            return;
        }

        elapsed += deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsed / playbackDuration);

        SampleAndApplyPose(normalizedTime);
        FlushEvents(normalizedTime);

        if (elapsed < playbackDuration)
        {
            return;
        }

        IsPlaying = false;
        if (currentSequence.RestoreDefaultPoseOnComplete)
        {
            RestoreDefaultPose();
        }

        Completed?.Invoke();
    }

    public void Stop(bool restoreDefaultPose = true)
    {
        IsPlaying = false;
        currentSequence = null;
        currentTargetLocalOffset = default;
        hasRetargetTarget = false;
        elapsed = 0f;
        playbackDuration = 0f;
        nextEventIndex = 0;

        if (restoreDefaultPose)
        {
            RestoreDefaultPose();
        }
    }

    private void FlushEvents(float normalizedTime)
    {
        if (currentSequence == null)
        {
            return;
        }

        IReadOnlyList<WeaponSequenceEventKeyframe> events = currentSequence.EventKeyframes;
        while (nextEventIndex < events.Count && normalizedTime >= events[nextEventIndex].normalizedTime)
        {
            WeaponSequenceEventKeyframe keyframe = events[nextEventIndex];
            EventTriggered?.Invoke(keyframe.eventType, keyframe.eventKey);
            nextEventIndex++;
        }
    }

    private void SampleAndApplyPose(float normalizedTime)
    {
        if (animatedTransform == null || currentSequence == null)
        {
            return;
        }

        Vector3 sampledPosition = defaultLocalPosition;
        IReadOnlyList<WeaponMotionKeyframe> keyframes = currentSequence.MotionKeyframes;
        if (keyframes != null && keyframes.Count > 0)
        {
            WeaponMotionKeyframe from = keyframes[0];
            WeaponMotionKeyframe to = keyframes[keyframes.Count - 1];

            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                WeaponMotionKeyframe current = keyframes[i];
                WeaponMotionKeyframe next = keyframes[i + 1];
                if (normalizedTime >= current.normalizedTime && normalizedTime <= next.normalizedTime)
                {
                    from = current;
                    to = next;
                    break;
                }
            }

            float segmentLength = Mathf.Max(0.0001f, to.normalizedTime - from.normalizedTime);
            float linearT = Mathf.Clamp01((normalizedTime - from.normalizedTime) / segmentLength);
            float easedT = EvaluateEase(linearT, to.ease, to.customCurve);

            Vector3 fromLocalPosition = ResolveLocalPosition(from);
            Vector3 toLocalPosition = ResolveLocalPosition(to);
            sampledPosition = Vector3.LerpUnclamped(defaultLocalPosition + fromLocalPosition, defaultLocalPosition + toLocalPosition, easedT);

            Quaternion fromRotation = Quaternion.Euler(defaultLocalEulerAngles + from.localEulerAngles);
            Quaternion toRotation = Quaternion.Euler(defaultLocalEulerAngles + to.localEulerAngles);
            animatedTransform.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, easedT);
        }

        animatedTransform.localPosition = sampledPosition;
        if (keyframes == null || keyframes.Count == 0)
        {
            animatedTransform.localEulerAngles = defaultLocalEulerAngles;
        }
    }

    private Vector3 ResolveLocalPosition(WeaponMotionKeyframe keyframe)
    {
        Vector2 localPosition = new(keyframe.localPositionX, keyframe.localPositionY);
        if (hasRetargetTarget && currentSequence != null)
        {
            localPosition = RetargetLocalPosition(
                localPosition,
                currentSequence.ReferenceTargetOffset,
                currentTargetLocalOffset,
                currentSequence.RetargetScaleWeight,
                currentSequence.OppositeDirectionRetargetWeight);
        }

        return new Vector3(localPosition.x, localPosition.y, 0f);
    }

    private static Vector2 RetargetLocalPosition(
        Vector2 localPosition,
        Vector2 referenceTargetOffset,
        Vector2 targetLocalOffset,
        Vector2 scaleWeight,
        Vector2 oppositeDirectionWeight)
    {
        return new Vector2(
            RetargetAxis(localPosition.x, referenceTargetOffset.x, targetLocalOffset.x, scaleWeight.x, oppositeDirectionWeight.x),
            RetargetAxis(localPosition.y, referenceTargetOffset.y, targetLocalOffset.y, scaleWeight.y, oppositeDirectionWeight.y));
    }

    private static float RetargetAxis(float localValue, float referenceValue, float targetValue, float weight, float oppositeDirectionWeight)
    {
        float clampedWeight = ResolveEffectiveRetargetWeight(localValue, referenceValue, weight, oppositeDirectionWeight);
        if (clampedWeight <= 0f || Mathf.Approximately(localValue, 0f) || Mathf.Abs(referenceValue) < 0.0001f)
        {
            return localValue;
        }

        float scale = Mathf.Lerp(1f, targetValue / referenceValue, clampedWeight);
        return localValue * scale;
    }

    private static float ResolveEffectiveRetargetWeight(float localValue, float referenceValue, float weight, float oppositeDirectionWeight)
    {
        float clampedWeight = Mathf.Clamp01(weight);
        if (clampedWeight <= 0f ||
            Mathf.Approximately(localValue, 0f) ||
            Mathf.Abs(referenceValue) < 0.0001f)
        {
            return clampedWeight;
        }

        bool movesOppositeReference = Mathf.Sign(localValue) != Mathf.Sign(referenceValue);
        if (!movesOppositeReference)
        {
            return clampedWeight;
        }

        return clampedWeight * Mathf.Clamp01(oppositeDirectionWeight);
    }

    private float EvaluateEase(float t, WeaponMotionEase ease, AnimationCurve customCurve)
    {
        switch (ease)
        {
            case WeaponMotionEase.InSine:
                return 1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
            case WeaponMotionEase.OutSine:
                return Mathf.Sin((t * Mathf.PI) * 0.5f);
            case WeaponMotionEase.InOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
            case WeaponMotionEase.InQuad:
                return t * t;
            case WeaponMotionEase.OutQuad:
                return 1f - ((1f - t) * (1f - t));
            case WeaponMotionEase.InOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            case WeaponMotionEase.InCubic:
                return t * t * t;
            case WeaponMotionEase.OutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);
            case WeaponMotionEase.InOutCubic:
                return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
            case WeaponMotionEase.InExpo:
                return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
            case WeaponMotionEase.OutExpo:
                return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            case WeaponMotionEase.InOutExpo:
                if (t <= 0f)
                {
                    return 0f;
                }

                if (t >= 1f)
                {
                    return 1f;
                }

                return t < 0.5f
                    ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                    : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
            case WeaponMotionEase.OutBack:
                const float C1 = 1.70158f;
                const float C3 = C1 + 1f;
                float p = t - 1f;
                return 1f + C3 * p * p * p + C1 * p * p;
            case WeaponMotionEase.OutElastic:
                if (t <= 0f)
                {
                    return 0f;
                }

                if (t >= 1f)
                {
                    return 1f;
                }

                const float C4 = (2f * Mathf.PI) / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * C4) + 1f;
            case WeaponMotionEase.CustomCurve:
                return customCurve != null ? customCurve.Evaluate(t) : t;
            default:
                return t;
        }
    }

    private void RestoreDefaultPose()
    {
        if (animatedTransform == null)
        {
            return;
        }

        animatedTransform.localPosition = defaultLocalPosition;
        animatedTransform.localEulerAngles = defaultLocalEulerAngles;
    }
}
