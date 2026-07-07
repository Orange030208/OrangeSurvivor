
namespace Orange.UIFramework
{
    using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public sealed class UISpriteSequenceMotionTrack : UIMotionTrackDefinition
{
    private enum SequenceTimingMode
    {
        FramesPerSecond,
        TotalDuration
    }

        [Serializable]
    private sealed class FrameEvent
    {
        [Min(0)] public int frameIndex;
        public UnityEvent callback;
    }

    [SerializeField] private List<Sprite> frames = new();
    [SerializeField] private bool reverse;
    [SerializeField] private bool keepLastFrameOnComplete = true;
    [SerializeField] private bool setNativeSizeIfEmpty;
    [SerializeField] private SequenceTimingMode timingMode = SequenceTimingMode.FramesPerSecond;
    [SerializeField] [Min(1f)] private float framesPerSecond = 12f;
    [SerializeField] private List<FrameEvent> frameEvents = new();

    protected override Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetImage(this, out Image image))
        {
            LogMissingTarget(nameof(Image));
            return null;
        }

        if (frames == null || frames.Count == 0)
        {
            return null;
        }

        Sequence sequence = DOTween.Sequence();
        List<int> order = BuildPlayOrder();
        float frameDuration = GetFrameDuration(order.Count, context);

        for (int i = 0; i < order.Count; i++)
        {
            int frameIndex = order[i];
            sequence.AppendCallback(() =>
            {
                SetFrame(image, frameIndex);
                InvokeFrameEvents(frameIndex);
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
                SetFrame(image, GetCompletedFrameIndex());
            }
        });

        return sequence;
    }

    protected override void ApplySample(UIMotionTargetCache targets, float normalizedTime)
    {
        if (!targets.TryGetImage(this, out Image image) || frames == null || frames.Count == 0)
        {
            return;
        }

        int index = Mathf.Clamp(Mathf.RoundToInt((frames.Count - 1) * normalizedTime), 0, frames.Count - 1);
        if (reverse)
        {
            index = frames.Count - 1 - index;
        }

        SetFrame(image, index);
    }

    private List<int> BuildPlayOrder()
    {
        List<int> order = new(frames.Count);
        if (reverse)
        {
            for (int i = frames.Count - 1; i >= 0; i--)
            {
                order.Add(i);
            }

            return order;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            order.Add(i);
        }

        return order;
    }

    private float GetFrameDuration(int frameCount, UIMotionPlaybackContext context)
    {
        if (frameCount <= 0)
        {
            return 0f;
        }

        if (timingMode == SequenceTimingMode.TotalDuration)
        {
            return ResolveDuration(context) / frameCount;
        }

        return 1f / Mathf.Max(1f, framesPerSecond);
    }

    private int GetCompletedFrameIndex()
    {
        return reverse ? 0 : frames.Count - 1;
    }

    private void SetFrame(Image image, int frameIndex)
    {
        image.enabled = true;
        image.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Count - 1)];
        if (setNativeSizeIfEmpty && image.rectTransform.rect.width <= 0f && image.rectTransform.rect.height <= 0f)
        {
            image.SetNativeSize();
        }
    }

    private void InvokeFrameEvents(int frameIndex)
    {
        if (frameEvents == null)
        {
            return;
        }

        for (int i = 0; i < frameEvents.Count; i++)
        {
            FrameEvent frameEvent = frameEvents[i];
            if (frameEvent != null && frameEvent.frameIndex == frameIndex)
            {
                frameEvent.callback?.Invoke();
            }
        }
    }
}
}
