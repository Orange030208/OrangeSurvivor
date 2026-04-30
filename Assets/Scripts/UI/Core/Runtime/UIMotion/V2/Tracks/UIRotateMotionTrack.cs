using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public sealed class UIRotateMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionVector3ValueMode fromMode = UIMotionVector3ValueMode.Current;
    [SerializeField] private Vector3 fromValue;
    [SerializeField] private UIMotionVector3ValueMode toMode = UIMotionVector3ValueMode.InitialPlusOffset;
    [SerializeField] private Vector3 toValue;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetTarget(TargetKey, out Transform target))
        {
            LogMissingTarget(nameof(Transform));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        Vector3 start = ResolveValue(fromMode, fromValue, target.localEulerAngles, snapshot.LocalEulerAngles);
        Vector3 end = ResolveValue(toMode, toValue, target.localEulerAngles, snapshot.LocalEulerAngles);
        target.localEulerAngles = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            target.localEulerAngles = end;
            return null;
        }

        return target.DOLocalRotate(end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetTarget(TargetKey, out Transform target)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        Vector3 current = target.localEulerAngles;
        Vector3 start = ResolveValue(fromMode, fromValue, current, snapshot.LocalEulerAngles);
        Vector3 end = ResolveValue(toMode, toValue, current, snapshot.LocalEulerAngles);
        target.localEulerAngles = Vector3.LerpUnclamped(start, end, normalizedTime);
    }

    private static Vector3 ResolveValue(UIMotionVector3ValueMode mode, Vector3 customValue, Vector3 currentValue, Vector3 initialValue)
    {
        return mode switch
        {
            UIMotionVector3ValueMode.Initial => initialValue,
            UIMotionVector3ValueMode.Custom => customValue,
            UIMotionVector3ValueMode.InitialPlusOffset => initialValue + customValue,
            UIMotionVector3ValueMode.InitialMultiplied => Vector3.Scale(initialValue, customValue),
            _ => currentValue
        };
    }
}
