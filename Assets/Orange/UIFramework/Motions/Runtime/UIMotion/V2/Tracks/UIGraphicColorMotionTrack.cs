
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class UIGraphicColorMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UIMotionColorValueMode fromMode = UIMotionColorValueMode.Current;
    [SerializeField] private Color fromValue = Color.white;
    [SerializeField] private UIMotionColorValueMode toMode = UIMotionColorValueMode.Custom;
    [SerializeField] private Color toValue = Color.white;

    protected override Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetGraphic(this, out Graphic graphic))
        {
            LogMissingTarget(nameof(Graphic));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        Color start = ResolveValue(fromMode, fromValue, graphic.color, snapshot.GraphicColor);
        Color end = ResolveValue(toMode, toValue, graphic.color, snapshot.GraphicColor);
        graphic.color = start;

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            graphic.color = end;
            return null;
        }

        return DOTween.To(() => graphic.color, value => graphic.color = value, end, duration);
    }

    protected override void ApplySample(UIMotionTargetCache targets, float normalizedTime)
    {
        if (!targets.TryGetGraphic(this, out Graphic graphic)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        Color start = ResolveValue(fromMode, fromValue, graphic.color, snapshot.GraphicColor);
        Color end = ResolveValue(toMode, toValue, graphic.color, snapshot.GraphicColor);
        graphic.color = Color.LerpUnclamped(start, end, normalizedTime);
    }

    private static Color ResolveValue(UIMotionColorValueMode mode, Color customValue, Color currentValue, Color initialValue)
    {
        return mode switch
        {
            UIMotionColorValueMode.Initial => initialValue,
            UIMotionColorValueMode.Custom => customValue,
            _ => currentValue
        };
    }
}
}
