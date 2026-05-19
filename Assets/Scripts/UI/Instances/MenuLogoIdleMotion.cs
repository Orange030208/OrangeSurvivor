using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MenuLogoIdleMotion : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 0.45f;
    [SerializeField, Min(0.01f)] private float scaleHalfCycleDuration = 2.05f;
    [SerializeField, Min(0.01f)] private float floatHalfCycleDuration = 2.35f;
    [SerializeField, Min(0.01f)] private float rotationHalfCycleDuration = 2.7f;

    [Header("Breathing")]
    [SerializeField, Min(0.001f)] private float scaleMultiplier = 1.025f;

    [Header("Float")]
    [SerializeField, Min(0f)] private float floatAmplitudeY = 8f;

    [Header("Tilt")]
    [SerializeField, Min(0f)] private float rotationAmplitudeZ = 0.8f;

    [Header("Update")]
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Tween startDelayTween;
    private Tween scaleTween;
    private Tween floatTween;
    private Tween rotationTween;
    private bool capturedRestPose;
    private Vector2 restAnchoredPosition;
    private Vector3 restLocalScale;
    private Vector3 restLocalEulerAngles;

    private void Awake()
    {
        ResolveRectTransform();
        CaptureRestPoseIfNeeded();
    }

    private void OnEnable()
    {
        ResolveRectTransform();
        CaptureRestPoseIfNeeded();
        StopTweens(false);
        ScheduleIdleMotion();
    }

    private void OnDisable()
    {
        StopTweens(true);
    }

    private void OnDestroy()
    {
        StopTweens(false);
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
        if (capturedRestPose || rectTransform == null)
        {
            return;
        }

        restAnchoredPosition = rectTransform.anchoredPosition;
        restLocalScale = rectTransform.localScale;
        restLocalEulerAngles = rectTransform.localEulerAngles;
        capturedRestPose = true;
    }

    private void ScheduleIdleMotion()
    {
        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        if (startDelay <= 0f)
        {
            StartIdleMotion();
            return;
        }

        startDelayTween = DOVirtual.DelayedCall(startDelay, StartIdleMotion).SetUpdate(useUnscaledTime);
    }

    private void StartIdleMotion()
    {
        startDelayTween = null;

        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        if (scaleTween != null && scaleTween.IsActive())
        {
            return;
        }

        Vector3 targetScale = restLocalScale * scaleMultiplier;
        Vector2 targetAnchoredPosition = restAnchoredPosition + Vector2.up * floatAmplitudeY;
        Vector3 negativeEulerAngles = restLocalEulerAngles - new Vector3(0f, 0f, rotationAmplitudeZ);
        Vector3 targetEulerAngles = restLocalEulerAngles + new Vector3(0f, 0f, rotationAmplitudeZ);

        RestoreRestPose();

        scaleTween = DOTween.Sequence()
            .AppendInterval(0.12f)
            .Append(rectTransform.DOScale(targetScale, scaleHalfCycleDuration)
                .SetEase(Ease.InOutSine))
            .Append(rectTransform.DOScale(restLocalScale, scaleHalfCycleDuration)
                .SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(useUnscaledTime);
        floatTween = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(targetAnchoredPosition, floatHalfCycleDuration)
                .SetEase(Ease.InOutSine))
            .Append(rectTransform.DOAnchorPos(restAnchoredPosition, floatHalfCycleDuration)
                .SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(useUnscaledTime);
        rotationTween = DOTween.Sequence()
            .AppendInterval(0.28f)
            .Append(rectTransform.DOLocalRotate(targetEulerAngles, rotationHalfCycleDuration * 0.5f, RotateMode.Fast)
                .SetEase(Ease.InOutSine))
            .Append(rectTransform.DOLocalRotate(negativeEulerAngles, rotationHalfCycleDuration, RotateMode.Fast)
                .SetEase(Ease.InOutSine))
            .Append(rectTransform.DOLocalRotate(restLocalEulerAngles, rotationHalfCycleDuration * 0.5f, RotateMode.Fast)
                .SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(useUnscaledTime);
    }

    private void StopTweens(bool restorePose)
    {
        if (startDelayTween != null)
        {
            startDelayTween.Kill();
            startDelayTween = null;
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

        if (rotationTween != null)
        {
            rotationTween.Kill();
            rotationTween = null;
        }

        if (restorePose)
        {
            RestoreRestPose();
        }
    }

    private void RestoreRestPose()
    {
        if (!capturedRestPose || rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = restAnchoredPosition;
        rectTransform.localScale = restLocalScale;
        rectTransform.localRotation = Quaternion.Euler(restLocalEulerAngles);
    }
}
