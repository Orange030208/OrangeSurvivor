
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public sealed class UIMoveMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionVector2ValueMode fromMode = UIMotionVector2ValueMode.Current;
    [SerializeField] private Vector2 fromValue;
    [SerializeField] private UIMotionVector2ValueMode toMode = UIMotionVector2ValueMode.InitialPlusOffset;
    [SerializeField] private Vector2 toValue;

    protected override Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetRectTransform(this, out RectTransform rectTransform))
        {
            LogMissingTarget(nameof(RectTransform));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        Vector2 start = ResolveValue(fromMode, fromValue, rectTransform.anchoredPosition, snapshot.AnchoredPosition);
        Vector2 end = ResolveValue(toMode, toValue, rectTransform.anchoredPosition, snapshot.AnchoredPosition);
        rectTransform.anchoredPosition = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            rectTransform.anchoredPosition = end;
            return null;
        }

        return DOTween.To(() => rectTransform.anchoredPosition, value => rectTransform.anchoredPosition = value, end, duration);
    }

    protected override void ApplySample(UIMotionTargetCache targets, float normalizedTime)
    {
        if (!targets.TryGetRectTransform(this, out RectTransform rectTransform)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        Vector2 current = rectTransform.anchoredPosition;
        Vector2 start = ResolveValue(fromMode, fromValue, current, snapshot.AnchoredPosition);
        Vector2 end = ResolveValue(toMode, toValue, current, snapshot.AnchoredPosition);
        rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, normalizedTime);
    }

    private static Vector2 ResolveValue(UIMotionVector2ValueMode mode, Vector2 customValue, Vector2 currentValue, Vector2 initialValue)
    {
        return mode switch
        {
            UIMotionVector2ValueMode.Initial => initialValue,
            UIMotionVector2ValueMode.Custom => customValue,
            UIMotionVector2ValueMode.InitialPlusOffset => initialValue + customValue,
            _ => currentValue
        };
    }
}
}
