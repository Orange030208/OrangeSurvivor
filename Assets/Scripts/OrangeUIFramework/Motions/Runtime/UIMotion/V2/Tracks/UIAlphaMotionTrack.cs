
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public sealed class UIAlphaMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionFloatValueMode fromMode = UIMotionFloatValueMode.Current;
    [SerializeField] [Range(0f, 1f)] private float fromValue;
    [SerializeField] private UIMotionFloatValueMode toMode = UIMotionFloatValueMode.Custom;
    [SerializeField] [Range(0f, 1f)] private float toValue = 1f;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetCanvasGroup(TargetKey, out CanvasGroup canvasGroup))
        {
            LogMissingTarget(nameof(CanvasGroup));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        float start = ResolveValue(fromMode, fromValue, canvasGroup.alpha, snapshot.CanvasAlpha);
        float end = ResolveValue(toMode, toValue, canvasGroup.alpha, snapshot.CanvasAlpha);
        canvasGroup.alpha = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            canvasGroup.alpha = end;
            return null;
        }

        return canvasGroup.DOFade(end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetCanvasGroup(TargetKey, out CanvasGroup canvasGroup)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        float current = canvasGroup.alpha;
        float start = ResolveValue(fromMode, fromValue, current, snapshot.CanvasAlpha);
        float end = ResolveValue(toMode, toValue, current, snapshot.CanvasAlpha);
        canvasGroup.alpha = Mathf.LerpUnclamped(start, end, normalizedTime);
    }

    private static float ResolveValue(UIMotionFloatValueMode mode, float customValue, float currentValue, float initialValue)
    {
        return mode switch
        {
            UIMotionFloatValueMode.Initial => initialValue,
            UIMotionFloatValueMode.Custom => customValue,
            _ => currentValue
        };
    }
}
}
