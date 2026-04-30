using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class UICallbackMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UnityEvent callback;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        return DOVirtual.DelayedCall(0f, () => callback?.Invoke());
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (normalizedTime >= 1f)
        {
            callback?.Invoke();
        }
    }
}
