using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Orange.UIFramework
{
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour, IUIManager
    {
        public static UIManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private UIFrameworkSettings settings;
        [SerializeField] private ViewCatalog catalog;
        [SerializeField] private Canvas existingRootCanvas;

        private readonly Dictionary<ViewLayer, LayerRuntime> layersByType = new Dictionary<ViewLayer, LayerRuntime>();
        private Canvas rootCanvas;
        private CanvasScaler rootCanvasScaler;
        private GraphicRaycaster rootGraphicRaycaster;
        private RectTransform layersRoot;
        private int requestVersion;
        private bool initialized;

        public UIFrameworkSettings Settings => settings;
        public ViewCatalog Catalog => catalog;
        public Canvas RootCanvas => rootCanvas;
        public int RequestVersion => requestVersion;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ValidateConfigurationOrThrow();
            BuildRootCanvas();
            BuildLayerRoots();
            initialized = true;
        }

        public bool TryGetLayerRoot(ViewLayer layer, out RectTransform root)
        {
            if (layersByType.TryGetValue(layer, out LayerRuntime runtime))
            {
                root = runtime.Root;
                return root != null;
            }

            root = null;
            return false;
        }

        public UIRuntimeDiagnostics GetRuntimeDiagnostics()
        {
            List<LayerDiagnostics> layerDiagnostics = new List<LayerDiagnostics>();
            foreach (KeyValuePair<ViewLayer, LayerRuntime> pair in layersByType)
            {
                LayerRuntime layer = pair.Value;
                layerDiagnostics.Add(new LayerDiagnostics(
                    layer.Root != null ? layer.Root.name : string.Empty,
                    pair.Key,
                    layer.Canvas != null ? layer.Canvas.sortingOrder : 0,
                    layer.Raycaster != null && layer.Raycaster.enabled,
                    layer.Root != null && layer.Root.gameObject.activeInHierarchy));
            }

            string canvasMode = rootCanvas != null ? rootCanvas.renderMode.ToString() : string.Empty;
            string cameraName = rootCanvas != null && rootCanvas.worldCamera != null
                ? rootCanvas.worldCamera.name
                : string.Empty;

            return new UIRuntimeDiagnostics(
                canvasMode,
                cameraName,
                requestVersion,
                Array.Empty<ViewDiagnostics>(),
                Array.Empty<PoolDiagnostics>(),
                string.Empty,
                rootCanvas != null ? rootCanvas.name : string.Empty,
                rootCanvas != null && rootCanvas.gameObject.activeInHierarchy,
                layerDiagnostics);
        }

        [ContextMenu("Log Runtime Diagnostics")]
        public void LogRuntimeDiagnostics()
        {
            UIRuntimeDiagnostics diagnostics = GetRuntimeDiagnostics();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[UIManager] Runtime Diagnostics");
            builder.AppendLine($"Initialized: {initialized}");
            builder.AppendLine($"Root: {(string.IsNullOrWhiteSpace(diagnostics.RootName) ? "None" : diagnostics.RootName)} active={diagnostics.RootActive}");
            builder.AppendLine($"CanvasMode: {diagnostics.CanvasMode}");
            builder.AppendLine($"Camera: {(string.IsNullOrWhiteSpace(diagnostics.CameraName) ? "None" : diagnostics.CameraName)}");
            builder.AppendLine($"RequestVersion: {diagnostics.RequestVersion}");
            builder.AppendLine($"Layers: {diagnostics.Layers.Count}");

            for (int i = 0; i < diagnostics.Layers.Count; i++)
            {
                LayerDiagnostics layer = diagnostics.Layers[i];
                builder.AppendLine($"- {layer.Layer}: name={layer.LayerName}, sorting={layer.SortingOrder}, raycast={layer.BlocksRaycasts}, active={layer.Active}");
            }

            Debug.Log(builder.ToString(), this);
        }

        public UniTask<ViewHandle<TPage>> OpenPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            throw CreateStageNotImplementedException(nameof(OpenPageAsync));
        }

        public UniTask<ViewHandle<TPage>> ReplacePageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            throw CreateStageNotImplementedException(nameof(ReplacePageAsync));
        }

        public UniTask<ViewHandle<TPage>> ResetToPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            throw CreateStageNotImplementedException(nameof(ResetToPageAsync));
        }

        public UniTask CloseTopPageAsync(CancellationToken cancellationToken = default)
        {
            throw CreateStageNotImplementedException(nameof(CloseTopPageAsync));
        }

        public UniTask CloseAllPagesAsync(CancellationToken cancellationToken = default)
        {
            throw CreateStageNotImplementedException(nameof(CloseAllPagesAsync));
        }

        public UniTask<ViewHandle<TPopup>> ShowPopupAsync<TPopup>(
            object payload = null,
            PopupOptions options = default,
            CancellationToken cancellationToken = default)
            where TPopup : PopupBase
        {
            throw CreateStageNotImplementedException(nameof(ShowPopupAsync));
        }

        public UniTask<ModalResult<TResult>> ShowModalAsync<TModal, TResult>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TModal : ModalBase<TResult>
        {
            throw CreateStageNotImplementedException(nameof(ShowModalAsync));
        }

        public UniTask<ViewHandle<TTooltip>> ShowTooltipAsync<TTooltip>(
            object payload,
            TooltipOptions options,
            CancellationToken cancellationToken = default)
            where TTooltip : TooltipBase
        {
            throw CreateStageNotImplementedException(nameof(ShowTooltipAsync));
        }

        public void UpdateTooltipPosition(Vector2 screenPosition)
        {
            throw CreateStageNotImplementedException(nameof(UpdateTooltipPosition));
        }

        public void HideTooltip()
        {
            throw CreateStageNotImplementedException(nameof(HideTooltip));
        }

        public bool IsOpen<TView>() where TView : ViewBase
        {
            return false;
        }

        private void ValidateConfigurationOrThrow()
        {
            if (settings == null)
            {
                throw new MissingReferenceException($"UIManager '{name}' is missing UIFrameworkSettings.");
            }

            if (catalog == null)
            {
                throw new MissingReferenceException($"UIManager '{name}' is missing ViewCatalog.");
            }

            ValidationReport report = settings.Validate(catalog);
            if (report.HasErrors)
            {
                throw new InvalidOperationException($"UIManager '{name}' configuration validation failed:\n{report.ToDisplayString()}");
            }
        }

        private void BuildRootCanvas()
        {
            CanvasProfile canvasProfile = settings.CanvasProfile;
            if (canvasProfile == null)
            {
                throw new MissingReferenceException($"UIManager '{name}' cannot build root because CanvasProfile is missing.");
            }

            rootCanvas = existingRootCanvas != null
                ? existingRootCanvas
                : CreateRootCanvas(settings.RootName);

            rootCanvasScaler = rootCanvas.GetComponent<CanvasScaler>();
            if (rootCanvasScaler == null)
            {
                rootCanvasScaler = rootCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            rootGraphicRaycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (rootGraphicRaycaster == null)
            {
                rootGraphicRaycaster = rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            ApplyCanvasProfile(rootCanvas, rootCanvasScaler, canvasProfile);
            rootCanvas.gameObject.name = settings.RootName;

            if (settings.DontDestroyOnLoad && rootCanvas.transform.parent == null)
            {
                DontDestroyOnLoad(rootCanvas.gameObject);
            }

            layersRoot = EnsureChildRect(rootCanvas.transform, "Layers");
        }

        private Canvas CreateRootCanvas(string rootName)
        {
            GameObject root = new GameObject(rootName);
            RectTransform rectTransform = root.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return root.AddComponent<Canvas>();
        }

        private static void ApplyCanvasProfile(Canvas canvas, CanvasScaler scaler, CanvasProfile profile)
        {
            canvas.renderMode = profile.RenderMode;
            canvas.sortingOrder = profile.RootSortingOrder;

            if (profile.RenderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = profile.UICamera;
                canvas.planeDistance = profile.PlaneDistance;
            }
            else
            {
                canvas.worldCamera = null;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = profile.ReferenceResolution;
            scaler.matchWidthOrHeight = profile.MatchWidthOrHeight;
        }

        private void BuildLayerRoots()
        {
            layersByType.Clear();
            IReadOnlyList<LayerDefinition> layers = settings.Layers;
            for (int i = 0; i < layers.Count; i++)
            {
                LayerDefinition definition = layers[i];
                if (definition == null)
                {
                    continue;
                }

                RectTransform layerRoot = EnsureChildRect(layersRoot, definition.RootName);
                Canvas layerCanvas = layerRoot.GetComponent<Canvas>();
                if (layerCanvas == null)
                {
                    layerCanvas = layerRoot.gameObject.AddComponent<Canvas>();
                }

                layerCanvas.overrideSorting = true;
                layerCanvas.sortingOrder = definition.SortingOrder;

                GraphicRaycaster layerRaycaster = layerRoot.GetComponent<GraphicRaycaster>();
                if (layerRaycaster == null)
                {
                    layerRaycaster = layerRoot.gameObject.AddComponent<GraphicRaycaster>();
                }

                layerRaycaster.enabled = definition.BlocksRaycasts;
                layerRoot.SetSiblingIndex(i);

                layersByType[definition.Layer] = new LayerRuntime(layerRoot, layerCanvas, layerRaycaster);
            }
        }

        private static RectTransform EnsureChildRect(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                RectTransform existingRect = existing as RectTransform;
                if (existingRect == null)
                {
                    throw new InvalidOperationException($"UIManager root child '{childName}' exists but is not a RectTransform.");
                }

                StretchToParent(existingRect);
                return existingRect;
            }

            GameObject child = new GameObject(childName);
            RectTransform rectTransform = child.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            StretchToParent(rectTransform);
            return rectTransform;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private static NotImplementedException CreateStageNotImplementedException(string apiName)
        {
            return new NotImplementedException($"UIManager.{apiName} is scheduled for a later OrangeUIFramework implementation stage.");
        }

        private readonly struct LayerRuntime
        {
            public LayerRuntime(RectTransform root, Canvas canvas, GraphicRaycaster raycaster)
            {
                Root = root;
                Canvas = canvas;
                Raycaster = raycaster;
            }

            public RectTransform Root { get; }
            public Canvas Canvas { get; }
            public GraphicRaycaster Raycaster { get; }
        }
    }
}
