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

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        return DOVirtual.DelayedCall(0f, () => ApplySprite(targets, useInitial: false));
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        ApplySprite(targets, useInitial: normalizedTime <= 0f && restoreInitialOnStart);
    }

    private void ApplySprite(UIMotionTargetRegistry targets, bool useInitial)
    {
        if (!targets.TryGetImage(TargetKey, out Image image))
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
