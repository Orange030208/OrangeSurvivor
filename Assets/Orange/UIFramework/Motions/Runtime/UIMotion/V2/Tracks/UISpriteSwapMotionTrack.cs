
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class UISpriteSwapMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private bool restoreInitialOnStart;
    [SerializeField] private bool setNativeSizeIfEmpty;

    protected override Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        return DOVirtual.DelayedCall(0f, () => ApplySprite(targets, useInitial: false));
    }

    protected override void ApplySample(UIMotionTargetCache targets, float normalizedTime)
    {
        ApplySprite(targets, useInitial: normalizedTime <= 0f && restoreInitialOnStart);
    }

    private void ApplySprite(UIMotionTargetCache targets, bool useInitial)
    {
        if (!targets.TryGetImage(this, out Image image))
        {
            LogMissingTarget(nameof(Image));
            return;
        }

        Sprite targetSprite = sprite;
        if (useInitial && TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            targetSprite = snapshot.Sprite;
        }

        image.sprite = targetSprite;
        if (setNativeSizeIfEmpty && image.rectTransform.rect.width <= 0f && image.rectTransform.rect.height <= 0f)
        {
            image.SetNativeSize();
        }
    }
}
}
