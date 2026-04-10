using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class UIRevealMotion : MonoBehaviour
{
    private enum UIRevealPreset
    {
        [InspectorName("自定义")]
        Custom,
        [InspectorName("标题")]
        Title,
        [InspectorName("主视觉标题")]
        HeroTitle,
        [InspectorName("主按钮")]
        PrimaryButton,
        [InspectorName("次按钮")]
        SecondaryButton,
        [InspectorName("悬浮操作按钮")]
        FloatingAction,
        [InspectorName("面板")]
        Panel,
        [InspectorName("弹窗")]
        Modal,
        [InspectorName("卡片")]
        Card,
        [InspectorName("提示气泡")]
        Tooltip,
        [InspectorName("通知")]
        Notification,
        [InspectorName("徽标")]
        Badge,
        [InspectorName("侧边栏")]
        Sidebar
    }

    private CanvasGroup canvasGroup;
    private RectTransform targetRect;

    [Header("预设")]
    [SerializeField] private UIRevealPreset preset = UIRevealPreset.Custom;
    [SerializeField] private bool applyPresetOnAwake = true;

    [Header("强度")]
    [SerializeField] [Min(0f)] private float motionIntensity = 1f;

    [Header("入场")]
    [SerializeField] private bool fadeOnEnter = true;
    [SerializeField] private bool moveOnEnter = true;
    [SerializeField] private Vector2 enterOffset = new Vector2(0f, 42f);
    [SerializeField] private bool scaleOnEnter = true;
    [SerializeField] private float enterStartScale = 0.9f;
    [SerializeField] private float enterDuration = 0.28f;
    [SerializeField] private Ease enterEase = Ease.OutBack;

    [Header("离场")]
    [SerializeField] private bool fadeOnExit = true;
    [SerializeField] private bool moveOnExit = true;
    [SerializeField] private Vector2 exitOffset = new Vector2(0f, -28f);
    [SerializeField] private bool scaleOnExit = true;
    [SerializeField] private float exitTargetScale = 0.92f;
    [SerializeField] private float exitDuration = 0.2f;
    [SerializeField] private Ease exitEase = Ease.InBack;

    [Header("运行时")]
    [SerializeField] private bool useUnscaledTime = true;

    private Tween currentTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private bool cached;
    private bool defaultInteractable;
    private bool defaultBlocksRaycasts;
    private bool defaultInteractionCached;

    private void Awake()
    {
        EnsureReferences();
        if (applyPresetOnAwake)
        {
            ApplyPreset();
        }

        CacheDefaults();
        CacheInteractionDefaults();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureReferences();
            ApplyPreset();
        }
    }

    private void OnDestroy()
    {
        Kill();
    }

    public void ApplyPreset()
    {
        switch (preset)
        {
            case UIRevealPreset.Title:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 52f);
                scaleOnEnter = false;
                enterStartScale = 1f;
                enterDuration = 0.34f;
                enterEase = Ease.OutCubic;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -26f);
                scaleOnExit = false;
                exitTargetScale = 1f;
                exitDuration = 0.22f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.HeroTitle:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 72f);
                scaleOnEnter = true;
                enterStartScale = 0.84f;
                enterDuration = 0.42f;
                enterEase = Ease.OutExpo;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -42f);
                scaleOnExit = true;
                exitTargetScale = 0.9f;
                exitDuration = 0.26f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.PrimaryButton:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 30f);
                scaleOnEnter = true;
                enterStartScale = 0.86f;
                enterDuration = 0.24f;
                enterEase = Ease.OutBack;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -18f);
                scaleOnExit = true;
                exitTargetScale = 0.9f;
                exitDuration = 0.16f;
                exitEase = Ease.InBack;
                break;
            case UIRevealPreset.SecondaryButton:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 24f);
                scaleOnEnter = true;
                enterStartScale = 0.9f;
                enterDuration = 0.22f;
                enterEase = Ease.OutCubic;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -16f);
                scaleOnExit = true;
                exitTargetScale = 0.94f;
                exitDuration = 0.15f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.FloatingAction:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 58f);
                scaleOnEnter = true;
                enterStartScale = 0.72f;
                enterDuration = 0.28f;
                enterEase = Ease.OutBack;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -34f);
                scaleOnExit = true;
                exitTargetScale = 0.82f;
                exitDuration = 0.18f;
                exitEase = Ease.InBack;
                break;
            case UIRevealPreset.Panel:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 40f);
                scaleOnEnter = true;
                enterStartScale = 0.92f;
                enterDuration = 0.3f;
                enterEase = Ease.OutCubic;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -24f);
                scaleOnExit = true;
                exitTargetScale = 0.94f;
                exitDuration = 0.2f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.Modal:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 64f);
                scaleOnEnter = true;
                enterStartScale = 0.82f;
                enterDuration = 0.32f;
                enterEase = Ease.OutBack;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -36f);
                scaleOnExit = true;
                exitTargetScale = 0.86f;
                exitDuration = 0.2f;
                exitEase = Ease.InBack;
                break;
            case UIRevealPreset.Card:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 34f);
                scaleOnEnter = true;
                enterStartScale = 0.88f;
                enterDuration = 0.26f;
                enterEase = Ease.OutBack;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -22f);
                scaleOnExit = true;
                exitTargetScale = 0.9f;
                exitDuration = 0.18f;
                exitEase = Ease.InBack;
                break;
            case UIRevealPreset.Tooltip:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(0f, 18f);
                scaleOnEnter = true;
                enterStartScale = 0.94f;
                enterDuration = 0.18f;
                enterEase = Ease.OutQuad;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(0f, -12f);
                scaleOnExit = true;
                exitTargetScale = 0.96f;
                exitDuration = 0.12f;
                exitEase = Ease.InQuad;
                break;
            case UIRevealPreset.Notification:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(64f, 0f);
                scaleOnEnter = true;
                enterStartScale = 0.9f;
                enterDuration = 0.24f;
                enterEase = Ease.OutCubic;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(72f, 0f);
                scaleOnExit = true;
                exitTargetScale = 0.92f;
                exitDuration = 0.16f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.Badge:
                fadeOnEnter = true;
                moveOnEnter = false;
                enterOffset = Vector2.zero;
                scaleOnEnter = true;
                enterStartScale = 0.68f;
                enterDuration = 0.2f;
                enterEase = Ease.OutBack;

                fadeOnExit = true;
                moveOnExit = false;
                exitOffset = Vector2.zero;
                scaleOnExit = true;
                exitTargetScale = 0.78f;
                exitDuration = 0.12f;
                exitEase = Ease.InBack;
                break;
            case UIRevealPreset.Sidebar:
                fadeOnEnter = true;
                moveOnEnter = true;
                enterOffset = new Vector2(-96f, 0f);
                scaleOnEnter = true;
                enterStartScale = 0.94f;
                enterDuration = 0.34f;
                enterEase = Ease.OutCubic;

                fadeOnExit = true;
                moveOnExit = true;
                exitOffset = new Vector2(-72f, 0f);
                scaleOnExit = true;
                exitTargetScale = 0.96f;
                exitDuration = 0.22f;
                exitEase = Ease.InCubic;
                break;
            case UIRevealPreset.Custom:
            default:
                break;
        }
    }

    public void PrepareEnter()
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();
        gameObject.SetActive(true);

        canvasGroup.alpha = fadeOnEnter ? 0f : 1f;
        RestoreInteractionState();

        targetRect.anchoredPosition = moveOnEnter ? defaultAnchoredPosition + GetScaledEnterOffset() : defaultAnchoredPosition;
        targetRect.localScale = scaleOnEnter ? defaultScale * GetScaledEnterStartScale() : defaultScale;
    }

    public Tween PlayEnter(float delay = 0f)
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();
        gameObject.SetActive(true);
        RestoreInteractionState();

        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);

        if (fadeOnEnter)
        {
            sequence.Join(canvasGroup.DOFade(1f, enterDuration));
        }

        if (moveOnEnter)
        {
            sequence.Join(targetRect.DOAnchorPos(defaultAnchoredPosition, enterDuration));
        }

        if (scaleOnEnter)
        {
            sequence.Join(targetRect.DOScale(defaultScale, enterDuration));
        }

        sequence.SetEase(enterEase);
        sequence.OnComplete(RestoreInteractionState);

        currentTween = sequence;
        return currentTween;
    }

    public Tween PlayExit(float delay = 0f)
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();
        RestoreInteractionState();

        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime).SetDelay(delay);

        if (fadeOnExit)
        {
            sequence.Join(canvasGroup.DOFade(0f, exitDuration));
        }

        if (moveOnExit)
        {
            sequence.Join(targetRect.DOAnchorPos(defaultAnchoredPosition + GetScaledExitOffset(), exitDuration));
        }

        if (scaleOnExit)
        {
            sequence.Join(targetRect.DOScale(defaultScale * GetScaledExitTargetScale(), exitDuration));
        }

        sequence.SetEase(exitEase);
        currentTween = sequence;
        return currentTween;
    }

    public void CompleteImmediate()
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();
        gameObject.SetActive(true);

        canvasGroup.alpha = 1f;
        RestoreInteractionState();

        targetRect.anchoredPosition = defaultAnchoredPosition;
        targetRect.localScale = defaultScale;
    }

    public void SetHiddenImmediate()
    {
        EnsureReferences();
        CacheDefaults();
        CacheInteractionDefaults();
        Kill();

        canvasGroup.alpha = 0f;
        RestoreInteractionState();

        targetRect.anchoredPosition = moveOnEnter ? defaultAnchoredPosition + GetScaledEnterOffset() : defaultAnchoredPosition;
        targetRect.localScale = scaleOnEnter ? defaultScale * GetScaledEnterStartScale() : defaultScale;
    }

    public void Kill()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    private void EnsureReferences()
    {
        targetRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void CacheDefaults()
    {
        if (cached || targetRect == null)
        {
            return;
        }

        defaultAnchoredPosition = targetRect.anchoredPosition;
        defaultScale = targetRect.localScale;
        cached = true;
    }

    private void CacheInteractionDefaults()
    {
        if (defaultInteractionCached || canvasGroup == null)
        {
            return;
        }

        defaultInteractable = canvasGroup.interactable;
        defaultBlocksRaycasts = canvasGroup.blocksRaycasts;
        defaultInteractionCached = true;
    }

    private void RestoreInteractionState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = defaultInteractable;
        canvasGroup.blocksRaycasts = defaultBlocksRaycasts;
    }

    private Vector2 GetScaledEnterOffset()
    {
        return enterOffset * motionIntensity;
    }

    private Vector2 GetScaledExitOffset()
    {
        return exitOffset * motionIntensity;
    }

    private float GetScaledEnterStartScale()
    {
        return 1f + ((enterStartScale - 1f) * motionIntensity);
    }

    private float GetScaledExitTargetScale()
    {
        return 1f + ((exitTargetScale - 1f) * motionIntensity);
    }
}
