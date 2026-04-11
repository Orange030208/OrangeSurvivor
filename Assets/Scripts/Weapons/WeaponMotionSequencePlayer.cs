using System;
using UnityEngine;

public sealed class WeaponMotionSequencePlayer
{
    private readonly Transform animatedTransform;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalEulerAngles;
    private AttackSequenceDefinitionSO currentSequence;
    private float elapsed;
    private int nextEventIndex;

    public bool IsPlaying { get; private set; }
    public event Action<WeaponSequenceEventContext> EventTriggered;
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

    public void Play(AttackSequenceDefinitionSO sequence)
    {
        currentSequence = sequence;
        elapsed = 0f;
        nextEventIndex = 0;
        IsPlaying = currentSequence != null;

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
        float normalizedTime = Mathf.Clamp01(elapsed / currentSequence.Duration);

        SampleAndApplyPose(normalizedTime);
        FlushEvents(normalizedTime);

        if (elapsed < currentSequence.Duration)
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
        elapsed = 0f;
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
            WeaponSequenceEventContext eventContext = keyframe.eventType switch
            {
                WeaponSequenceEventType.OpenHitWindow => WeaponSequenceEventContext.CreateWindowEvent(keyframe.eventType, keyframe.windowId),
                WeaponSequenceEventType.CloseHitWindow => WeaponSequenceEventContext.CreateWindowEvent(keyframe.eventType, keyframe.windowId),
                WeaponSequenceEventType.SpawnProjectile => WeaponSequenceEventContext.CreateProjectileEvent(keyframe.projectileSpawnPayload),
                _ => WeaponSequenceEventContext.CreateSimpleEvent(keyframe.eventType)
            };

            EventTriggered?.Invoke(eventContext);
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

            sampledPosition = Vector3.LerpUnclamped(defaultLocalPosition + from.localPosition, defaultLocalPosition + to.localPosition, easedT);
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
            case WeaponMotionEase.OutBack:
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float p = t - 1f;
                return 1f + c3 * p * p * p + c1 * p * p;
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
