using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class UIImageFillMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionFloatValueMode fromMode = UIMotionFloatValueMode.Current;
    [SerializeField] [Range(0f, 1f)] private float fromValue;
    [SerializeField] private UIMotionFloatValueMode toMode = UIMotionFloatValueMode.Custom;
    [SerializeField] [Range(0f, 1f)] private float toValue = 1f;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetImage(TargetKey, out Image image))
        {
            LogMissingTarget(nameof(Image));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        float start = ResolveValue(fromMode, fromValue, image.fillAmount, snapshot.ImageFillAmount);
        float end = ResolveValue(toMode, toValue, image.fillAmount, snapshot.ImageFillAmount);
        image.fillAmount = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            image.fillAmount = end;
            return null;
        }

        return image.DOFillAmount(end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetImage(TargetKey, out Image image)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        float start = ResolveValue(fromMode, fromValue, image.fillAmount, snapshot.ImageFillAmount);
        float end = ResolveValue(toMode, toValue, image.fillAmount, snapshot.ImageFillAmount);
        image.fillAmount = Mathf.LerpUnclamped(start, end, normalizedTime);
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
