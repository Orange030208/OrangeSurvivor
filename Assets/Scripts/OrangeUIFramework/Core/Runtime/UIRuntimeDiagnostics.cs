using System;
using System.Collections.Generic;

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
            IReadOnlyList<LayerDiagnostics> layers = null)
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
        }

        public string CanvasMode { get; }
        public string CameraName { get; }
        public string RootName { get; }
        public bool RootActive { get; }
        public int RequestVersion { get; }
        public IReadOnlyList<LayerDiagnostics> Layers { get; }
        public IReadOnlyList<ViewDiagnostics> OpenViews { get; }
        public IReadOnlyList<PoolDiagnostics> Pools { get; }
        public string CurrentTooltipInstanceId { get; }
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
            string layerName,
            bool inputActive,
            bool blocksRaycasts)
        {
            InstanceId = instanceId ?? string.Empty;
            ViewId = viewId ?? string.Empty;
            ViewTypeName = viewTypeName ?? string.Empty;
            Kind = kind;
            Phase = phase;
            LayerName = layerName ?? string.Empty;
            InputActive = inputActive;
            BlocksRaycasts = blocksRaycasts;
        }

        public string InstanceId { get; }
        public string ViewId { get; }
        public string ViewTypeName { get; }
        public ViewKind Kind { get; }
        public ViewRuntimePhase Phase { get; }
        public string LayerName { get; }
        public bool InputActive { get; }
        public bool BlocksRaycasts { get; }
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
