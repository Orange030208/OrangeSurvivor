using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ContinuousSpinMotion : MonoBehaviour
{
    [SerializeField, Min(0f)] private float startDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float secondsPerLoop = 14f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Tween delayTween;
    private Tween spinTween;
    private bool restCaptured;
    private Vector3 restEulerAngles;

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
        ScheduleSpin();
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

        restEulerAngles = rectTransform.localEulerAngles;
        restCaptured = true;
    }

    private void ScheduleSpin()
    {
        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        if (startDelay <= 0f)
        {
            StartSpin();
            return;
        }

        delayTween = DOVirtual.DelayedCall(startDelay, StartSpin).SetUpdate(useUnscaledTime);
    }

    private void StartSpin()
    {
        delayTween = null;
        if (!isActiveAndEnabled || rectTransform == null)
        {
            return;
        }

        RestoreRestPose();
        Vector3 loopEulerAngles = restEulerAngles + new Vector3(0f, 0f, clockwise ? -360f : 360f);
        spinTween = rectTransform
            .DOLocalRotate(loopEulerAngles, secondsPerLoop, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
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

        if (spinTween != null)
        {
            spinTween.Kill();
            spinTween = null;
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

        rectTransform.localRotation = Quaternion.Euler(restEulerAngles);
    }
}
