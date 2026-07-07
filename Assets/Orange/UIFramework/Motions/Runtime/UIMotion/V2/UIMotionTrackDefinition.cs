
namespace Orange.UIFramework
{
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
// Track 是最小动画单元：只负责一个目标上的一种属性变化。
// 新增 UI 动画类型时优先继承这个类，而不是把分支堆到 UIMotionPlayer 中。
public abstract class UIMotionTrackDefinition
{
    // 为空时默认作用于挂载 UIMotionPlayer 的 Transform；需要动画子节点时直接拖拽目标。
    [SerializeField] private Transform target;
    [SerializeField] [Min(0f)] private float startDelay;
    [SerializeField] [Min(0f)] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    public Transform Target => target;
    public float StartDelay => Mathf.Max(0f, startDelay);
    public float Duration => Mathf.Max(0f, duration);
    public Ease Ease => ease;

    public Transform ResolveTarget(Transform owner)
    {
        return target != null ? target : owner;
    }

    public Tween CreateTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        if (targets == null)
        {
            throw new System.ArgumentNullException(nameof(targets));
        }

        // 采样模式用于“直接设置到起点/终点”，不创建 Tween，避免立即状态也进入播放队列。
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
            // 零时长或不可 Tween 的 Track 仍应落到终点，保持 Play 和 SampleEnd 的视觉结果一致。
            ApplySample(targets, 1f);
            return null;
        }

        tween.SetEase(Ease);
        if (StartDelay <= 0f)
        {
            return tween;
        }

        // 只有配置了 Track 自身延迟时才需要 wrapper，避免零延迟采样/播放额外占用 Sequence 容量。
        Sequence wrapper = DOTween.Sequence();
        wrapper.AppendInterval(StartDelay);
        wrapper.Append(tween);
        return wrapper;
    }

    protected abstract Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context);
    protected abstract void ApplySample(UIMotionTargetCache targets, float normalizedTime);

    protected float ResolveDuration(UIMotionPlaybackContext context)
    {
        // Clip 的 DurationScale 是整体节奏控制，单个 Track 的 duration 保持 Inspector 中的局部语义。
        return Duration * Mathf.Max(0.01f, context.DurationScale);
    }

    protected bool TryGetSnapshot(UIMotionTargetCache targets, out UIMotionTargetSnapshot snapshot)
    {
        return targets.TryGetSnapshot(this, out snapshot);
    }

    protected void LogMissingTarget(string expectedComponent)
    {
        Debug.LogWarning($"{GetType().Name} could not play because the resolved target is missing {expectedComponent}.");
    }
}
}
