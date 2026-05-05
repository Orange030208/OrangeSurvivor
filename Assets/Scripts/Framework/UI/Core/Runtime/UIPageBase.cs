using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using Orange.UIFramework;
    using UnityEngine;
    using UnityEngine.UI;

/// <summary>
/// 所有 UI 页面公共基类：负责页面壳体状态、内容导演自动接入以及关闭等待管线。
/// 页面视觉入场/退场统一交给 UISequenceDirector，页面基类不再维护额外的壳体 tween 过渡。
/// 语义约定：
/// - PlayOpenTransition：仅触发 enter，不等待 enter 完成。
/// - PlayCloseTransition：会等待关闭等待管线完成；若启用了 UISequenceDirector，会等待 exit 完成后再回调。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPageBase : PageBase, IUIPage
{
    private UISequenceDirector sequenceDirector;
    [SerializeField] private bool autoPlaySequenceDirector = true;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private bool transformCached;

    private string instanceId = string.Empty;
    private bool isVisible;
    private bool closeWaitRunning;
    private bool closeWaitCompleted;
    private System.Action closeWaitCallbacks;

    public System.Type PageType => GetType();
    public new string InstanceId => string.IsNullOrWhiteSpace(instanceId) ? base.InstanceId : instanceId;
    public bool IsVisible => isVisible;

    protected UISequenceDirector SequenceDirector => sequenceDirector;

    protected override void Awake()
    {
        base.Awake();
        ResolveLegacyReferences();
        CacheDefaultTransform();
        ValidateRectTransform();
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
        ResolveLegacyReferences();
        ValidateCanvasGroup();
        CacheDefaultTransform();
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
        ResolveLegacyReferences();
        ValidateCanvasGroup();
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
        ResolveLegacyReferences();
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

    public void PlayOpenTransition(bool useUnscaledTime)
    {
        ResolveLegacyReferences();
        ValidateCanvasGroup();
        CacheDefaultTransform();
        ResetTransformState();
        canvasGroup.alpha = 1f;
        PlayContentEnter();
    }

    public void PlayCloseTransition(bool useUnscaledTime, System.Action onCompleted)
    {
        ResolveLegacyReferences();
        ValidateCanvasGroup();
        CacheDefaultTransform();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RunCloseWaitPipeline(useUnscaledTime, () =>
        {
            ResetTransformState();
            onCompleted?.Invoke();
        });
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

        if (ShouldAutoPlaySequenceDirector())
        {
            pendingCount++;
            Tween exitTween = sequenceDirector.PlayExit();
            if (exitTween == null)
            {
                MarkCompleted();
            }
            else
            {
                bool exitCompleted = false;

                void CompleteExitOnce()
                {
                    if (exitCompleted)
                    {
                        return;
                    }

                    exitCompleted = true;
                    MarkCompleted();
                }

                exitTween.OnComplete(CompleteExitOnce);
                exitTween.OnKill(CompleteExitOnce);
            }
        }

        if (HasAdditionalCloseWaitActions())
        {
            pendingCount++;
            PlayAdditionalCloseWaitActions(useUnscaledTime, MarkCompleted);
        }

        if (pendingCount <= 0)
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

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UIPageOpenContext legacyContext = new UIPageOpenContext(context.ViewType, context.InstanceId, context.Payload);
        SetupInstance(context.InstanceId);
        HandleOpen(legacyContext);
        PlayOpenTransition(useUnscaledTime: true);
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
        PlayCloseTransition(useUnscaledTime: true, () => completionSource.TrySetResult());
        return completionSource.Task;
    }

    protected override void OnClosed(CloseReason reason)
    {
        HandleClose();
    }

    protected override void OnInputChanged(bool interactable, bool blocksRaycasts)
    {
        OnActivationChanged(isVisible, interactable && blocksRaycasts);
    }

    protected override void OnTick(float deltaTime)
    {
        OnPageTick(deltaTime);
    }

    protected virtual bool ShouldAutoPlaySequenceDirector()
    {
        return autoPlaySequenceDirector && sequenceDirector != null;
    }

    private void PrepareContentForOpen()
    {
        if (!ShouldAutoPlaySequenceDirector())
        {
            return;
        }

        sequenceDirector.SetHiddenImmediate();
    }

    private void PlayContentEnter()
    {
        if (!ShouldAutoPlaySequenceDirector())
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

    private void ResolveLegacyReferences()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        ResolveSequenceDirector();
    }

    private void CacheDefaultTransform()
    {
        ResolveLegacyReferences();
        ValidateRectTransform();

        if (transformCached)
        {
            return;
        }

        defaultAnchoredPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
        transformCached = true;
    }

    private void ResetTransformState()
    {
        ResolveLegacyReferences();
        ValidateRectTransform();

        rectTransform.anchoredPosition = defaultAnchoredPosition;
        rectTransform.localScale = defaultScale;
    }

    private void ApplyActivationState(bool visualActive, bool inputActive)
    {
        ResolveLegacyReferences();
        ValidateCanvasGroup();

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
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"UIPage '{name}' is missing CanvasGroup reference.");
        }
    }

    private void ValidateRectTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform == null)
        {
            throw new MissingComponentException($"UIPage '{name}' requires RectTransform.");
        }
    }
}
}
