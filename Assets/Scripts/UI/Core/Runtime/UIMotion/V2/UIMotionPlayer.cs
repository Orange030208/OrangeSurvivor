using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIMotionPlayer : MonoBehaviour, IUIRuntimeMotion, IUISequenceMotion, IStringConfig
{
    [SerializeField] private UIMotionDefinition definition;
    [SerializeField] private UIMotionTargetRegistry targets = new();
    [SerializeField] private string currentConfigOption = string.Empty;
    [SerializeField] private bool refreshDefaultsOnEnable = true;
    [SerializeField] private bool stopAllChannelsOnDestroy = true;

    private readonly Dictionary<string, List<Tween>> activeTweensByChannel = new(StringComparer.Ordinal);
    private bool initialized;
    private bool defaultsCaptured;

    public UIMotionDefinition Definition => definition;
    public string CurrentConfigOption => currentConfigOption;

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

    public void SetImmediate(string clipId, bool atEnd = true)
    {
        InitializeIfNeeded();
        if (!TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip))
        {
            Debug.LogWarning($"{nameof(UIMotionPlayer)} '{name}' could not find clip '{clipId}' for immediate sampling.", this);
            return;
        }

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

    public void ApplyConfigByString(string selectedOption)
    {
        SetCurrentConfigOption(selectedOption);
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
            if (context.IsImmediate)
            {
                continue;
            }

            if (trackTween == null)
            {
                continue;
            }

            hasTween = true;
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

    private void SetCurrentConfigOption(string selectedOption)
    {
        currentConfigOption = selectedOption ?? string.Empty;
    }
}
