using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orange.UIFramework
{
    public readonly struct UIRuntimeDiagnostics
    {
        public UIRuntimeDiagnostics(
            string canvasMode,
            string cameraName,
            int requestVersion,
            IReadOnlyList<ViewDiagnostics> openViews,
            IReadOnlyList<PoolDiagnostics> pools,
            string currentTooltipInstanceId,
            string rootName = "",
            bool rootActive = false,
            IReadOnlyList<LayerDiagnostics> layers = null,
            IReadOnlyList<ViewStackDiagnostics> pageStack = null,
            IReadOnlyList<ViewStackDiagnostics> popupStack = null,
            IReadOnlyList<ViewStackDiagnostics> modalStack = null,
            TooltipDiagnostics tooltip = default,
            UIOperationDiagnostics operations = default,
            UIBlockerDiagnostics modalMask = default,
            UIBlockerDiagnostics popupOutsideClickBlocker = default,
            UIInputDiagnostics input = default)
        {
            CanvasMode = canvasMode ?? string.Empty;
            CameraName = cameraName ?? string.Empty;
            RequestVersion = requestVersion;
            OpenViews = openViews ?? Array.Empty<ViewDiagnostics>();
            Pools = pools ?? Array.Empty<PoolDiagnostics>();
            CurrentTooltipInstanceId = currentTooltipInstanceId ?? string.Empty;
            RootName = rootName ?? string.Empty;
            RootActive = rootActive;
            Layers = layers ?? Array.Empty<LayerDiagnostics>();
            PageStack = pageStack ?? Array.Empty<ViewStackDiagnostics>();
            PopupStack = popupStack ?? Array.Empty<ViewStackDiagnostics>();
            ModalStack = modalStack ?? Array.Empty<ViewStackDiagnostics>();
            Tooltip = tooltip;
            Operations = operations;
            ModalMask = modalMask;
            PopupOutsideClickBlocker = popupOutsideClickBlocker;
            Input = input;
        }

        public string CanvasMode { get; }
        public string CameraName { get; }
        public string RootName { get; }
        public bool RootActive { get; }
        public int RequestVersion { get; }
        public IReadOnlyList<LayerDiagnostics> Layers { get; }
        public IReadOnlyList<ViewStackDiagnostics> PageStack { get; }
        public IReadOnlyList<ViewStackDiagnostics> PopupStack { get; }
        public IReadOnlyList<ViewStackDiagnostics> ModalStack { get; }
        public IReadOnlyList<ViewDiagnostics> OpenViews { get; }
        public IReadOnlyList<PoolDiagnostics> Pools { get; }
        public string CurrentTooltipInstanceId { get; }
        public TooltipDiagnostics Tooltip { get; }
        public UIOperationDiagnostics Operations { get; }
        public UIBlockerDiagnostics ModalMask { get; }
        public UIBlockerDiagnostics PopupOutsideClickBlocker { get; }
        public UIInputDiagnostics Input { get; }
    }

    public readonly struct LayerDiagnostics
    {
        public LayerDiagnostics(
            string layerName,
            ViewLayer layer,
            int sortingOrder,
            bool blocksRaycasts,
            bool active)
        {
            LayerName = layerName ?? string.Empty;
            Layer = layer;
            SortingOrder = sortingOrder;
            BlocksRaycasts = blocksRaycasts;
            Active = active;
        }

        public string LayerName { get; }
        public ViewLayer Layer { get; }
        public int SortingOrder { get; }
        public bool BlocksRaycasts { get; }
        public bool Active { get; }
    }

    public readonly struct ViewDiagnostics
    {
        public ViewDiagnostics(
            string instanceId,
            string viewId,
            string viewTypeName,
            ViewKind kind,
            ViewRuntimePhase phase,
            int requestVersion,
            string layerName,
            bool inputActive,
            bool blocksRaycasts,
            bool hasPlacement = false,
            Vector2 anchoredPosition = default,
            FloatingViewAnchor resolvedAnchor = FloatingViewAnchor.BottomRight,
            bool placementWasFlipped = false,
            bool placementWasClamped = false,
            Vector2 requestedPosition = default,
            FloatingViewAnchor requestedAnchor = FloatingViewAnchor.BottomRight,
            Rect localRect = default,
            Rect boundsRect = default)
        {
            InstanceId = instanceId ?? string.Empty;
            ViewId = viewId ?? string.Empty;
            ViewTypeName = viewTypeName ?? string.Empty;
            Kind = kind;
            Phase = phase;
            RequestVersion = requestVersion;
            LayerName = layerName ?? string.Empty;
            InputActive = inputActive;
            BlocksRaycasts = blocksRaycasts;
            HasPlacement = hasPlacement;
            AnchoredPosition = anchoredPosition;
            ResolvedAnchor = resolvedAnchor;
            PlacementWasFlipped = placementWasFlipped;
            PlacementWasClamped = placementWasClamped;
            RequestedPosition = requestedPosition;
            RequestedAnchor = requestedAnchor;
            LocalRect = localRect;
            BoundsRect = boundsRect;
        }

        public string InstanceId { get; }
        public string ViewId { get; }
        public string ViewTypeName { get; }
        public ViewKind Kind { get; }
        public ViewRuntimePhase Phase { get; }
        public int RequestVersion { get; }
        public string LayerName { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
        public bool HasPlacement { get; }
        public Vector2 AnchoredPosition { get; }
        public FloatingViewAnchor ResolvedAnchor { get; }
        public bool PlacementWasFlipped { get; }
        public bool PlacementWasClamped { get; }
        public Vector2 RequestedPosition { get; }
        public FloatingViewAnchor RequestedAnchor { get; }
        public Rect LocalRect { get; }
        public Rect BoundsRect { get; }
    }

    public readonly struct ViewStackDiagnostics
    {
        public ViewStackDiagnostics(
            int index,
            bool isTop,
            string instanceId,
            string viewId,
            string viewTypeName,
            ViewKind kind,
            ViewRuntimePhase phase,
            int requestVersion,
            bool inputActive,
            bool blocksRaycasts,
            bool closing,
            string popupGroupId = "",
            bool popupTrackInStack = false,
            bool closeOnOutsideClick = false,
            bool closeOnBackgroundClick = false)
        {
            Index = index;
            IsTop = isTop;
            InstanceId = instanceId ?? string.Empty;
            ViewId = viewId ?? string.Empty;
            ViewTypeName = viewTypeName ?? string.Empty;
            Kind = kind;
            Phase = phase;
            RequestVersion = requestVersion;
            InputActive = inputActive;
            BlocksRaycasts = blocksRaycasts;
            Closing = closing;
            PopupGroupId = popupGroupId ?? string.Empty;
            PopupTrackInStack = popupTrackInStack;
            CloseOnOutsideClick = closeOnOutsideClick;
            CloseOnBackgroundClick = closeOnBackgroundClick;
        }

        public int Index { get; }
        public bool IsTop { get; }
        public string InstanceId { get; }
        public string ViewId { get; }
        public string ViewTypeName { get; }
        public ViewKind Kind { get; }
        public ViewRuntimePhase Phase { get; }
        public int RequestVersion { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
        public bool Closing { get; }
        public string PopupGroupId { get; }
        public bool PopupTrackInStack { get; }
        public bool CloseOnOutsideClick { get; }
        public bool CloseOnBackgroundClick { get; }
    }

    public readonly struct TooltipDiagnostics
    {
        public TooltipDiagnostics(
            bool hasTooltip,
            string instanceId,
            string viewId,
            string viewTypeName,
            ViewRuntimePhase phase,
            bool followPointer,
            bool inputActive,
            bool blocksRaycasts,
            bool hasPlacement,
            Vector2 anchoredPosition,
            FloatingViewAnchor resolvedAnchor,
            bool placementWasFlipped,
            bool placementWasClamped)
        {
            HasTooltip = hasTooltip;
            InstanceId = instanceId ?? string.Empty;
            ViewId = viewId ?? string.Empty;
            ViewTypeName = viewTypeName ?? string.Empty;
            Phase = phase;
            FollowPointer = followPointer;
            InputActive = inputActive;
            BlocksRaycasts = blocksRaycasts;
            HasPlacement = hasPlacement;
            AnchoredPosition = anchoredPosition;
            ResolvedAnchor = resolvedAnchor;
            PlacementWasFlipped = placementWasFlipped;
            PlacementWasClamped = placementWasClamped;
        }

        public bool HasTooltip { get; }
        public string InstanceId { get; }
        public string ViewId { get; }
        public string ViewTypeName { get; }
        public ViewRuntimePhase Phase { get; }
        public bool FollowPointer { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
        public bool HasPlacement { get; }
        public Vector2 AnchoredPosition { get; }
        public FloatingViewAnchor ResolvedAnchor { get; }
        public bool PlacementWasFlipped { get; }
        public bool PlacementWasClamped { get; }
    }

    public readonly struct UIOperationDiagnostics
    {
        public UIOperationDiagnostics(
            int requestVersion,
            bool pageOperationBusy,
            bool popupOperationBusy,
            bool modalOperationBusy,
            bool tooltipOperationBusy,
            int trackedViewCount,
            int openingViewCount,
            int closingViewCount,
            int failedViewCount)
        {
            RequestVersion = requestVersion;
            PageOperationBusy = pageOperationBusy;
            PopupOperationBusy = popupOperationBusy;
            ModalOperationBusy = modalOperationBusy;
            TooltipOperationBusy = tooltipOperationBusy;
            TrackedViewCount = trackedViewCount;
            OpeningViewCount = openingViewCount;
            ClosingViewCount = closingViewCount;
            FailedViewCount = failedViewCount;
        }

        public int RequestVersion { get; }
        public bool PageOperationBusy { get; }
        public bool PopupOperationBusy { get; }
        public bool ModalOperationBusy { get; }
        public bool TooltipOperationBusy { get; }
        public int TrackedViewCount { get; }
        public int OpeningViewCount { get; }
        public int ClosingViewCount { get; }
        public int FailedViewCount { get; }
    }

    public readonly struct UIBlockerDiagnostics
    {
        public UIBlockerDiagnostics(
            string name,
            bool exists,
            bool active,
            bool blocksRaycasts,
            bool buttonEnabled,
            string topViewInstanceId,
            string topViewId,
            string topViewTypeName,
            bool clickCanCloseTopView,
            int siblingIndex)
        {
            Name = name ?? string.Empty;
            Exists = exists;
            Active = active;
            BlocksRaycasts = blocksRaycasts;
            ButtonEnabled = buttonEnabled;
            TopViewInstanceId = topViewInstanceId ?? string.Empty;
            TopViewId = topViewId ?? string.Empty;
            TopViewTypeName = topViewTypeName ?? string.Empty;
            ClickCanCloseTopView = clickCanCloseTopView;
            SiblingIndex = siblingIndex;
        }

        public string Name { get; }
        public bool Exists { get; }
        public bool Active { get; }
        public bool BlocksRaycasts { get; }
        public bool ButtonEnabled { get; }
        public string TopViewInstanceId { get; }
        public string TopViewId { get; }
        public string TopViewTypeName { get; }
        public bool ClickCanCloseTopView { get; }
        public int SiblingIndex { get; }
    }

    public readonly struct UIInputDiagnostics
    {
        public UIInputDiagnostics(
            string topPageInstanceId,
            string topPopupInstanceId,
            string topModalInstanceId,
            bool modalBlocksUnderlyingInput,
            int inputActiveViewCount,
            int raycastBlockingViewCount,
            bool tooltipBlocksRaycasts)
        {
            TopPageInstanceId = topPageInstanceId ?? string.Empty;
            TopPopupInstanceId = topPopupInstanceId ?? string.Empty;
            TopModalInstanceId = topModalInstanceId ?? string.Empty;
            ModalBlocksUnderlyingInput = modalBlocksUnderlyingInput;
            InputActiveViewCount = inputActiveViewCount;
            RaycastBlockingViewCount = raycastBlockingViewCount;
            TooltipBlocksRaycasts = tooltipBlocksRaycasts;
        }

        public string TopPageInstanceId { get; }
        public string TopPopupInstanceId { get; }
        public string TopModalInstanceId { get; }
        public bool ModalBlocksUnderlyingInput { get; }
        public int InputActiveViewCount { get; }
        public int RaycastBlockingViewCount { get; }
        public bool TooltipBlocksRaycasts { get; }
    }

    public readonly struct PoolDiagnostics
    {
        public PoolDiagnostics(string viewTypeName, string viewId, int cachedCount, int maxCachedCount)
        {
            ViewTypeName = viewTypeName ?? string.Empty;
            ViewId = viewId ?? string.Empty;
            CachedCount = cachedCount;
            MaxCachedCount = maxCachedCount;
        }

        public string ViewTypeName { get; }
        public string ViewId { get; }
        public int CachedCount { get; }
        public int MaxCachedCount { get; }
    }
}
