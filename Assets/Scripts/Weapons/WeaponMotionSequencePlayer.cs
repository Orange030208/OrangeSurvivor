using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 纯代码序列播放器：
/// - 根据 AttackSequenceDefinitionSO 采样位移/旋转关键帧；
/// - 根据事件关键帧发出命中窗口、发射、特效等事件。
/// WeaponSequenceBridge 只是 MonoBehaviour 包装层，真正的时间推进和插值都在这里。
/// </summary>
public sealed class WeaponMotionSequencePlayer
{
    private readonly Transform animatedTransform;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalEulerAngles;
    private AttackSequenceDefinitionSO currentSequence;
    private IReadOnlyDictionary<int, Vector3> currentPositionOverrides;
    private float currentReachScale = 1f;
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

    public void Play(AttackSequenceDefinitionSO sequence, float durationOverride = -1f, float reachScale = 1f)
    {
        Play(sequence, null, durationOverride, reachScale);
    }

    public void Play(AttackSequenceDefinitionSO sequence, IReadOnlyDictionary<int, Vector3> localPositionOverrides, float durationOverride = -1f, float reachScale = 1f)
    {
        currentSequence = sequence;
        currentPositionOverrides = localPositionOverrides;
        currentReachScale = Mathf.Max(0.01f, reachScale);
        elapsed = 0f;
        nextEventIndex = 0;
        IsPlaying = currentSequence != null;
        playbackDuration = currentSequence != null
            ? Mathf.Max(0.01f, durationOverride > 0f ? durationOverride : currentSequence.Duration)
            : 0f;

        if (!IsPlaying)
        {
            RestoreDefaultPose();
        }
        else
        {
            SampleAndApplyPose(0f);
            FlushEvents(0f);
        }
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
        currentPositionOverrides = null;
        currentReachScale = 1f;
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

        var events = currentSequence.EventKeyframes;
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
        var keyframes = currentSequence.MotionKeyframes;
        if (keyframes != null && keyframes.Count > 0)
        {
            WeaponMotionKeyframe from = keyframes[0];
            WeaponMotionKeyframe to = keyframes[keyframes.Count - 1];
            int fromIndex = 0;
            int toIndex = keyframes.Count - 1;

            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                WeaponMotionKeyframe current = keyframes[i];
                WeaponMotionKeyframe next = keyframes[i + 1];
                if (normalizedTime >= current.normalizedTime && normalizedTime <= next.normalizedTime)
                {
                    from = current;
                    to = next;
                    fromIndex = i;
                    toIndex = i + 1;
                    break;
                }
            }

            float segmentLength = Mathf.Max(0.0001f, to.normalizedTime - from.normalizedTime);
            float linearT = Mathf.Clamp01((normalizedTime - from.normalizedTime) / segmentLength);
            float easedT = EvaluateEase(linearT, to.ease, to.customCurve);

            Vector3 fromLocalPosition = ResolveLocalPosition(keyframes, fromIndex, from);
            Vector3 toLocalPosition = ResolveLocalPosition(keyframes, toIndex, to);
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

    private Vector3 ResolveLocalPosition(IReadOnlyList<WeaponMotionKeyframe> keyframes, int keyframeIndex, WeaponMotionKeyframe keyframe)
    {
        if (currentPositionOverrides != null && currentPositionOverrides.TryGetValue(keyframeIndex, out Vector3 overridePosition))
        {
            return overridePosition;
        }

        // 固定帧始终按配置里的分离 X/Y 位置直接播放，不受武器 Range 影响。
        // 只有动态轴才会在外部先结合当前目标和攻击半径解出真实落点，再通过 override 传进来。
        return new Vector3(keyframe.localPositionX, keyframe.localPositionY, 0f);
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
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float p = t - 1f;
                return 1f + c3 * p * p * p + c1 * p * p;
            case WeaponMotionEase.OutElastic:
                if (t <= 0f)
                {
                    return 0f;
                }
                if (t >= 1f)
                {
                    return 1f;
                }
                const float c4 = (2f * Mathf.PI) / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
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
