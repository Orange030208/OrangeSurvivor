using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Sprite 序列版 UI motion：直接播放一组图片帧，不依赖 Animator。
/// `Show` 用于展示/入场播放，默认可复用 `Common` 帧资源但保留独立覆盖入口；`Common` 用于回到稳定常态，`Hide` 用于隐藏/退场。
/// 通过把 `UIMotionAction` 映射到 Sprite 列表，实现与 `UISequenceDirector` 的统一编排。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UISpriteSequenceMotion : UIRuntimeMotionBase, IUISequenceMotion
{
    private enum SequenceTimingMode
    {
        FramesPerSecond,
        TotalDuration
    }

    private enum FrameSourceMode
    {
        Self,
        UseOtherActionFrames
    }

    [Serializable]
    private class FrameEvent
    {
        [Min(0)] public int frameIndex;
        public UnityEvent callback;
    }

    [Serializable]
    private class SpriteSequenceClip
    {
        public UIMotionAction action;
        public FrameSourceMode frameSourceMode = FrameSourceMode.Self;
        public UIMotionAction sourceAction = UIMotionAction.Show;
        public List<Sprite> frames = new();
        public SequenceTimingMode timingMode = SequenceTimingMode.FramesPerSecond;
        [Min(1f)] public float framesPerSecond = 12f;
        [Min(0.01f)] public float totalDuration = 0.5f;
        public bool reverse;
        public bool deactivateOnComplete;
        public List<FrameEvent> frameEvents = new();
    }

    [SerializeField] private Image targetImage;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool keepLastFrameOnComplete = true;
    [SerializeField] private List<SpriteSequenceClip> actionClips = new()
    {
        new SpriteSequenceClip { action = UIMotionAction.Common, timingMode = SequenceTimingMode.FramesPerSecond, framesPerSecond = 12f, totalDuration = 0.5f },
        new SpriteSequenceClip { action = UIMotionAction.Show, frameSourceMode = FrameSourceMode.UseOtherActionFrames, sourceAction = UIMotionAction.Common, timingMode = SequenceTimingMode.FramesPerSecond, framesPerSecond = 12f, totalDuration = 0.5f },
        new SpriteSequenceClip { action = UIMotionAction.Hide, frameSourceMode = FrameSourceMode.UseOtherActionFrames, sourceAction = UIMotionAction.Common, timingMode = SequenceTimingMode.FramesPerSecond, framesPerSecond = 12f, totalDuration = 0.5f, reverse = true }
    };

    private readonly Dictionary<UIMotionAction, SpriteSequenceClip> clipMap = new();
    private Tween currentTween;

    private void Awake()
    {
        targetImage ??= GetComponent<Image>();
        RebuildClipMap();
    }

    private void OnValidate()
    {
        targetImage ??= GetComponent<Image>();
        RebuildClipMap();
    }

    private void OnDestroy()
    {
        Kill();
    }

    public void PrepareEnter()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    public Tween PlayEnter(float delay = 0f)
    {
        return Play(UIMotionAction.Show, delay);
    }

    public Tween PlayExit(float delay = 0f)
    {
        return Play(UIMotionAction.Hide, delay);
    }

    public void SetHiddenImmediate()
    {
        SetImmediate(UIMotionAction.Hide);
    }

    public void CompleteImmediate()
    {
        SampleImmediate(UIMotionAction.Common, useLastFrame: true);
    }

    public override bool SupportsAction(UIMotionAction action)
    {
        SpriteSequenceClip clip = GetClip(action);
        return HasFrames(ResolveFrames(clip));
    }

    public override Tween Play(UIMotionAction action, float delay = 0f)
    {
        if (!SupportsAction(action))
        {
            return null;
        }

        SpriteSequenceClip clip = GetClip(action);
        IReadOnlyList<Sprite> frames = ResolveFrames(clip);
        if (!HasFrames(frames))
        {
            return null;
        }

        Kill();
        gameObject.SetActive(true);
        targetImage.enabled = true;

        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
        if (delay > 0f)
        {
            sequence.AppendInterval(delay);
        }

        List<int> playOrder = BuildPlayOrder(clip, frames.Count);
        float frameDuration = GetFrameDuration(clip, playOrder.Count);
        for (int i = 0; i < playOrder.Count; i++)
        {
            int frameIndex = playOrder[i];
            sequence.AppendCallback(() =>
            {
                SetFrame(frames, frameIndex);
                InvokeFrameEvents(clip, frameIndex);
            });

            if (frameDuration > 0f)
            {
                sequence.AppendInterval(frameDuration);
            }
        }

        sequence.OnComplete(() =>
        {
            if (keepLastFrameOnComplete)
            {
                SetFrame(frames, GetCompletedFrameIndex(clip, frames.Count));
            }

            if (clip.deactivateOnComplete)
            {
                gameObject.SetActive(false);
            }
        });

        currentTween = sequence;
        return sequence;
    }

    public override void SetImmediate(UIMotionAction action)
    {
        SampleImmediate(action, useLastFrame: true);
    }

    public override void RefreshDefaults()
    {
        targetImage ??= GetComponent<Image>();
        RebuildClipMap();
    }

    public override void Kill()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    private void SampleImmediate(UIMotionAction action, bool useLastFrame)
    {
        SpriteSequenceClip clip = GetClip(action);
        IReadOnlyList<Sprite> frames = ResolveFrames(clip);
        if (!HasFrames(frames))
        {
            return;
        }

        gameObject.SetActive(true);
        targetImage.enabled = true;
        int frameIndex = useLastFrame ? GetCompletedFrameIndex(clip, frames.Count) : GetStartFrameIndex(clip, frames.Count);
        SetFrame(frames, frameIndex);
        if (clip.deactivateOnComplete && useLastFrame)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetFrame(IReadOnlyList<Sprite> frames, int frameIndex)
    {
        targetImage.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Count - 1)];
        SetNativeSizeIfNeeded();
    }

    private void InvokeFrameEvents(SpriteSequenceClip clip, int frameIndex)
    {
        if (clip.frameEvents == null)
        {
            return;
        }

        foreach (FrameEvent frameEvent in clip.frameEvents)
        {
            if (frameEvent != null && frameEvent.frameIndex == frameIndex)
            {
                frameEvent.callback?.Invoke();
            }
        }
    }

    private float GetFrameDuration(SpriteSequenceClip clip, int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0f;
        }

        if (clip.timingMode == SequenceTimingMode.TotalDuration)
        {
            return clip.totalDuration / frameCount;
        }

        return 1f / Mathf.Max(1f, clip.framesPerSecond);
    }

    private List<int> BuildPlayOrder(SpriteSequenceClip clip, int frameCount)
    {
        List<int> order = new(frameCount);
        if (clip.reverse)
        {
            for (int i = frameCount - 1; i >= 0; i--)
            {
                order.Add(i);
            }

            return order;
        }

        for (int i = 0; i < frameCount; i++)
        {
            order.Add(i);
        }

        return order;
    }

    private int GetStartFrameIndex(SpriteSequenceClip clip, int frameCount)
    {
        return clip.reverse ? frameCount - 1 : 0;
    }

    private int GetCompletedFrameIndex(SpriteSequenceClip clip, int frameCount)
    {
        return clip.reverse ? 0 : frameCount - 1;
    }

    private bool HasFrames(IReadOnlyList<Sprite> frames)
    {
        return frames != null && frames.Count > 0;
    }

    private IReadOnlyList<Sprite> ResolveFrames(SpriteSequenceClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        if (clip.frameSourceMode == FrameSourceMode.UseOtherActionFrames)
        {
            SpriteSequenceClip sourceClip = GetClip(clip.sourceAction);
            if (sourceClip == null || ReferenceEquals(sourceClip, clip))
            {
                return null;
            }

            return sourceClip.frames;
        }

        return clip.frames;
    }

    private void SetNativeSizeIfNeeded()
    {
        RectTransform rectTransform = targetImage.rectTransform;
        if (rectTransform.rect.width > 0f && rectTransform.rect.height > 0f)
        {
            return;
        }

        targetImage.SetNativeSize();
    }

    private SpriteSequenceClip GetClip(UIMotionAction action)
    {
        if (clipMap.TryGetValue(action, out SpriteSequenceClip clip))
        {
            return clip;
        }

        return null;
    }

    private void RebuildClipMap()
    {
        clipMap.Clear();
        foreach (SpriteSequenceClip clip in actionClips)
        {
            if (clip != null && !clipMap.ContainsKey(clip.action))
            {
                clipMap.Add(clip.action, clip);
            }
        }
    }
}
