
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class UIMaterialColorMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private string propertyName = "_Color";
    [SerializeField] private UIMotionColorValueMode fromMode = UIMotionColorValueMode.Current;
    [SerializeField] private Color fromValue = Color.white;
    [SerializeField] private UIMotionColorValueMode toMode = UIMotionColorValueMode.Custom;
    [SerializeField] private Color toValue = Color.white;
    [SerializeField] private bool instantiateMaterial = true;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!TryGetMaterial(targets, out Material material))
        {
            return null;
        }

        Color initialValue = material.GetColor(propertyName);
        Color start = ResolveValue(fromMode, fromValue, initialValue, initialValue);
        Color end = ResolveValue(toMode, toValue, initialValue, initialValue);
        material.SetColor(propertyName, start);

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            material.SetColor(propertyName, end);
            return null;
        }

        Color current = start;
        return DOTween.To(() => current, value =>
            {
                current = value;
                material.SetColor(propertyName, value);
            }, end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!TryGetMaterial(targets, out Material material))
        {
            return;
        }

        Color initialValue = material.GetColor(propertyName);
        Color start = ResolveValue(fromMode, fromValue, initialValue, initialValue);
        Color end = ResolveValue(toMode, toValue, initialValue, initialValue);
        material.SetColor(propertyName, Color.LerpUnclamped(start, end, normalizedTime));
    }

    private bool TryGetMaterial(UIMotionTargetRegistry targets, out Material material)
    {
        material = null;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            Debug.LogWarning($"{GetType().Name} requires a material property name.");
            return false;
        }

        if (!targets.TryGetGraphic(TargetKey, out Graphic graphic))
        {
            LogMissingTarget(nameof(Graphic));
            return false;
        }

        material = ResolveMaterialInstance(graphic);
        if (material == null || !material.HasProperty(propertyName))
        {
            Debug.LogWarning($"{GetType().Name} could not find material property '{propertyName}' on target '{TargetKey}'.");
            return false;
        }

        return true;
    }

    private Material ResolveMaterialInstance(Graphic graphic)
    {
        Material source = graphic.material != null ? graphic.material : graphic.materialForRendering;
        if (!instantiateMaterial || source == null)
        {
            return source;
        }

        if (graphic.material != null && graphic.material.name.EndsWith(" (Motion Instance)", System.StringComparison.Ordinal))
        {
            return graphic.material;
        }

        Material instance = new Material(source)
        {
            name = $"{source.name} (Motion Instance)"
        };
        graphic.material = instance;
        return instance;
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
