using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UIMotionPlayer))]
public class CardMotionController : MonoBehaviour
{
    private const string VISUAL_ROOT_NAME = "VisualRoot";
    private const string SHADOW_NAME = "Shadow";
    private const string GLOW_NAME = "Glow";

    [Header("依赖")]
    [SerializeField] private UIMotionPlayer motionPlayer;
    [SerializeField] private CardMotionProfileSO profile;

    [Header("复位根节点")]
    [Tooltip("默认使用当前物体。后续如果卡牌拆出VisualRoot，可以把VisualRoot拖进来，避免和外层布局互相影响。")]
    [SerializeField] private RectTransform restRoot;

    [Header("运行时动态")]
    [Tooltip("用于悬停倾斜和轻微浮动的视觉根节点。为空时使用复位根节点。")]
    [SerializeField] private RectTransform dynamicRoot;
    [SerializeField] private bool autoResolveVisualRoot = true;

    [Header("视觉层")]
    [SerializeField] private bool autoResolveVisualLayers = true;
    [SerializeField] private RectTransform shadowRoot;
    [SerializeField] private CanvasGroup shadowCanvasGroup;
    [SerializeField] private CanvasGroup glowCanvasGroup;

    private RectTransform resolvedRestRoot;
    private RectTransform resolvedDynamicRoot;
    private bool capturedRestPose;
    private Vector2 restAnchoredPosition;
    private Vector3 restLocalScale;
    private Vector3 restLocalEulerAngles;
    private bool capturedDynamicPose;
    private Vector2 dynamicAnchoredPosition;
    private Vector3 dynamicLocalEulerAngles;
    private bool capturedVisualLayerPose;
    private Vector2 shadowAnchoredPosition;
    private float shadowCanvasAlpha;
    private float glowCanvasAlpha;
    private Tween idleFloatTween;
    private Tween pointerTiltTween;
    private Tween revealTween;
    private Tween shadowPositionTween;
    private Tween shadowAlphaTween;
    private Tween glowAlphaTween;
    private bool isPointerInside;
    private bool isSubmitting;
    private bool isRevealPlaying;
    private bool missingProfileLogged;

    public bool CanReceiveInteraction => !isRevealPlaying && !isSubmitting;

    protected virtual void Awake()
    {
        ResolveDependencies();
        CaptureRestPoseIfNeeded();
        ResolveVisualLayerReferences();
        CaptureVisualLayerPoseIfNeeded();
    }

    protected virtual void OnEnable()
    {
        ResolveDependencies();
        CaptureRestPoseIfNeeded();
        CaptureDynamicPoseIfNeeded();
        ResolveVisualLayerReferences();
        CaptureVisualLayerPoseIfNeeded();
        StartIdleFloatIfNeeded();
    }

    protected virtual void OnDisable()
    {
        CancelAndReset();
    }

    protected virtual void OnValidate()
    {
        if (motionPlayer == null)
        {
            motionPlayer = GetComponent<UIMotionPlayer>();
        }

        if (restRoot == null)
        {
            restRoot = transform as RectTransform;
        }

        if (autoResolveVisualRoot && dynamicRoot == null)
        {
            dynamicRoot = FindRectTransformByName(VISUAL_ROOT_NAME);
        }

        if (autoResolveVisualLayers)
        {
            ResolveVisualLayerReferences();
        }
    }

    public void ConfigureForReuse()
    {
        ConfigureForReuse(playReveal: true);
    }

    public void ConfigureForReuse(bool playReveal)
    {
        ResolveDependencies();
        CaptureRestPoseIfNeeded();
        CaptureDynamicPoseIfNeeded();
        ResolveVisualLayerReferences();
        CaptureVisualLayerPoseIfNeeded();
        StopRuntimeDynamics(restorePose: true);
        if (!HasProfile())
        {
            return;
        }

        PlayIdleVisualLayer(immediate: true);
        SampleRestClipIfNeeded();

        if (profile.RefreshDefaultsOnConfigure)
        {
            motionPlayer?.RefreshDefaults();
        }

        if (playReveal)
        {
            PlayRevealOrStartIdle();
            return;
        }

        StartIdleFloatIfNeeded();
    }

    public void PlayHoverIn(PointerEventData eventData = null)
    {
        if (!HasProfile())
        {
            return;
        }

        isPointerInside = true;
        if (isRevealPlaying)
        {
            return;
        }

        StopIdleFloat(restorePose: false);
        PlayHoverVisualLayer();
        PlayClip(profile.HoverInClipId);
        UpdatePointerTilt(eventData);
    }

    public void PlayHoverOut()
    {
        if (!HasProfile())
        {
            return;
        }

        isPointerInside = false;
        if (isRevealPlaying)
        {
            return;
        }

        PlayIdleVisualLayer(immediate: false);
        Tween hoverOutTween = PlayClip(profile.HoverOutClipId);
        ReturnPointerTilt();
        if (hoverOutTween != null)
        {
            hoverOutTween.OnComplete(StartIdleFloatIfNeeded);
        }
        else
        {
            StartIdleFloatIfNeeded();
        }
    }

    public void PlayPress()
    {
        if (!HasProfile() || isRevealPlaying)
        {
            return;
        }

        StopIdleFloat(restorePose: false);
        PlayPressVisualLayer();
        PlayClip(profile.PressClipId);
    }

    public void PlayRelease()
    {
        if (!HasProfile() || isRevealPlaying)
        {
            return;
        }

        PlayHoverVisualLayer();
        PlayClip(profile.ReleaseClipId);
    }

    public IEnumerator PlaySelectAndWait()
    {
        ResolveDependencies();
        if (!HasProfile())
        {
            yield break;
        }

        isSubmitting = true;
        StopRuntimeDynamics(restorePose: true);
        PlaySelectVisualLayer();

        if (motionPlayer == null || string.IsNullOrWhiteSpace(profile.SelectClipId))
        {
            yield break;
        }

        yield return motionPlayer.PlayAndWait(profile.SelectClipId);
    }

    public IEnumerator PlayRefreshOutAndWait()
    {
        ResolveDependencies();
        if (!HasProfile())
        {
            yield break;
        }

        isRevealPlaying = false;
        StopRuntimeDynamics(restorePose: true);
        PlayIdleVisualLayer(immediate: false);

        if (motionPlayer == null)
        {
            yield break;
        }

        yield return motionPlayer.PlayAndWait(UIMotionClipIds.HIDE);
    }

    public void CancelAndReset()
    {
        ResetToRest();
    }

    public void ResetToRest()
    {
        ResolveDependencies();
        isPointerInside = false;
        isSubmitting = false;
        isRevealPlaying = false;
        StopRuntimeDynamics(restorePose: true);
        motionPlayer?.Kill();

        if (!HasProfile())
        {
            RestoreRestPose();
            RestoreDynamicPose();
            RestoreVisualLayerPose();
            motionPlayer?.RefreshDefaults();
            return;
        }

        string restClipId = profile.RestClipId;
        if (motionPlayer != null
            && profile.ResetToRestClipWhenInterrupted
            && !string.IsNullOrWhiteSpace(restClipId))
        {
            motionPlayer.SetImmediate(restClipId);
        }

        RestoreRestPose();
        RestoreDynamicPose();
        RestoreVisualLayerPose();
        PlayIdleVisualLayer(immediate: true);
        motionPlayer?.RefreshDefaults();

        StartIdleFloatIfNeeded();
    }

    public void UpdatePointerTilt(PointerEventData eventData)
    {
        ResolveDependencies();
        if (!HasProfile()
            || eventData == null
            || !isPointerInside
            || isSubmitting
            || isRevealPlaying
            || !profile.EnablePointerTilt)
        {
            return;
        }

        if (resolvedDynamicRoot == null)
        {
            return;
        }

        Camera eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                resolvedDynamicRoot,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Rect rect = resolvedDynamicRoot.rect;
        float normalizedX = rect.width > 0f
            ? Mathf.Clamp(localPoint.x / (rect.width * 0.5f), -1f, 1f)
            : 0f;
        float normalizedY = rect.height > 0f
            ? Mathf.Clamp(localPoint.y / (rect.height * 0.5f), -1f, 1f)
            : 0f;

        float tiltAngle = profile.HoverTiltAngle;
        Vector3 targetEulerAngles = dynamicLocalEulerAngles + new Vector3(
            -normalizedY * tiltAngle,
            normalizedX * tiltAngle,
            -normalizedX * tiltAngle * 0.18f);

        PlayPointerTilt(targetEulerAngles, profile.HoverTiltDuration);
    }

    private Tween PlayClip(string clipId)
    {
        ResolveDependencies();
        if (string.IsNullOrWhiteSpace(clipId))
        {
            return null;
        }

        return motionPlayer?.Play(clipId);
    }

    private void PlayRevealOrStartIdle()
    {
        if (!HasProfile())
        {
            return;
        }

        if (!profile.PlayRevealOnConfigure)
        {
            StartIdleFloatIfNeeded();
            return;
        }

        ResolveDependencies();
        if (motionPlayer == null)
        {
            StartIdleFloatIfNeeded();
            return;
        }

        revealTween?.Kill();
        isRevealPlaying = true;
        revealTween = PlayClip(profile.RevealClipId);
        if (revealTween == null)
        {
            isRevealPlaying = false;
            StartIdleFloatIfNeeded();
            return;
        }

        revealTween.OnComplete(OnRevealComplete);
    }

    private void SampleRestClipIfNeeded()
    {
        if (motionPlayer == null
            || string.IsNullOrWhiteSpace(profile.RestClipId)
            || !profile.ResetToRestClipWhenInterrupted)
        {
            return;
        }

        motionPlayer.SetImmediate(profile.RestClipId);
    }

    private void OnRevealComplete()
    {
        revealTween = null;
        isRevealPlaying = false;

        if (isPointerInside)
        {
            StopIdleFloat(restorePose: false);
            PlayHoverVisualLayer();
            PlayClip(profile.HoverInClipId);
            return;
        }

        StartIdleFloatIfNeeded();
    }

    private bool HasProfile()
    {
        if (profile != null)
        {
            missingProfileLogged = false;
            return true;
        }

        if (!missingProfileLogged)
        {
            Debug.LogError(
                $"{nameof(CardMotionController)} on '{name}' requires an {nameof(CardMotionProfileSO)} asset.",
                this);
            missingProfileLogged = true;
        }

        return false;
    }

    private void ResolveDependencies()
    {
        if (motionPlayer == null)
        {
            motionPlayer = GetComponent<UIMotionPlayer>();
        }

        resolvedRestRoot = restRoot != null ? restRoot : transform as RectTransform;
        if (autoResolveVisualRoot && dynamicRoot == null)
        {
            dynamicRoot = FindRectTransformByName(VISUAL_ROOT_NAME);
        }

        resolvedDynamicRoot = dynamicRoot != null ? dynamicRoot : resolvedRestRoot;
    }

    private void ResolveVisualLayerReferences()
    {
        if (!autoResolveVisualLayers)
        {
            return;
        }

        if (shadowRoot == null)
        {
            shadowRoot = FindRectTransformByName(SHADOW_NAME);
        }

        if (shadowCanvasGroup == null && shadowRoot != null)
        {
            shadowCanvasGroup = shadowRoot.GetComponent<CanvasGroup>();
        }

        if (glowCanvasGroup == null)
        {
            RectTransform glowRoot = FindRectTransformByName(GLOW_NAME);
            if (glowRoot != null)
            {
                glowCanvasGroup = glowRoot.GetComponent<CanvasGroup>();
            }
        }
    }

    private void CaptureRestPoseIfNeeded()
    {
        if (capturedRestPose || resolvedRestRoot == null)
        {
            return;
        }

        restAnchoredPosition = resolvedRestRoot.anchoredPosition;
        restLocalScale = resolvedRestRoot.localScale;
        restLocalEulerAngles = resolvedRestRoot.localEulerAngles;
        capturedRestPose = true;
    }

    private void CaptureDynamicPoseIfNeeded()
    {
        if (capturedDynamicPose || resolvedDynamicRoot == null)
        {
            return;
        }

        dynamicAnchoredPosition = resolvedDynamicRoot.anchoredPosition;
        dynamicLocalEulerAngles = resolvedDynamicRoot.localEulerAngles;
        capturedDynamicPose = true;
    }

    private void CaptureVisualLayerPoseIfNeeded()
    {
        if (capturedVisualLayerPose)
        {
            return;
        }

        if (shadowRoot != null)
        {
            shadowAnchoredPosition = shadowRoot.anchoredPosition;
        }

        if (shadowCanvasGroup != null)
        {
            shadowCanvasAlpha = shadowCanvasGroup.alpha;
        }

        if (glowCanvasGroup != null)
        {
            glowCanvasAlpha = glowCanvasGroup.alpha;
        }

        capturedVisualLayerPose = true;
    }

    private void RestoreRestPose()
    {
        if (!capturedRestPose || resolvedRestRoot == null)
        {
            return;
        }

        resolvedRestRoot.anchoredPosition = restAnchoredPosition;
        resolvedRestRoot.localScale = restLocalScale;
        resolvedRestRoot.localEulerAngles = restLocalEulerAngles;
    }

    private void RestoreDynamicPose()
    {
        if (!capturedDynamicPose || resolvedDynamicRoot == null)
        {
            return;
        }

        resolvedDynamicRoot.anchoredPosition = dynamicAnchoredPosition;
        resolvedDynamicRoot.localEulerAngles = dynamicLocalEulerAngles;
    }

    private void RestoreVisualLayerPose()
    {
        if (!capturedVisualLayerPose)
        {
            return;
        }

        if (shadowRoot != null)
        {
            shadowRoot.anchoredPosition = shadowAnchoredPosition;
        }

        if (shadowCanvasGroup != null)
        {
            shadowCanvasGroup.alpha = shadowCanvasAlpha;
        }

        if (glowCanvasGroup != null)
        {
            glowCanvasGroup.alpha = glowCanvasAlpha;
        }
    }

    private void StartIdleFloatIfNeeded()
    {
        ResolveDependencies();
        CaptureDynamicPoseIfNeeded();

        if (!HasProfile()
            || !isActiveAndEnabled
            || isSubmitting
            || !profile.EnableIdleFloat)
        {
            return;
        }

        if (resolvedDynamicRoot == null || Mathf.Approximately(profile.IdleFloatAmplitude, 0f))
        {
            return;
        }

        if (idleFloatTween != null && idleFloatTween.IsActive())
        {
            return;
        }

        resolvedDynamicRoot.anchoredPosition = dynamicAnchoredPosition;
        idleFloatTween = resolvedDynamicRoot
            .DOAnchorPosY(dynamicAnchoredPosition.y + profile.IdleFloatAmplitude, profile.IdleFloatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopRuntimeDynamics(bool restorePose)
    {
        if (idleFloatTween != null)
        {
            idleFloatTween.Kill();
            idleFloatTween = null;
        }

        if (pointerTiltTween != null)
        {
            pointerTiltTween.Kill();
            pointerTiltTween = null;
        }

        if (revealTween != null)
        {
            revealTween.Kill();
            revealTween = null;
        }

        isRevealPlaying = false;

        KillVisualLayerTweens();

        if (restorePose)
        {
            RestoreDynamicPose();
            RestoreVisualLayerPose();
        }
    }

    private void StopIdleFloat(bool restorePose)
    {
        if (idleFloatTween == null)
        {
            return;
        }

        idleFloatTween.Kill();
        idleFloatTween = null;
        if (restorePose)
        {
            RestoreDynamicPose();
        }
    }

    private void ReturnPointerTilt()
    {
        if (!capturedDynamicPose || resolvedDynamicRoot == null)
        {
            return;
        }

        if (!HasProfile())
        {
            return;
        }

        PlayPointerTilt(dynamicLocalEulerAngles, profile.HoverReturnDuration);
    }

    private void PlayPointerTilt(Vector3 targetEulerAngles, float duration)
    {
        if (resolvedDynamicRoot == null)
        {
            return;
        }

        pointerTiltTween?.Kill();
        pointerTiltTween = resolvedDynamicRoot
            .DOLocalRotate(targetEulerAngles, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void PlayIdleVisualLayer(bool immediate)
    {
        PlayVisualLayerState(
            profile.GlowIdleAlpha,
            profile.ShadowIdleAlpha,
            Vector2.zero,
            immediate);
    }

    private void PlayHoverVisualLayer()
    {
        PlayVisualLayerState(
            profile.GlowHoverAlpha,
            profile.ShadowHoverAlpha,
            profile.ShadowHoverOffset,
            immediate: false);
    }

    private void PlayPressVisualLayer()
    {
        PlayVisualLayerState(
            profile.GlowPressAlpha,
            profile.ShadowPressAlpha,
            profile.ShadowPressOffset,
            immediate: false);
    }

    private void PlaySelectVisualLayer()
    {
        PlayVisualLayerState(
            profile.GlowSelectAlpha,
            profile.ShadowHoverAlpha,
            profile.ShadowHoverOffset,
            immediate: false);
    }

    private void PlayVisualLayerState(
        float glowAlpha,
        float shadowAlpha,
        Vector2 shadowOffset,
        bool immediate)
    {
        ResolveVisualLayerReferences();
        CaptureVisualLayerPoseIfNeeded();
        if (!HasProfile() || !profile.EnableVisualLayerDynamics)
        {
            return;
        }

        float duration = profile.VisualLayerTweenDuration;
        if (glowCanvasGroup != null)
        {
            glowAlphaTween?.Kill();
            if (immediate)
            {
                glowCanvasGroup.alpha = glowAlpha;
            }
            else
            {
                glowAlphaTween = glowCanvasGroup
                    .DOFade(glowAlpha, duration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }
        }

        if (shadowCanvasGroup != null)
        {
            shadowAlphaTween?.Kill();
            if (immediate)
            {
                shadowCanvasGroup.alpha = shadowAlpha;
            }
            else
            {
                shadowAlphaTween = shadowCanvasGroup
                    .DOFade(shadowAlpha, duration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }
        }

        if (shadowRoot != null)
        {
            Vector2 targetPosition = shadowAnchoredPosition + shadowOffset;
            shadowPositionTween?.Kill();
            if (immediate)
            {
                shadowRoot.anchoredPosition = targetPosition;
            }
            else
            {
                shadowPositionTween = shadowRoot
                    .DOAnchorPos(targetPosition, duration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }
        }
    }

    private void KillVisualLayerTweens()
    {
        if (shadowPositionTween != null)
        {
            shadowPositionTween.Kill();
            shadowPositionTween = null;
        }

        if (shadowAlphaTween != null)
        {
            shadowAlphaTween.Kill();
            shadowAlphaTween = null;
        }

        if (glowAlphaTween != null)
        {
            glowAlphaTween.Kill();
            glowAlphaTween = null;
        }
    }

    private RectTransform FindRectTransformByName(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target as RectTransform;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
