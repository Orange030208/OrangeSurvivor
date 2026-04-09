using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPageBase : MonoBehaviour, IUIPage
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Tween transitionTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private bool transformCached;

    private string instanceId = string.Empty;
    private bool isVisible;

    public System.Type PageType => GetType();
    public string InstanceId => instanceId;
    public bool IsVisible => isVisible;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        CacheDefaultTransform();
    }

    public void SetupInstance(string newInstanceId)
    {
        if (string.IsNullOrWhiteSpace(newInstanceId))
        {
            throw new System.ArgumentException("SetupInstance failed: newInstanceId is null or empty.", nameof(newInstanceId));
        }

        instanceId = newInstanceId;
    }

    public void HandleOpen(UIPageOpenContext context)
    {
        ValidateCanvasGroup();
        CacheDefaultTransform();
        KillTransition();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isVisible = true;
        ResetTransformState();
        OnPageOpened(context);
    }

    public void HandleClose()
    {
        ValidateCanvasGroup();
        KillTransition();
        OnPageClosed();
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        ResetTransformState();
        gameObject.SetActive(false);
    }

    public void HandleFocusChanged(bool hasFocus)
    {
        ValidateCanvasGroup();
        canvasGroup.interactable = hasFocus;
        canvasGroup.blocksRaycasts = hasFocus;
        OnFocusChanged(hasFocus);
    }

    public void HandleTick(float deltaTime)
    {
        if (!isVisible)
        {
            return;
        }

        OnPageTick(deltaTime);
    }

    public void PlayOpenTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime)
    {
        ValidateCanvasGroup();
        CacheDefaultTransform();
        KillTransition();
        ResetTransformState();

        if (transitionSettings == null || transitionSettings.transitionType == UITransitionType.None || transitionSettings.duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            return;
        }

        ApplyTransitionStartState(transitionSettings);
        transitionTween = CreateTransitionTween(transitionSettings, true, useUnscaledTime, null);
    }

    public void PlayCloseTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime, System.Action onCompleted)
    {
        ValidateCanvasGroup();
        CacheDefaultTransform();
        KillTransition();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (transitionSettings == null || transitionSettings.transitionType == UITransitionType.None || transitionSettings.duration <= 0f)
        {
            onCompleted?.Invoke();
            return;
        }

        ResetTransformState();
        transitionTween = CreateTransitionTween(transitionSettings, false, useUnscaledTime, onCompleted);
    }

    protected virtual void OnPageOpened(UIPageOpenContext context)
    {
    }

    protected virtual void OnPageClosed()
    {
    }

    protected virtual void OnFocusChanged(bool hasFocus)
    {
    }

    protected virtual void OnPageTick(float deltaTime)
    {
    }

    private void CacheDefaultTransform()
    {
        if (transformCached)
        {
            return;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform == null)
        {
            return;
        }

        defaultAnchoredPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
        transformCached = true;
    }

    private void ResetTransformState()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = defaultAnchoredPosition;
        rectTransform.localScale = defaultScale;
    }

    private void ApplyTransitionStartState(UIPageTransitionSettings transitionSettings)
    {
        if (transitionSettings.fade)
        {
            canvasGroup.alpha = 0f;
        }

        if (rectTransform == null)
        {
            return;
        }

        switch (transitionSettings.transitionType)
        {
            case UITransitionType.SlideFromLeft:
                rectTransform.anchoredPosition = defaultAnchoredPosition + Vector2.left * transitionSettings.offset;
                break;
            case UITransitionType.SlideFromRight:
                rectTransform.anchoredPosition = defaultAnchoredPosition + Vector2.right * transitionSettings.offset;
                break;
            case UITransitionType.SlideFromTop:
                rectTransform.anchoredPosition = defaultAnchoredPosition + Vector2.up * transitionSettings.offset;
                break;
            case UITransitionType.SlideFromBottom:
                rectTransform.anchoredPosition = defaultAnchoredPosition + Vector2.down * transitionSettings.offset;
                break;
            case UITransitionType.Scale:
                rectTransform.localScale = defaultScale * transitionSettings.startScale;
                break;
        }
    }

    private Tween CreateTransitionTween(UIPageTransitionSettings transitionSettings, bool isOpening, bool useUnscaledTime, System.Action onCompleted)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

        if (transitionSettings.fade)
        {
            sequence.Join(canvasGroup.DOFade(isOpening ? 1f : 0f, transitionSettings.duration));
        }

        if (rectTransform != null)
        {
            switch (transitionSettings.transitionType)
            {
                case UITransitionType.SlideFromLeft:
                case UITransitionType.SlideFromRight:
                case UITransitionType.SlideFromTop:
                case UITransitionType.SlideFromBottom:
                    Vector2 targetPosition = isOpening
                        ? defaultAnchoredPosition
                        : GetClosingTargetPosition(transitionSettings);
                    sequence.Join(rectTransform.DOAnchorPos(targetPosition, transitionSettings.duration));
                    break;
                case UITransitionType.Scale:
                    Vector3 targetScale = isOpening ? defaultScale : defaultScale * transitionSettings.startScale;
                    sequence.Join(rectTransform.DOScale(targetScale, transitionSettings.duration));
                    break;
            }
        }

        sequence.SetEase(transitionSettings.ease);
        sequence.OnComplete(() =>
        {
            if (isOpening)
            {
                ResetTransformState();
                canvasGroup.alpha = 1f;
            }

            onCompleted?.Invoke();
        });

        return sequence;
    }

    private Vector2 GetClosingTargetPosition(UIPageTransitionSettings transitionSettings)
    {
        return transitionSettings.transitionType switch
        {
            UITransitionType.SlideFromLeft => defaultAnchoredPosition + Vector2.left * transitionSettings.offset,
            UITransitionType.SlideFromRight => defaultAnchoredPosition + Vector2.right * transitionSettings.offset,
            UITransitionType.SlideFromTop => defaultAnchoredPosition + Vector2.up * transitionSettings.offset,
            UITransitionType.SlideFromBottom => defaultAnchoredPosition + Vector2.down * transitionSettings.offset,
            _ => defaultAnchoredPosition
        };
    }

    private void KillTransition()
    {
        transitionTween?.Kill();
        transitionTween = null;
    }

    private void ValidateCanvasGroup()
    {
        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"UIPage '{name}' is missing CanvasGroup reference.");
        }
    }
}
