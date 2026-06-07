using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class PulseBreathingMotion : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 0.3f;
    [SerializeField, Min(0.01f)] private float scaleHalfCycleDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float floatHalfCycleDuration = 1.9f;

    [Header("Breathing")]
    [SerializeField, Min(0.001f)] private float scaleMultiplier = 1.035f;

    [Header("Float")]
    [SerializeField, Min(0f)] private float floatAmplitudeY = 10f;

    [Header("Update")]
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Tween delayTween;
    private Tween scaleTween;
    private Tween floatTween;
    private bool restCaptured;
    private Vector2 restAnchoredPosition;
    private Vector3 restLocalScale;

    private void Awake()
    {
        ResolveRectTransform();
        CaptureRestPoseIfNeeded();
    }

    private void OnEnable()
    {
        ResolveRectTransform();
        CaptureRestPoseIfNeeded();
        StopMotion(restorePose: false);
        ScheduleMotion();
    }

    private void OnDisable()
    {
        StopMotion(restorePose: true);
    }

    private void OnDestroy()
    {
        StopMotion(restorePose: false);
    }

    private void ResolveRectTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    private void CaptureRestPoseIfNeeded()
    {
        if (restCaptured || rectTransform == null)
        {
            return;
        }

        restAnchoredPosition = rectTransform.anchoredPosition;
        restLocalScale = rectTransform.localScale;
        restCaptured = true;
    }

    private void ScheduleMotion()
    {
        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        if (startDelay <= 0f)
        {
            StartMotion();
            return;
        }

        delayTween = DOVirtual.DelayedCall(startDelay, StartMotion).SetUpdate(useUnscaledTime);
    }

    private void StartMotion()
    {
        delayTween = null;
        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        RestoreRestPose();

        Vector3 targetScale = restLocalScale * scaleMultiplier;
        Vector2 targetPosition = restAnchoredPosition + Vector2.up * floatAmplitudeY;

        scaleTween = DOTween.Sequence()
            .Append(rectTransform.DOScale(targetScale, scaleHalfCycleDuration).SetEase(Ease.InOutSine))
            .Append(rectTransform.DOScale(restLocalScale, scaleHalfCycleDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(useUnscaledTime);

        floatTween = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(targetPosition, floatHalfCycleDuration).SetEase(Ease.InOutSine))
            .Append(rectTransform.DOAnchorPos(restAnchoredPosition, floatHalfCycleDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(useUnscaledTime);
    }

    private void StopMotion(bool restorePose)
    {
        if (delayTween != null)
        {
            delayTween.Kill();
            delayTween = null;
        }

        if (scaleTween != null)
        {
            scaleTween.Kill();
            scaleTween = null;
        }

        if (floatTween != null)
        {
            floatTween.Kill();
            floatTween = null;
        }

        if (restorePose)
        {
            RestoreRestPose();
        }
    }

    private void RestoreRestPose()
    {
        if (!restCaptured || rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = restAnchoredPosition;
        rectTransform.localScale = restLocalScale;
    }
}
