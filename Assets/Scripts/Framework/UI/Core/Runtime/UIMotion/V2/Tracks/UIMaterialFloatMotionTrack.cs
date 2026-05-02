using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UIMaterialFloatMotionTrack")]
public sealed class UIMaterialFloatMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private string propertyName = "_Amount";
    [SerializeField] private UIMotionFloatValueMode fromMode = UIMotionFloatValueMode.Current;
    [SerializeField] private float fromValue;
    [SerializeField] private UIMotionFloatValueMode toMode = UIMotionFloatValueMode.Custom;
    [SerializeField] private float toValue = 1f;
    [SerializeField] private bool instantiateMaterial = true;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!TryGetMaterial(targets, out Material material))
        {
            return null;
        }

        float initialValue = material.HasProperty(propertyName) ? material.GetFloat(propertyName) : 0f;
        float start = ResolveValue(fromMode, fromValue, initialValue, initialValue);
        float end = ResolveValue(toMode, toValue, initialValue, initialValue);
        material.SetFloat(propertyName, start);

        float duration = ResolveDuration(context);
        if (Mathf.Approximately(duration, 0f))
        {
            material.SetFloat(propertyName, end);
            return null;
        }

        float current = start;
        return DOTween.To(() => current, value =>
            {
                current = value;
                material.SetFloat(propertyName, value);
            }, end, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!TryGetMaterial(targets, out Material material))
        {
            return;
        }

        float initialValue = material.HasProperty(propertyName) ? material.GetFloat(propertyName) : 0f;
        float start = ResolveValue(fromMode, fromValue, initialValue, initialValue);
        float end = ResolveValue(toMode, toValue, initialValue, initialValue);
        material.SetFloat(propertyName, Mathf.LerpUnclamped(start, end, normalizedTime));
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
