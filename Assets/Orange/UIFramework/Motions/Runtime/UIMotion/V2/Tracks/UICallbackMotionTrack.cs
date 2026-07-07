
namespace Orange.UIFramework
{
    using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class UICallbackMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UnityEvent callback;

    protected override Tween CreateTrackTween(UIMotionTargetCache targets, UIMotionPlaybackContext context)
    {
        return DOVirtual.DelayedCall(0f, () => callback?.Invoke());
    }

    protected override void ApplySample(UIMotionTargetCache targets, float normalizedTime)
    {
        if (normalizedTime >= 1f)
        {
            callback?.Invoke();
        }
    }
}
}
