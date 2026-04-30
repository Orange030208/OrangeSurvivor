using System;
using DG.Tweening;
using UnityEngine;

public enum UIMotionFloatValueMode
{
    Current,
    Initial,
    Custom
}

public enum UIMotionVector2ValueMode
{
    Current,
    Initial,
    Custom,
    InitialPlusOffset
}

public enum UIMotionVector3ValueMode
{
    Current,
    Initial,
    Custom,
    InitialPlusOffset,
    InitialMultiplied
}

public enum UIMotionColorValueMode
{
    Current,
    Initial,
    Custom
}

[Serializable]
public abstract class UIMotionTrackDefinition
{
    [SerializeField] private string targetKey = UIMotionTargetKeys.SELF;
    [SerializeField] [Min(0f)] private float startDelay;
    [SerializeField] [Min(0f)] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    public string TargetKey => string.IsNullOrWhiteSpace(targetKey) ? UIMotionTargetKeys.SELF : targetKey;
    public float StartDelay => Mathf.Max(0f, startDelay);
    public float Duration => Mathf.Max(0f, duration);
    public Ease Ease => ease;

    public Tween CreateTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (targets == null)
        {
            throw new System.ArgumentNullException(nameof(targets));
        }

        if (context.PlaybackMode == UIMotionPlaybackMode.SampleStart)
        {
            ApplySample(targets, 0f);
            return null;
        }

        if (context.PlaybackMode == UIMotionPlaybackMode.SampleEnd)
        {
            ApplySample(targets, 1f);
            return null;
        }

        Tween tween = CreateTrackTween(targets, context);
        if (tween == null)
        {
            ApplySample(targets, 1f);
            return null;
        }

        Sequence wrapper = DOTween.Sequence();
        if (StartDelay > 0f)
        {
            wrapper.AppendInterval(StartDelay);
        }

        wrapper.Append(tween.SetEase(Ease));
        return wrapper;
    }

    protected abstract Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context);
    protected abstract void ApplySample(UIMotionTargetRegistry targets, float normalizedTime);

    protected float ResolveDuration(UIMotionPlaybackContext context)
    {
        return Duration * Mathf.Max(0.01f, context.DurationScale);
    }

    protected bool TryGetSnapshot(UIMotionTargetRegistry targets, out UIMotionTargetSnapshot snapshot)
    {
        return targets.TryGetSnapshot(TargetKey, out snapshot);
    }

    protected void LogMissingTarget(string expectedComponent)
    {
        Debug.LogWarning($"{GetType().Name} could not play because target '{TargetKey}' is missing {expectedComponent}.");
    }
}
