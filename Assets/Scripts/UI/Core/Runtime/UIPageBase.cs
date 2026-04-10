using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 所有 UI 页面公共基类：负责页面壳体状态、开关过渡、内容导演自动接入以及关闭等待管线。
/// 子类通常只需要关心业务绑定和少量特殊动画扩展。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPageBase : MonoBehaviour, IUIPage
{
    private UISequenceDirector sequenceDirector;
    [SerializeField] private bool autoPlaySequenceDirector = true;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Tween transitionTween;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private bool transformCached;

    private string instanceId = string.Empty;
    private bool isVisible;
    private bool closeWaitRunning;
    private bool closeWaitCompleted;
    private System.Action closeWaitCallbacks;

    public System.Type PageType => GetType();
    public string InstanceId => instanceId;
    public bool IsVisible => isVisible;

    protected UISequenceDirector SequenceDirector => sequenceDirector;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        ResolveSequenceDirector();
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
        ResetCloseWaitState();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        isVisible = true;
        ResetTransformState();
        PrepareContentForOpen();
        OnPageOpened(context);
        ApplyActivationState(visualActive: true, inputActive: true);
    }

    public void HandleClose()
    {
        ValidateCanvasGroup();
        KillTransition();
        sequenceDirector?.Kill();
        sequenceDirector?.SetHiddenImmediate();
        OnPageClosed();
        isVisible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
        ResetTransformState();
        ResetCloseWaitState();
        gameObject.SetActive(false);
    }

    public void HandleActivationChanged(bool visualActive, bool inputActive)
    {
        ValidateCanvasGroup();
        ApplyActivationState(visualActive, inputActive);
        OnActivationChanged(visualActive, inputActive);
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
            PlayContentEnter();
            return;
        }

        ApplyTransitionStartState(transitionSettings);
        transitionTween = CreateTransitionTween(transitionSettings, true, useUnscaledTime, null);
        PlayContentEnter();
    }

    public void PlayCloseTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime, System.Action onCompleted)
    {
        ValidateCanvasGroup();
        CacheDefaultTransform();
        KillTransition();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RunCloseWaitPipeline(useUnscaledTime, () => StartCloseTransition(transitionSettings, useUnscaledTime, onCompleted));
    }

    protected void RunCloseWaitPipeline(bool useUnscaledTime, System.Action onCompleted)
    {
        if (closeWaitCompleted)
        {
            onCompleted?.Invoke();
            return;
        }

        closeWaitCallbacks += onCompleted;
        if (closeWaitRunning)
        {
            return;
        }

        closeWaitRunning = true;
        int pendingCount = 0;

        void MarkCompleted()
        {
            pendingCount--;
            if (pendingCount <= 0)
            {
                CompleteCloseWaitPipeline();
            }
        }

        if (ShouldAutoPlaySequenceDirector() && sequenceDirector != null)
        {
            pendingCount++;
            sequenceDirector.PlayExit(MarkCompleted);
        }

        if (HasAdditionalCloseWaitActions())
        {
            pendingCount++;
            PlayAdditionalCloseWaitActions(useUnscaledTime, MarkCompleted);
        }

        if (pendingCount == 0)
        {
            CompleteCloseWaitPipeline();
        }
    }

    protected virtual bool HasAdditionalCloseWaitActions()
    {
        return false;
    }

    protected virtual void PlayAdditionalCloseWaitActions(bool useUnscaledTime, System.Action onCompleted)
    {
        onCompleted?.Invoke();
    }

    protected virtual void OnPageOpened(UIPageOpenContext context)
    {
    }

    protected virtual void OnPageClosed()
    {
    }

    protected virtual void OnActivationChanged(bool visualActive, bool inputActive)
    {
    }

    protected virtual void OnPageTick(float deltaTime)
    {
    }

    protected virtual bool ShouldAutoPlaySequenceDirector()
    {
        return autoPlaySequenceDirector;
    }

    private void StartCloseTransition(UIPageTransitionSettings transitionSettings, bool useUnscaledTime, System.Action onCompleted)
    {
        if (transitionSettings == null || transitionSettings.transitionType == UITransitionType.None || transitionSettings.duration <= 0f)
        {
            onCompleted?.Invoke();
            return;
        }

        ResetTransformState();
        transitionTween = CreateTransitionTween(transitionSettings, false, useUnscaledTime, onCompleted);
    }

    private void PrepareContentForOpen()
    {
        if (!ShouldAutoPlaySequenceDirector() || sequenceDirector == null)
        {
            return;
        }

        sequenceDirector.SetHiddenImmediate();
    }

    private void PlayContentEnter()
    {
        if (!ShouldAutoPlaySequenceDirector() || sequenceDirector == null)
        {
            return;
        }

        sequenceDirector.PlayEnter();
    }

    private void CompleteCloseWaitPipeline()
    {
        closeWaitRunning = false;
        closeWaitCompleted = true;

        System.Action callbacks = closeWaitCallbacks;
        closeWaitCallbacks = null;
        callbacks?.Invoke();
    }

    private void ResetCloseWaitState()
    {
        closeWaitRunning = false;
        closeWaitCompleted = false;
        closeWaitCallbacks = null;
    }

    private void ResolveSequenceDirector()
    {
        if (sequenceDirector != null)
        {
            return;
        }

        sequenceDirector = GetComponentInChildren<UISequenceDirector>(true);
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

    private void ApplyActivationState(bool visualActive, bool inputActive)
    {
        if (!visualActive)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = inputActive;
        canvasGroup.blocksRaycasts = inputActive;
    }

    private void ValidateCanvasGroup()
    {
        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"UIPage '{name}' is missing CanvasGroup reference.");
        }
    }
}
