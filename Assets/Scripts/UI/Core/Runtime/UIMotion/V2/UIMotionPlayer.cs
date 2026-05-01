using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
// UI 动画系统的运行时入口：负责把 UIMotionDefinition 中配置的 Clip 解析成 DOTween 播放序列。
// 这个组件只做“播放编排”和“目标解析”，具体属性如何变化由各个 UIMotionTrackDefinition 决定。
public sealed class UIMotionPlayer : MonoBehaviour, IUIRuntimeMotion, IUISequenceMotion
{
    [SerializeField] private UIMotionDefinition definition;
    // 目标注册表保存 Inspector 中配置的 key -> Transform 映射，并缓存初始快照。
    // Track 通过 key 查找目标，避免 Clip 直接依赖具体层级或硬编码对象引用。
    [SerializeField] private UIMotionTargetRegistry targets = new();
    // 打开对象时是否重新捕获默认状态。用于 UI 被外部逻辑调整后，再次启用时以当前状态作为 Initial。
    [SerializeField] private bool refreshDefaultsOnEnable = true;
    // 销毁时清理所有仍在播放的 Tween，避免回调继续访问已经销毁的 UI 对象。
    [SerializeField] private bool stopAllChannelsOnDestroy = true;

    // 以 Channel 为单位记录活跃 Tween，保证“显示/隐藏”“悬停/按下”等动画可以独立冲突处理。
    private readonly Dictionary<string, List<Tween>> activeTweensByChannel = new(StringComparer.Ordinal);
    private bool initialized;
    private bool defaultsCaptured;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        if (refreshDefaultsOnEnable && !defaultsCaptured)
        {
            RefreshDefaults();
        }
    }

    private void OnDestroy()
    {
        if (stopAllChannelsOnDestroy)
        {
            Kill();
        }
    }

    public Tween Play(string clipId, float delay = 0f)
    {
        InitializeIfNeeded();
        if (!TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip))
        {
            Debug.LogWarning($"{nameof(UIMotionPlayer)} '{name}' could not find clip '{clipId}'.", this);
            return null;
        }

        // 播放前先按 Clip 的冲突策略处理旧动画。默认只停同 Channel，避免按钮 hover 打断页面进出场。
        ApplyConflictPolicy(clip);
        UIMotionPlaybackContext context = new(this, clip, UIMotionPlaybackMode.PlayToEnd, delay, clip.DurationScale);
        Tween tween = BuildClipTween(clip, context);
        if (tween == null)
        {
            return null;
        }

        tween.SetUpdate(definition.UseUnscaledTime);
        RegisterChannelTween(clip.Channel, tween);
        return tween;
    }

    public IEnumerator PlayAndWait(string clipId, float delay = 0f)
    {
        Tween tween = Play(clipId, delay);
        if (tween != null && tween.IsActive())
        {
            yield return tween.WaitForCompletion();
        }
    }

    public void SetImmediate(string clipId, bool atEnd = true)
    {
        InitializeIfNeeded();
        if (!TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip))
        {
            Debug.LogWarning($"{nameof(UIMotionPlayer)} '{name}' could not find clip '{clipId}' for immediate sampling.", this);
            return;
        }

        // 立即采样用于页面入场前的隐藏态、或跳过动画时的完成态。
        // 这里不注册 Tween，因为 SampleStart/SampleEnd 只写入目标属性，不产生持续播放的序列。
        StopChannel(clip.Channel);
        UIMotionPlaybackMode mode = atEnd ? UIMotionPlaybackMode.SampleEnd : UIMotionPlaybackMode.SampleStart;
        UIMotionPlaybackContext context = new(this, clip, mode, 0f, clip.DurationScale);
        BuildClipTween(clip, context);
    }

    public void StopChannel(string channel)
    {
        string resolvedChannel = ResolveChannel(channel);
        if (!activeTweensByChannel.TryGetValue(resolvedChannel, out List<Tween> tweens))
        {
            return;
        }

        // 从后往前 Kill，配合 OnKill 回调移除注册项时不会影响尚未遍历的元素。
        for (int i = tweens.Count - 1; i >= 0; i--)
        {
            tweens[i]?.Kill();
        }

        tweens.Clear();
        activeTweensByChannel.Remove(resolvedChannel);
    }

    public void RefreshDefaults()
    {
        InitializeIfNeeded();
        // Initial 值来自快照而不是 Prefab 原始值。需要以当前布局作为新起点时，主动调用这里刷新。
        targets.RefreshSnapshots();
        defaultsCaptured = true;
    }

    public void Kill()
    {
        List<string> channels = new(activeTweensByChannel.Keys);
        for (int i = 0; i < channels.Count; i++)
        {
            StopChannel(channels[i]);
        }
    }

    public List<string> GetOptionList()
    {
        InitializeIfNeeded();
        return definition != null ? definition.GetClipIds() : new List<string>();
    }

    public void PrepareEnter()
    {
        SetImmediate(UIMotionClipIds.HIDDEN);
    }

    public Tween PlayEnter(float delay = 0f)
    {
        return Play(UIMotionClipIds.SHOW, delay);
    }

    public Tween PlayExit(float delay = 0f)
    {
        return Play(UIMotionClipIds.HIDE, delay);
    }

    public void SetHiddenImmediate()
    {
        SetImmediate(UIMotionClipIds.HIDDEN);
    }

    public void CompleteImmediate()
    {
        SetImmediate(UIMotionClipIds.VISIBLE);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        // 初始化会建立 SELF 目标、Inspector 绑定目标，以及首份默认快照。
        // 后续 Track 的 Initial/InitialPlusOffset 都依赖这份快照。
        targets.Initialize(transform);
        initialized = true;
        defaultsCaptured = true;
    }

    private bool TryGetClip(string clipId, out UIMotionClipDefinition clip)
    {
        clip = null;
        return definition != null && definition.TryGetClip(clipId, out clip);
    }

    private bool TryGetClipWithFallback(string clipId, out UIMotionClipDefinition clip)
    {
        if (TryGetClip(clipId, out clip))
        {
            return true;
        }

        // 常用状态 Clip 允许降级到对应动作 Clip，减少每个 Definition 必须重复配置的样板。
        // 例如没有 Visible 时可采样 Show 的末帧，没有 Hidden 时可采样 Hide 的末帧。
        string fallbackClipId = clipId switch
        {
            UIMotionClipIds.VISIBLE => UIMotionClipIds.SHOW,
            UIMotionClipIds.HIDDEN => UIMotionClipIds.HIDE,
            UIMotionClipIds.HOVER_OUT => UIMotionClipIds.RELEASE,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(fallbackClipId) && TryGetClip(fallbackClipId, out clip);
    }

    private Tween BuildClipTween(UIMotionClipDefinition clip, UIMotionPlaybackContext context)
    {
        IReadOnlyList<UIMotionTrackDefinition> tracks = clip.Tracks;
        if (tracks == null || tracks.Count == 0)
        {
            return null;
        }

        Sequence sequence = DOTween.Sequence();
        if (context.Delay > 0f && context.PlaybackMode == UIMotionPlaybackMode.PlayToEnd)
        {
            sequence.AppendInterval(context.Delay);
        }

        bool hasTween = false;
        for (int i = 0; i < tracks.Count; i++)
        {
            UIMotionTrackDefinition track = tracks[i];
            if (track == null)
            {
                continue;
            }

            Tween trackTween = track.CreateTween(targets, context);
            // Immediate 模式下 Track 已经在 CreateTween 内完成采样写值，不需要加入 DOTween Sequence。
            if (context.IsImmediate)
            {
                continue;
            }

            if (trackTween == null)
            {
                continue;
            }

            hasTween = true;
            // Clip 负责 Track 间的并行/顺序关系；Track 自己只关心单个目标属性的变化。
            if (clip.PlayMode == UIMotionClipPlayMode.Sequential)
            {
                sequence.Append(trackTween);
            }
            else
            {
                sequence.Join(trackTween);
            }
        }

        if (context.IsImmediate || !hasTween)
        {
            sequence.Kill();
            return null;
        }

        return sequence;
    }

    private void ApplyConflictPolicy(UIMotionClipDefinition clip)
    {
        // 冲突策略放在 Clip 上，便于同一个 Player 内同时支持“页面级动画互斥”和“交互反馈并行”。
        switch (clip.ConflictPolicy)
        {
            case UIMotionConflictPolicy.StopAllChannels:
                Kill();
                break;
            case UIMotionConflictPolicy.AllowParallel:
                break;
            default:
                StopChannel(clip.Channel);
                break;
        }
    }

    private void RegisterChannelTween(string channel, Tween tween)
    {
        string resolvedChannel = ResolveChannel(channel);
        if (!activeTweensByChannel.TryGetValue(resolvedChannel, out List<Tween> tweens))
        {
            tweens = new List<Tween>();
            activeTweensByChannel.Add(resolvedChannel, tweens);
        }

        tweens.Add(tween);
        // DOTween 完成和 Kill 都可能结束播放；两边都注销可覆盖自然完成、手动中断和对象销毁。
        tween.OnKill(() => RemoveChannelTween(resolvedChannel, tween));
        tween.OnComplete(() => RemoveChannelTween(resolvedChannel, tween));
    }

    private void RemoveChannelTween(string channel, Tween tween)
    {
        if (!activeTweensByChannel.TryGetValue(channel, out List<Tween> tweens))
        {
            return;
        }

        tweens.Remove(tween);
        if (tweens.Count == 0)
        {
            activeTweensByChannel.Remove(channel);
        }
    }

    private static string ResolveChannel(string channel)
    {
        return string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
    }
}
