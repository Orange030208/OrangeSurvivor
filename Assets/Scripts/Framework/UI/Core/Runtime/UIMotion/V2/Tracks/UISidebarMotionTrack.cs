using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using DG.Tweening;
using System;
using UnityEngine;

public enum UISidebarMotionPhase
{
    Show,
    Hide
}

[Serializable]
[MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "UISidebarMotionTrack")]
public sealed class UISidebarMotionTrack : UIMotionTrackDefinition
{
    [SerializeField] private UISidebarMotionPhase phase = UISidebarMotionPhase.Show;
    [SerializeField] private UISidebarEdgeDirection hiddenDirection = UISidebarEdgeDirection.Left;
    [SerializeField] [Min(0f)] private float extraHideOffset;
    [SerializeField] private bool fade;
    [SerializeField] [Range(0f, 1f)] private float hiddenAlpha;
    [SerializeField] private bool useEnterOvershoot = true;
    [SerializeField] [Min(0f)] private float enterOvershootDistance = 36f;
    [SerializeField] [Range(0f, 1f)] private float enterOvershootDurationRatio = 0.78f;
    [SerializeField] private Ease enterOvershootEase = Ease.OutCubic;
    [SerializeField] private Ease enterSettleEase = Ease.OutCubic;

    protected override Tween CreateTrackTween(UIMotionTargetRegistry targets, UIMotionPlaybackContext context)
    {
        if (!targets.TryGetRectTransform(TargetKey, out RectTransform rectTransform))
        {
            LogMissingTarget(nameof(RectTransform));
            return null;
        }

        if (!TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return null;
        }

        CanvasGroup canvasGroup = snapshot.CanvasGroup;
        float duration = ResolveDuration(context);
        Vector2 visiblePosition = snapshot.AnchoredPosition;
        Vector2 hiddenPosition = visiblePosition + GetHiddenOffset(rectTransform);

        if (Mathf.Approximately(duration, 0f))
        {
            ApplyState(rectTransform, canvasGroup, phase == UISidebarMotionPhase.Hide ? hiddenPosition : visiblePosition);
            return null;
        }

        if (phase == UISidebarMotionPhase.Hide)
        {
            return CreateHideTween(rectTransform, canvasGroup, hiddenPosition, duration);
        }

        return CreateShowTween(rectTransform, canvasGroup, visiblePosition, duration);
    }

    protected override void ApplySample(UIMotionTargetRegistry targets, float normalizedTime)
    {
        if (!targets.TryGetRectTransform(TargetKey, out RectTransform rectTransform)
            || !TryGetSnapshot(targets, out UIMotionTargetSnapshot snapshot))
        {
            return;
        }

        CanvasGroup canvasGroup = snapshot.CanvasGroup;
        Vector2 visiblePosition = snapshot.AnchoredPosition;
        Vector2 hiddenPosition = visiblePosition + GetHiddenOffset(rectTransform);
        Vector2 start = phase == UISidebarMotionPhase.Hide ? visiblePosition : hiddenPosition;
        Vector2 end = phase == UISidebarMotionPhase.Hide ? hiddenPosition : visiblePosition;
        rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, normalizedTime);

        if (canvasGroup != null)
        {
            float visibleAlpha = 1f;
            float startAlpha = phase == UISidebarMotionPhase.Hide && fade ? visibleAlpha : hiddenAlpha;
            float endAlpha = phase == UISidebarMotionPhase.Hide && fade ? hiddenAlpha : visibleAlpha;
            canvasGroup.alpha = fade ? Mathf.LerpUnclamped(startAlpha, endAlpha, normalizedTime) : visibleAlpha;
        }
    }

    private Tween CreateShowTween(RectTransform rectTransform, CanvasGroup canvasGroup, Vector2 visiblePosition, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        Vector2 hiddenPosition = visiblePosition + GetHiddenOffset(rectTransform);
        rectTransform.anchoredPosition = hiddenPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = fade ? hiddenAlpha : 1f;
        }

        if (!useEnterOvershoot || enterOvershootDistance <= 0f)
        {
            sequence.Join(rectTransform.DOAnchorPos(visiblePosition, duration).SetEase(Ease));
            JoinFade(sequence, canvasGroup, 1f, duration, Ease);
            return sequence;
        }

        float overshootDuration = duration * Mathf.Clamp01(enterOvershootDurationRatio);
        float settleDuration = Mathf.Max(0f, duration - overshootDuration);
        Vector2 overshootPosition = visiblePosition + GetEnterOvershootOffset();

        sequence.Join(rectTransform.DOAnchorPos(overshootPosition, overshootDuration).SetEase(enterOvershootEase));
        JoinFade(sequence, canvasGroup, 1f, overshootDuration, enterOvershootEase);

        if (settleDuration > 0f)
        {
            sequence.Append(rectTransform.DOAnchorPos(visiblePosition, settleDuration).SetEase(enterSettleEase));
        }
        else
        {
            sequence.AppendCallback(() => rectTransform.anchoredPosition = visiblePosition);
        }

        return sequence;
    }

    private Tween CreateHideTween(RectTransform rectTransform, CanvasGroup canvasGroup, Vector2 hiddenPosition, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(rectTransform.DOAnchorPos(hiddenPosition, duration).SetEase(Ease));
        JoinFade(sequence, canvasGroup, fade ? hiddenAlpha : 1f, duration, Ease);
        return sequence;
    }

    private void ApplyState(RectTransform rectTransform, CanvasGroup canvasGroup, Vector2 position)
    {
        rectTransform.anchoredPosition = position;
        if (canvasGroup != null)
        {
            bool hidden = phase == UISidebarMotionPhase.Hide;
            canvasGroup.alpha = hidden && fade ? hiddenAlpha : 1f;
        }
    }

    private void JoinFade(Sequence sequence, CanvasGroup canvasGroup, float alpha, float duration, Ease ease)
    {
        if (canvasGroup == null)
        {
            return;
        }

        sequence.Join(canvasGroup.DOFade(alpha, duration).SetEase(ease));
    }

    private Vector2 GetHiddenOffset(RectTransform rectTransform)
    {
        float width = rectTransform.rect.width > 0f ? rectTransform.rect.width : Mathf.Abs(rectTransform.sizeDelta.x);
        float height = rectTransform.rect.height > 0f ? rectTransform.rect.height : Mathf.Abs(rectTransform.sizeDelta.y);
        float distance = ((hiddenDirection == UISidebarEdgeDirection.Left || hiddenDirection == UISidebarEdgeDirection.Right) ? width : height) + extraHideOffset;
        return GetHiddenDirectionVector() * distance;
    }

    private Vector2 GetHiddenDirectionVector()
    {
        return hiddenDirection switch
        {
            UISidebarEdgeDirection.Right => Vector2.right,
            UISidebarEdgeDirection.Top => Vector2.up,
            UISidebarEdgeDirection.Bottom => Vector2.down,
            _ => Vector2.left
        };
    }

    private Vector2 GetEnterOvershootOffset()
    {
        return hiddenDirection switch
        {
            UISidebarEdgeDirection.Right => Vector2.left * enterOvershootDistance,
            UISidebarEdgeDirection.Top => Vector2.down * enterOvershootDistance,
            UISidebarEdgeDirection.Bottom => Vector2.up * enterOvershootDistance,
            _ => Vector2.right * enterOvershootDistance
        };
    }
}
}
