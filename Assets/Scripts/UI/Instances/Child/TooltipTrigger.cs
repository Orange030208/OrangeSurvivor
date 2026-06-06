using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[DisallowMultipleComponent]
public sealed class TooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Tooltip("可选的显式内容源组件。留空时会在当前物体上查找实现了 ITooltipContentSource 的组件。")]
    [SerializeField] private MonoBehaviour contentSourceComponent;

    [Tooltip("控制哪些指针交互会触发 Tooltip。Hover 用于鼠标悬停，LongPress 用于触屏长按。")]
    [SerializeField] private TooltipTriggerMode triggerMode = TooltipTriggerMode.Hover;

    [Tooltip("鼠标悬停后延迟多久显示 Tooltip，单位为秒。")]
    [Min(0f)]
    [SerializeField] private float hoverDelay = 0.05f;

    [Tooltip("长按后延迟多久显示 Tooltip，单位为秒。")]
    [Min(0f)]
    [SerializeField] private float longPressDelay = 0.45f;

    [Tooltip("Tooltip 显示后是否持续跟随当前指针位置。")]
    [SerializeField] private bool followPointer = true;

    [Tooltip("Tooltip 相对指针锚点的屏幕空间偏移量。")]
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    [Tooltip("Tooltip 贴边时保留的最小屏幕边距。")]
    [Min(0f)]
    [SerializeField] private float margin = 12f;

    [Tooltip("Tooltip 相对指针优先采用的展开方向。")]
    [SerializeField] private FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight;

    [Tooltip("启用后，若 Tooltip 视图支持固定操作，则允许用户将其固定。")]
    [SerializeField] private bool allowPin;

    [Tooltip("启用后，指针在触发源和 Tooltip 本体之间移动时不会立即关闭 Tooltip。")]
    [SerializeField] private bool allowInteractiveTransient;

    private ITooltipContentSource contentSource;
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
            contentSource = contentSourceComponent as ITooltipContentSource;
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

    public void SetContentSource(ITooltipContentSource source)
    {
        contentSource = source;
        if (source is MonoBehaviour behaviour)
        {
            contentSourceComponent = behaviour;
        }
        else if (source == null)
        {
            contentSourceComponent = null;
        }
    }

    public void Configure(
        ITooltipContentSource source,
        bool canPin = false,
        bool interactiveTransient = false)
    {
        SetContentSource(source);
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
        ITooltipContentSource source = ResolveContentSource();
        if (source == null)
        {
            return;
        }

        if (!source.TryBuildTooltipContent(out TooltipContent content) || content == null)
        {
            Debug.LogWarning(
                $"{nameof(TooltipTrigger)} '{name}' could not build tooltip content from source '{source.GetType().Name}'.",
                this);
            return;
        }

        TooltipRequest request = new TooltipRequest(
            content: content,
            placementOptions: CreatePlacement(screenPosition),
            pinMode: ResolvePinMode(),
            chromeOptions: ResolveChromeOptions(),
            sessionMode: TooltipSessionMode.Transient);

        currentSession = await UIManager.Instance.ShowTooltipAsync(request, cancellationToken);
        BindTooltipHoverArea(currentSession);
        if (!pointerInsideSource && !pointerDown && !pointerInsideTooltip && !IsInteractiveRequest())
        {
            await currentSession.CloseAsync(CloseReason.Cancel);
        }
    }

    private TooltipPlacementOptions CreatePlacement(Vector2 screenPosition)
    {
        return new TooltipPlacementOptions(
            screenPosition: screenPosition,
            offset: offset,
            followPointer: followPointer,
            margin: margin,
            preferredAnchor: preferredAnchor,
            useScreenPosition: true);
    }

    private TooltipChromeOptions ResolveChromeOptions()
    {
        return new TooltipChromeOptions(
            allowUserPin: allowPin,
            showCloseButton: false,
            allowInteractiveTransient: allowInteractiveTransient);
    }

    private TooltipPinMode ResolvePinMode()
    {
        return allowPin
            ? TooltipPinMode.UserOptional
            : TooltipPinMode.Disabled;
    }

    private bool IsInteractiveRequest()
    {
        return allowPin || allowInteractiveTransient;
    }

    private bool IsHoverEnabled()
    {
        return (triggerMode & TooltipTriggerMode.Hover) != 0;
    }

    private bool IsLongPressEnabled()
    {
        return (triggerMode & TooltipTriggerMode.LongPress) != 0;
    }

    private float ResolveHoverDelay()
    {
        return hoverDelay;
    }

    private float ResolveLongPressDelay()
    {
        return longPressDelay;
    }

    private ITooltipContentSource ResolveContentSource()
    {
        if (contentSource != null)
        {
            return contentSource;
        }

        if (contentSourceComponent != null)
        {
            if (contentSourceComponent is ITooltipContentSource typedContentSource)
            {
                return typedContentSource;
            }

            Debug.LogWarning(
                $"{nameof(TooltipTrigger)} '{name}' content source component must implement {nameof(ITooltipContentSource)}.",
                this);
        }

        ITooltipContentSource tooltipContentSource = GetComponent<ITooltipContentSource>();
        if (tooltipContentSource != null)
        {
            return tooltipContentSource;
        }

        return null;
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
        if (eventData == null)
        {
            return false;
        }

        if (eventData is ExtendedPointerEventData extendedEventData)
        {
            return extendedEventData.pointerType == UIPointerType.MouseOrPen;
        }

        return eventData.pointerId < 0;
    }
}
