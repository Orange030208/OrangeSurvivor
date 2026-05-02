using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UIScaleMotionTrack")]
public sealed class UIScaleMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionVector3ValueMode fromMode = UIMotionVector3ValueMode.Current;
    [SerializeField] private Vector3 fromValue = Vector3.one;
    [SerializeField] private UIMotionVector3ValueMode toMode = UIMotionVector3ValueMode.InitialMultiplied;
    [SerializeField] private Vector3 toValue = Vector3.one;

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

        Vector3 start = ResolveValue(fromMode, fromValue, target.localScale, snapshot.LocalScale);
        Vector3 end = ResolveValue(toMode, toValue, target.localScale, snapshot.LocalScale);
        target.localScale = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            target.localScale = end;
            return null;
        }

        return target.DOScale(end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetTarget(TargetKey, out Transform target)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        Vector3 current = target.localScale;
        Vector3 start = ResolveValue(fromMode, fromValue, current, snapshot.LocalScale);
        Vector3 end = ResolveValue(toMode, toValue, current, snapshot.LocalScale);
        target.localScale = Vector3.LerpUnclamped(start, end, normalizedTime);
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
}
