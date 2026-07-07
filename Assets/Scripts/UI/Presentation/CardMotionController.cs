using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UIMotionPlayer))]
public class CardMotionController : ViewPartBase
{
    [Header("依赖")]
    [SerializeField] private UIMotionPlayer motionPlayer;
    [SerializeField] private CardMotionSettings motionSettings = new();

    [Header("复位根节点")]
    [SerializeField] private RectTransform restRoot;

    [Header("运行时动态")]
    [SerializeField] private RectTransform dynamicRoot;

    [Header("视觉层")]
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
    private bool missingSettingsLogged;

    public bool CanReceiveInteraction => !isRevealPlaying && !isSubmitting;

    [Serializable]
    private sealed class CardMotionSettings
    {
        [Header("界面动画片段")]
        [SerializeField] private string revealClipId = UIMotionClipIds.SHOW;
        [SerializeField] private string hoverInClipId = UIMotionClipIds.HOVER_IN;
        [SerializeField] private string hoverOutClipId = UIMotionClipIds.HOVER_OUT;
        [SerializeField] private string pressClipId = UIMotionClipIds.PRESS;
        [SerializeField] private string releaseClipId = UIMotionClipIds.RELEASE;
        [SerializeField] private string selectClipId = UIMotionClipIds.CLICK_PULSE;
        [SerializeField] private string selectedClaimClipId = CardMotionClipIds.SELECTED_CLAIM;
        [SerializeField] private string rejectedSubmitClipId = CardMotionClipIds.REJECTED_SUBMIT;
        [Tooltip("可选。为空时中断复位只恢复控制器记录的根节点 Transform，不调用界面动画采样，避免没有可见/显示片段的配置刷警告。")]
        [SerializeField] private string restClipId = UIMotionClipIds.VISIBLE;

        [Header("复用与中断")]
        [Tooltip("配置新卡牌内容时刷新界面动画默认快照，避免对象复用后保留上一张卡的交互状态。")]
        [SerializeField] private bool refreshDefaultsOnConfigure = true;

        [Tooltip("对象隐藏或点击流程被打断时，采样到稳定可见状态后再刷新默认快照。")]
        [SerializeField] private bool resetToRestClipWhenInterrupted = true;

        [Tooltip("卡牌配置新内容后是否播放显示片段。关闭后只刷新状态并恢复运行时浮动。")]
        [SerializeField] private bool playRevealOnConfigure = true;

        [Header("运行时卡牌动态")]
        [SerializeField] private bool enableIdleFloat = true;
        [SerializeField] [Min(0f)] private float idleFloatAmplitude = 4f;
        [SerializeField] [Min(0.05f)] private float idleFloatDuration = 2.4f;

        [SerializeField] private bool enablePointerTilt = true;
        [SerializeField] [Range(0f, 18f)] private float hoverTiltAngle = 5f;
        [SerializeField] [Min(0.01f)] private float hoverTiltDuration = 0.14f;
        [SerializeField] [Min(0.01f)] private float hoverReturnDuration = 0.18f;

        [Header("视觉层动态")]
        [SerializeField] private bool enableVisualLayerDynamics = true;
        [SerializeField] [Range(0f, 1f)] private float glowIdleAlpha = 0.12f;
        [SerializeField] [Range(0f, 1f)] private float glowHoverAlpha = 0.38f;
        [SerializeField] [Range(0f, 1f)] private float glowPressAlpha = 0.24f;
        [SerializeField] [Range(0f, 1f)] private float glowSelectAlpha = 0.68f;
        [SerializeField] [Range(0f, 1f)] private float shadowIdleAlpha = 0.42f;
        [SerializeField] [Range(0f, 1f)] private float shadowHoverAlpha = 0.62f;
        [SerializeField] [Range(0f, 1f)] private float shadowPressAlpha = 0.34f;
        [SerializeField] private Vector2 shadowHoverOffset = new Vector2(0f, -8f);
        [SerializeField] private Vector2 shadowPressOffset = new Vector2(0f, -3f);
        [SerializeField] [Min(0.01f)] private float visualLayerTweenDuration = 0.14f;

        public string RevealClipId => NormalizeClipId(revealClipId);
        public string HoverInClipId => NormalizeClipId(hoverInClipId);
        public string HoverOutClipId => NormalizeClipId(hoverOutClipId);
        public string PressClipId => NormalizeClipId(pressClipId);
        public string ReleaseClipId => NormalizeClipId(releaseClipId);
        public string SelectClipId => NormalizeClipId(selectClipId);
        public string SelectedClaimClipId => NormalizeClipId(selectedClaimClipId);
        public string RejectedSubmitClipId => NormalizeClipId(rejectedSubmitClipId);
        public string RestClipId => NormalizeClipId(restClipId);
        public bool RefreshDefaultsOnConfigure => refreshDefaultsOnConfigure;
        public bool ResetToRestClipWhenInterrupted => resetToRestClipWhenInterrupted;
        public bool PlayRevealOnConfigure => playRevealOnConfigure;
        public bool EnableIdleFloat => enableIdleFloat;
        public float IdleFloatAmplitude => Mathf.Max(0f, idleFloatAmplitude);
        public float IdleFloatDuration => Mathf.Max(0.05f, idleFloatDuration);
        public bool EnablePointerTilt => enablePointerTilt;
        public float HoverTiltAngle => Mathf.Max(0f, hoverTiltAngle);
        public float HoverTiltDuration => Mathf.Max(0.01f, hoverTiltDuration);
        public float HoverReturnDuration => Mathf.Max(0.01f, hoverReturnDuration);
        public bool EnableVisualLayerDynamics => enableVisualLayerDynamics;
        public float GlowIdleAlpha => Mathf.Clamp01(glowIdleAlpha);
        public float GlowHoverAlpha => Mathf.Clamp01(glowHoverAlpha);
        public float GlowPressAlpha => Mathf.Clamp01(glowPressAlpha);
        public float GlowSelectAlpha => Mathf.Clamp01(glowSelectAlpha);
        public float ShadowIdleAlpha => Mathf.Clamp01(shadowIdleAlpha);
        public float ShadowHoverAlpha => Mathf.Clamp01(shadowHoverAlpha);
        public float ShadowPressAlpha => Mathf.Clamp01(shadowPressAlpha);
        public Vector2 ShadowHoverOffset => shadowHoverOffset;
        public Vector2 ShadowPressOffset => shadowPressOffset;
        public float VisualLayerTweenDuration => Mathf.Max(0.01f, visualLayerTweenDuration);

        private static string NormalizeClipId(string clipId)
        {
            return string.IsNullOrWhiteSpace(clipId) ? string.Empty : clipId.Trim();
        }
    }

    protected virtual void Awake()
    {
        ResolveDependencies();
        CaptureRestPoseIfNeeded();
        CaptureVisualLayerPoseIfNeeded();
    }

    protected virtual void OnEnable()
    {
        ResolveDependencies();
        CaptureRestPoseIfNeeded();
        CaptureDynamicPoseIfNeeded();
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

        if (motionSettings == null)
        {
            motionSettings = new CardMotionSettings();
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
        CaptureVisualLayerPoseIfNeeded();
        StopRuntimeDynamics(restorePose: true);
        if (!HasMotionSettings())
        {
            return;
        }

        PlayIdleVisualLayer(immediate: true);
        SampleRestClipIfNeeded();

        if (motionSettings.RefreshDefaultsOnConfigure)
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
        if (!HasMotionSettings())
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
        PlayClip(motionSettings.HoverInClipId);
        UpdatePointerTilt(eventData);
    }

    public void PlayHoverOut()
    {
        if (!HasMotionSettings())
        {
            return;
        }

        isPointerInside = false;
        if (isRevealPlaying)
        {
            return;
        }

        PlayIdleVisualLayer(immediate: false);
        Tween hoverOutTween = PlayClip(motionSettings.HoverOutClipId);
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
        if (!HasMotionSettings() || isRevealPlaying)
        {
            return;
        }

        StopIdleFloat(restorePose: false);
        PlayPressVisualLayer();
        PlayClip(motionSettings.PressClipId);
    }

    public void PlayRelease()
    {
        if (!HasMotionSettings() || isRevealPlaying)
        {
            return;
        }

        PlayHoverVisualLayer();
        PlayClip(motionSettings.ReleaseClipId);
    }

    public async UniTask PlaySelectAsync(CancellationToken cancellationToken)
    {
        ResolveDependencies();
        if (!HasMotionSettings())
        {
            return;
        }

        isSubmitting = true;
        StopRuntimeDynamics(restorePose: true);
        PlaySelectVisualLayer();

        if (motionPlayer == null || string.IsNullOrWhiteSpace(motionSettings.SelectClipId))
        {
            return;
        }

        await motionPlayer.PlayAsync(motionSettings.SelectClipId, cancellationToken);
    }

    public async UniTask PlaySelectedSubmitAsync(CancellationToken cancellationToken)
    {
        ResolveDependencies();
        if (!HasMotionSettings())
        {
            return;
        }

        isSubmitting = true;
        StopRuntimeDynamics(restorePose: true);
        PlaySelectVisualLayer();

        if (motionPlayer == null)
        {
            return;
        }

        string clipId = !string.IsNullOrWhiteSpace(motionSettings.SelectedClaimClipId)
            ? motionSettings.SelectedClaimClipId
            : motionSettings.SelectClipId;
        if (string.IsNullOrWhiteSpace(clipId))
        {
            return;
        }

        await motionPlayer.PlayAsync(clipId, cancellationToken);
    }

    public async UniTask PlayRejectedSubmitAsync(CancellationToken cancellationToken)
    {
        ResolveDependencies();
        if (!HasMotionSettings())
        {
            return;
        }

        isSubmitting = true;
        isRevealPlaying = false;
        StopRuntimeDynamics(restorePose: true);
        PlayIdleVisualLayer(immediate: false);

        if (motionPlayer == null)
        {
            return;
        }

        string clipId = !string.IsNullOrWhiteSpace(motionSettings.RejectedSubmitClipId)
            ? motionSettings.RejectedSubmitClipId
            : UIMotionClipIds.HIDE;
        await motionPlayer.PlayAsync(clipId, cancellationToken);
    }

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        ResolveDependencies();
        if (!HasMotionSettings())
        {
            return;
        }

        isRevealPlaying = false;
        StopRuntimeDynamics(restorePose: true);
        PlayIdleVisualLayer(immediate: false);

        if (motionPlayer == null)
        {
            return;
        }

        await motionPlayer.PlayAsync(UIMotionClipIds.HIDE, cancellationToken);
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

        if (!HasMotionSettings())
        {
            RestoreRestPose();
            RestoreDynamicPose();
            RestoreVisualLayerPose();
            motionPlayer?.RefreshDefaults();
            return;
        }

        string restClipId = motionSettings.RestClipId;
        if (motionPlayer != null
            && motionSettings.ResetToRestClipWhenInterrupted
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
        if (!HasMotionSettings()
            || eventData == null
            || !isPointerInside
            || isSubmitting
            || isRevealPlaying
            || !motionSettings.EnablePointerTilt)
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

        float tiltAngle = motionSettings.HoverTiltAngle;
        Vector3 targetEulerAngles = dynamicLocalEulerAngles + new Vector3(
            -normalizedY * tiltAngle,
            normalizedX * tiltAngle,
            -normalizedX * tiltAngle * 0.18f);

        PlayPointerTilt(targetEulerAngles, motionSettings.HoverTiltDuration);
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
        if (!HasMotionSettings())
        {
            return;
        }

        if (!motionSettings.PlayRevealOnConfigure)
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
        revealTween = PlayClip(motionSettings.RevealClipId);
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
            || string.IsNullOrWhiteSpace(motionSettings.RestClipId)
            || !motionSettings.ResetToRestClipWhenInterrupted)
        {
            return;
        }

        motionPlayer.SetImmediate(motionSettings.RestClipId);
    }

    private void OnRevealComplete()
    {
        revealTween = null;
        isRevealPlaying = false;

        if (isPointerInside)
        {
            StopIdleFloat(restorePose: false);
            PlayHoverVisualLayer();
            PlayClip(motionSettings.HoverInClipId);
            return;
        }

        StartIdleFloatIfNeeded();
    }

    private bool HasMotionSettings()
    {
        if (motionSettings != null)
        {
            missingSettingsLogged = false;
            return true;
        }

        if (!missingSettingsLogged)
        {
            Debug.LogError(
                $"{nameof(CardMotionController)} on '{name}' is missing inline motion settings.",
                this);
            missingSettingsLogged = true;
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
        resolvedDynamicRoot = dynamicRoot != null ? dynamicRoot : resolvedRestRoot;
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

        if (!HasMotionSettings()
            || !isActiveAndEnabled
            || isSubmitting
            || !motionSettings.EnableIdleFloat)
        {
            return;
        }

        if (resolvedDynamicRoot == null || Mathf.Approximately(motionSettings.IdleFloatAmplitude, 0f))
        {
            return;
        }

        if (idleFloatTween != null && idleFloatTween.IsActive())
        {
            return;
        }

        resolvedDynamicRoot.anchoredPosition = dynamicAnchoredPosition;
        idleFloatTween = resolvedDynamicRoot
            .DOAnchorPosY(dynamicAnchoredPosition.y + motionSettings.IdleFloatAmplitude, motionSettings.IdleFloatDuration)
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

        if (!HasMotionSettings())
        {
            return;
        }

        PlayPointerTilt(dynamicLocalEulerAngles, motionSettings.HoverReturnDuration);
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
            motionSettings.GlowIdleAlpha,
            motionSettings.ShadowIdleAlpha,
            Vector2.zero,
            immediate);
    }

    private void PlayHoverVisualLayer()
    {
        PlayVisualLayerState(
            motionSettings.GlowHoverAlpha,
            motionSettings.ShadowHoverAlpha,
            motionSettings.ShadowHoverOffset,
            immediate: false);
    }

    private void PlayPressVisualLayer()
    {
        PlayVisualLayerState(
            motionSettings.GlowPressAlpha,
            motionSettings.ShadowPressAlpha,
            motionSettings.ShadowPressOffset,
            immediate: false);
    }

    private void PlaySelectVisualLayer()
    {
        PlayVisualLayerState(
            motionSettings.GlowSelectAlpha,
            motionSettings.ShadowHoverAlpha,
            motionSettings.ShadowHoverOffset,
            immediate: false);
    }

    private void PlayVisualLayerState(
        float glowAlpha,
        float shadowAlpha,
        Vector2 shadowOffset,
        bool immediate)
    {
        CaptureVisualLayerPoseIfNeeded();
        if (!HasMotionSettings() || !motionSettings.EnableVisualLayerDynamics)
        {
            return;
        }

        float duration = motionSettings.VisualLayerTweenDuration;
        if (glowCanvasGroup != null)
        {
            glowAlphaTween?.Kill();
            if (immediate)
            {
                glowCanvasGroup.alpha = glowAlpha;
                glowAlphaTween = null;
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
                shadowAlphaTween = null;
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
                shadowPositionTween = null;
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

}
