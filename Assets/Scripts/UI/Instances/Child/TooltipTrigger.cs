using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class TooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private static readonly Vector2 DEFAULT_OFFSET = new Vector2(18f, -18f);
    private const float DEFAULT_MARGIN = 12f;
    private const float DEFAULT_HOVER_DELAY = 0.05f;
    private const float DEFAULT_LONG_PRESS_DELAY = 0.45f;

    [SerializeField] private MonoBehaviour contentSourceComponent;
    [SerializeField] private TooltipTriggerProfileSO profile;
    [SerializeField] private bool allowPin;
    [SerializeField] private bool allowInteractiveTransient;
    [SerializeField] private string viewIdOverride;

    private object contentSource;
    private UIManager uiManager;
    private TooltipSessionHandle currentSession;
    private TooltipHoverArea currentHoverArea;
    private CancellationTokenSource showCts;
    private CancellationTokenSource closeCts;
    private bool pointerInsideSource;
    private bool pointerInsideTooltip;
    private bool pointerDown;
    private Vector2 latestScreenPosition;

    private void Awake()
    {
        if (contentSourceComponent != null)
        {
            contentSource = contentSourceComponent;
        }
    }

    private void OnDisable()
    {
        CancelPendingShow();
        CloseCurrentSession();
        pointerInsideSource = false;
        pointerInsideTooltip = false;
        pointerDown = false;
    }

    private void OnDestroy()
    {
        CancelPendingShow();
        CancelPendingClose();
    }

    public void SetContentSource(object source)
    {
        contentSource = source;
        if (source is MonoBehaviour behaviour)
        {
            contentSourceComponent = behaviour;
        }
    }

    public void ConfigureOwner(UIManager manager)
    {
        uiManager = manager;
    }

    public void Configure(
        object source,
        UIManager manager,
        bool canPin = false,
        bool interactiveTransient = false)
    {
        SetContentSource(source);
        uiManager = manager;
        allowPin = canPin;
        allowInteractiveTransient = interactiveTransient;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInsideSource = true;
        latestScreenPosition = eventData.position;
        CancelPendingClose();

        if (IsHoverEnabled() && IsMousePointer(eventData))
        {
            ScheduleShow(latestScreenPosition, ResolveHoverDelay());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInsideSource = false;
        if (!pointerDown && !pointerInsideTooltip)
        {
            CancelPendingShow();
            ScheduleCloseIfOutside();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        latestScreenPosition = eventData.position;
        if (currentSession.IsValid)
        {
            currentSession.UpdatePosition(latestScreenPosition);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        latestScreenPosition = eventData.position;

        if (IsLongPressEnabled() && !IsMousePointer(eventData))
        {
            ScheduleShow(latestScreenPosition, ResolveLongPressDelay());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
        CancelPendingShow();

        if ((!pointerInsideSource && !pointerInsideTooltip) || !IsInteractiveRequest())
        {
            CloseCurrentSession();
        }
    }

    private void ScheduleShow(Vector2 screenPosition, float delay)
    {
        CancelPendingShow();
        showCts = new CancellationTokenSource();
        ShowAfterDelayAsync(screenPosition, delay, showCts.Token).Forget();
    }

    private async UniTaskVoid ShowAfterDelayAsync(Vector2 screenPosition, float delay, CancellationToken cancellationToken)
    {
        try
        {
            if (delay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delay),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            await ShowAsync(screenPosition, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private async UniTask ShowAsync(Vector2 screenPosition, CancellationToken cancellationToken)
    {
        UIManager manager = ResolveUIManager();
        object source = ResolveContentSource();
        if (manager == null || source == null)
        {
            return;
        }

        TooltipRequest request = new TooltipRequest(
            source: source,
            placementOptions: CreatePlacement(screenPosition),
            pinMode: ResolvePinMode(),
            chromeOptions: ResolveChromeOptions(),
            viewIdOverride: viewIdOverride,
            sessionMode: TooltipSessionMode.Transient);

        currentSession = await manager.ShowTooltipAsync(request, cancellationToken);
        BindTooltipHoverArea(currentSession);
        if (!pointerInsideSource && !pointerDown && !pointerInsideTooltip && !IsInteractiveRequest())
        {
            await currentSession.CloseAsync(CloseReason.Cancel);
        }
    }

    private TooltipPlacementOptions CreatePlacement(Vector2 screenPosition)
    {
        if (profile != null)
        {
            return profile.CreatePlacement(screenPosition);
        }

        return new TooltipPlacementOptions(
            screenPosition: screenPosition,
            offset: DEFAULT_OFFSET,
            followPointer: true,
            margin: DEFAULT_MARGIN,
            preferredAnchor: FloatingViewAnchor.BottomRight,
            useScreenPosition: true);
    }

    private TooltipChromeOptions ResolveChromeOptions()
    {
        bool canPin = allowPin || profile != null && profile.AllowPin;
        bool interactive = allowInteractiveTransient || profile != null && profile.AllowInteractiveTransient;
        return new TooltipChromeOptions(
            allowUserPin: canPin,
            showCloseButton: false,
            allowInteractiveTransient: interactive);
    }

    private TooltipPinMode ResolvePinMode()
    {
        return allowPin || profile != null && profile.AllowPin
            ? TooltipPinMode.UserOptional
            : TooltipPinMode.Disabled;
    }

    private bool IsInteractiveRequest()
    {
        return allowPin ||
               allowInteractiveTransient ||
               profile != null && (profile.AllowPin || profile.AllowInteractiveTransient);
    }

    private bool IsHoverEnabled()
    {
        TooltipTriggerMode mode = profile != null ? profile.TriggerMode : TooltipTriggerMode.HoverAndLongPress;
        return (mode & TooltipTriggerMode.Hover) != 0;
    }

    private bool IsLongPressEnabled()
    {
        TooltipTriggerMode mode = profile != null ? profile.TriggerMode : TooltipTriggerMode.HoverAndLongPress;
        return (mode & TooltipTriggerMode.LongPress) != 0;
    }

    private float ResolveHoverDelay()
    {
        return profile != null ? profile.HoverDelay : DEFAULT_HOVER_DELAY;
    }

    private float ResolveLongPressDelay()
    {
        return profile != null ? profile.LongPressDelay : DEFAULT_LONG_PRESS_DELAY;
    }

    private object ResolveContentSource()
    {
        if (contentSource != null)
        {
            return contentSource;
        }

        if (contentSourceComponent != null)
        {
            return contentSourceComponent;
        }

        ITooltipContentSource tooltipContentSource = GetComponent<ITooltipContentSource>();
        if (tooltipContentSource != null)
        {
            return tooltipContentSource;
        }

        IInfoDocumentSource infoDocumentSource = GetComponent<IInfoDocumentSource>();
        return infoDocumentSource;
    }

    private UIManager ResolveUIManager()
    {
        return uiManager != null ? uiManager : UIManager.Instance;
    }

    private void CancelPendingShow()
    {
        if (showCts == null)
        {
            return;
        }

        showCts.Cancel();
        showCts.Dispose();
        showCts = null;
    }

    private void ScheduleCloseIfOutside()
    {
        if (!IsInteractiveRequest())
        {
            CloseCurrentSession();
            return;
        }

        CancelPendingClose();
        closeCts = new CancellationTokenSource();
        CloseIfOutsideNextFrameAsync(closeCts).Forget();
    }

    private async UniTaskVoid CloseIfOutsideNextFrameAsync(CancellationTokenSource cancellationSource)
    {
        try
        {
            CancellationToken cancellationToken = cancellationSource.Token;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (!pointerInsideSource && !pointerInsideTooltip && !pointerDown)
            {
                CloseCurrentSession();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(closeCts, cancellationSource))
            {
                closeCts.Dispose();
                closeCts = null;
            }
        }
    }

    private void CancelPendingClose()
    {
        if (closeCts == null)
        {
            return;
        }

        closeCts.Cancel();
        closeCts.Dispose();
        closeCts = null;
    }

    private void CloseCurrentSession()
    {
        if (!currentSession.IsValid)
        {
            return;
        }

        UnbindTooltipHoverArea();
        CancelPendingClose();
        currentSession.CloseAsync(CloseReason.Normal).Forget();
        currentSession = default;
    }

    private void BindTooltipHoverArea(TooltipSessionHandle session)
    {
        UnbindTooltipHoverArea();
        if (!session.IsValid || session.ViewHandle.View == null)
        {
            return;
        }

        currentHoverArea = session.ViewHandle.View.GetComponent<TooltipHoverArea>();
        if (currentHoverArea == null)
        {
            currentHoverArea = session.ViewHandle.View.GetComponentInChildren<TooltipHoverArea>(true);
        }

        if (currentHoverArea != null)
        {
            currentHoverArea.HoverChanged += OnTooltipHoverChanged;
        }
    }

    private void UnbindTooltipHoverArea()
    {
        if (currentHoverArea != null)
        {
            currentHoverArea.HoverChanged -= OnTooltipHoverChanged;
            currentHoverArea = null;
        }

        pointerInsideTooltip = false;
    }

    private void OnTooltipHoverChanged(bool hovering)
    {
        pointerInsideTooltip = hovering;
        if (hovering)
        {
            CancelPendingClose();
        }

        if (!hovering && !pointerInsideSource && !pointerDown)
        {
            ScheduleCloseIfOutside();
        }
    }

    private static bool IsMousePointer(PointerEventData eventData)
    {
        return eventData != null && eventData.pointerId < 0;
    }
}
