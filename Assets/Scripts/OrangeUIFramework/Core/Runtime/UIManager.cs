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
        private readonly Dictionary<string, RuntimeView> openedViewsByInstance = new Dictionary<string, RuntimeView>();
        private readonly Dictionary<Type, RuntimeView> singletonViewsByType = new Dictionary<Type, RuntimeView>();
        private readonly Dictionary<Type, Queue<ViewBase>> pooledViewsByType = new Dictionary<Type, Queue<ViewBase>>();
        private readonly List<RuntimeView> pageStack = new List<RuntimeView>();
        private readonly List<ViewBase> tickingViews = new List<ViewBase>();
        private Canvas rootCanvas;
        private CanvasScaler rootCanvasScaler;
        private GraphicRaycaster rootGraphicRaycaster;
        private RectTransform layersRoot;
        private IViewLoader viewLoader;
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

        private void Update()
        {
            if (tickingViews.Count == 0)
            {
                return;
            }

            float deltaTime = settings != null && settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            for (int i = tickingViews.Count - 1; i >= 0; i--)
            {
                ViewBase view = tickingViews[i];
                if (view == null || !view.IsOpen)
                {
                    tickingViews.RemoveAt(i);
                    continue;
                }

                view.Tick(deltaTime);
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
            viewLoader = new PrefabViewLoader();
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
                BuildViewDiagnostics(),
                BuildPoolDiagnostics(),
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
            builder.AppendLine($"OpenViews: {diagnostics.OpenViews.Count}");
            builder.AppendLine($"Pools: {diagnostics.Pools.Count}");

            for (int i = 0; i < diagnostics.Layers.Count; i++)
            {
                LayerDiagnostics layer = diagnostics.Layers[i];
                builder.AppendLine($"- {layer.Layer}: name={layer.LayerName}, sorting={layer.SortingOrder}, raycast={layer.BlocksRaycasts}, active={layer.Active}");
            }

            for (int i = 0; i < diagnostics.OpenViews.Count; i++)
            {
                ViewDiagnostics view = diagnostics.OpenViews[i];
                builder.AppendLine($"- View {view.ViewTypeName}: id={view.ViewId}, instance={view.InstanceId}, kind={view.Kind}, phase={view.Phase}, layer={view.LayerName}, input={view.InputActive}, raycast={view.BlocksRaycasts}");
            }

            for (int i = 0; i < diagnostics.Pools.Count; i++)
            {
                PoolDiagnostics pool = diagnostics.Pools[i];
                builder.AppendLine($"- Pool {pool.ViewTypeName}: id={pool.ViewId}, cached={pool.CachedCount}/{pool.MaxCachedCount}");
            }

            Debug.Log(builder.ToString(), this);
        }

        public UniTask<ViewHandle<TPage>> OpenPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            return OpenPageInternalAsync<TPage>(payload, PageOpenMode.Push, cancellationToken);
        }

        public UniTask<ViewHandle<TPage>> ReplacePageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            return OpenPageInternalAsync<TPage>(payload, PageOpenMode.ReplaceTop, cancellationToken);
        }

        public UniTask<ViewHandle<TPage>> ResetToPageAsync<TPage>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            return OpenPageInternalAsync<TPage>(payload, PageOpenMode.Reset, cancellationToken);
        }

        public UniTask CloseTopPageAsync(CancellationToken cancellationToken = default)
        {
            if (pageStack.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            RuntimeView topPage = pageStack[pageStack.Count - 1];
            return CloseRuntimeViewAsync(topPage, CloseReason.Back, cancellationToken);
        }

        public UniTask CloseAllPagesAsync(CancellationToken cancellationToken = default)
        {
            return CloseAllPagesInternalAsync(CloseReason.Reset, cancellationToken);
        }

        public ViewHandle<TPage> OpenPage<TPage>(object payload = null)
            where TPage : PageBase
        {
            return OpenPageAsync<TPage>(payload).GetAwaiter().GetResult();
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
            Type viewType = typeof(TView);
            for (int i = 0; i < pageStack.Count; i++)
            {
                RuntimeView runtimeView = pageStack[i];
                if (runtimeView.View != null && runtimeView.View.GetType() == viewType && runtimeView.View.IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        private async UniTask<ViewHandle<TPage>> OpenPageInternalAsync<TPage>(
            object payload,
            PageOpenMode mode,
            CancellationToken cancellationToken)
            where TPage : PageBase
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            Type pageType = typeof(TPage);
            ViewDefinition definition = ResolveDefinition(pageType, ViewKind.Page);
            int currentRequestVersion = ++requestVersion;

            if (mode == PageOpenMode.ReplaceTop && pageStack.Count > 0)
            {
                RuntimeView topPage = pageStack[pageStack.Count - 1];
                await CloseRuntimeViewAsync(topPage, CloseReason.Replace, cancellationToken);
            }
            else if (mode == PageOpenMode.Reset)
            {
                await CloseAllPagesInternalAsync(CloseReason.Reset, cancellationToken);
            }

            if (definition.Singleton && singletonViewsByType.TryGetValue(pageType, out RuntimeView openedSingleton))
            {
                MovePageToTop(openedSingleton);
                ApplyPageInputState();
                return new ViewHandle<TPage>(openedSingleton.Handle, (TPage)openedSingleton.View);
            }

            RuntimeView runtimeView = await CreateRuntimeViewAsync(definition, pageType, cancellationToken);
            OpenContext context = new OpenContext(
                pageType,
                definition.Id,
                runtimeView.InstanceId,
                ViewKind.Page,
                payload,
                currentRequestVersion);

            try
            {
                await runtimeView.View.OpenInternalAsync(context, cancellationToken);
                RegisterOpenedPage(runtimeView);
                ApplyPageInputState();
                return new ViewHandle<TPage>(runtimeView.Handle, (TPage)runtimeView.View);
            }
            catch (Exception exception)
            {
                runtimeView.ClosedSource.TrySetException(exception);
                viewLoader.Release(runtimeView.View, runtimeView.Definition);
                throw;
            }
        }

        private async UniTask<RuntimeView> CreateRuntimeViewAsync(
            ViewDefinition definition,
            Type viewType,
            CancellationToken cancellationToken)
        {
            if (!TryGetLayerRoot(definition.Layer, out RectTransform layerRoot))
            {
                throw new KeyNotFoundException($"CreateRuntimeViewAsync failed: layer '{definition.Layer}' is not configured.");
            }

            ViewBase view = SpawnFromPool(viewType, definition, layerRoot);
            if (view == null)
            {
                view = await viewLoader.LoadAsync(definition, layerRoot, cancellationToken);
            }
            else
            {
                view.transform.SetParent(layerRoot, false);
                view.transform.SetAsLastSibling();
            }

            string instanceId = CreateInstanceId();
            UniTaskCompletionSource closedSource = new UniTaskCompletionSource();
            ViewHandle handle = new ViewHandle(
                instanceId,
                definition.Id,
                definition.Kind,
                closedSource.Task,
                (reason, token) => CloseByInstanceIdAsync(instanceId, reason, token));

            view.Initialize(handle);
            return new RuntimeView(instanceId, definition, viewType, view, handle, closedSource);
        }

        private void RegisterOpenedPage(RuntimeView runtimeView)
        {
            openedViewsByInstance[runtimeView.InstanceId] = runtimeView;
            pageStack.Add(runtimeView);

            if (runtimeView.Definition.Singleton)
            {
                singletonViewsByType[runtimeView.ViewType] = runtimeView;
            }

            if (runtimeView.View.RequiresTick && !tickingViews.Contains(runtimeView.View))
            {
                tickingViews.Add(runtimeView.View);
            }
        }

        private UniTask CloseByInstanceIdAsync(
            string instanceId,
            CloseReason reason,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return UniTask.CompletedTask;
            }

            if (!openedViewsByInstance.TryGetValue(instanceId, out RuntimeView runtimeView))
            {
                return UniTask.CompletedTask;
            }

            return CloseRuntimeViewAsync(runtimeView, reason, cancellationToken);
        }

        private async UniTask CloseRuntimeViewAsync(
            RuntimeView runtimeView,
            CloseReason reason,
            CancellationToken cancellationToken)
        {
            if (runtimeView == null || runtimeView.Closing)
            {
                return;
            }

            runtimeView.Closing = true;
            try
            {
                await runtimeView.View.CloseInternalAsync(reason, cancellationToken);
                UnregisterRuntimeView(runtimeView);
                RecycleOrRelease(runtimeView);
                runtimeView.ClosedSource.TrySetResult();
                ApplyPageInputState();
            }
            catch (Exception exception)
            {
                runtimeView.ClosedSource.TrySetException(exception);
                throw;
            }
        }

        private async UniTask CloseAllPagesInternalAsync(
            CloseReason reason,
            CancellationToken cancellationToken)
        {
            if (pageStack.Count == 0)
            {
                return;
            }

            List<RuntimeView> pages = new List<RuntimeView>(pageStack);
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                await CloseRuntimeViewAsync(pages[i], reason, cancellationToken);
            }
        }

        private void UnregisterRuntimeView(RuntimeView runtimeView)
        {
            openedViewsByInstance.Remove(runtimeView.InstanceId);
            pageStack.Remove(runtimeView);
            tickingViews.Remove(runtimeView.View);

            if (runtimeView.Definition.Singleton &&
                singletonViewsByType.TryGetValue(runtimeView.ViewType, out RuntimeView singleton) &&
                ReferenceEquals(singleton, runtimeView))
            {
                singletonViewsByType.Remove(runtimeView.ViewType);
            }
        }

        private void RecycleOrRelease(RuntimeView runtimeView)
        {
            if (settings.EnablePooling && runtimeView.Definition.CacheOnClose)
            {
                Queue<ViewBase> pool = GetOrCreatePool(runtimeView.ViewType);
                int maxCount = runtimeView.Definition.MaxCachedInstancesOverride >= 0
                    ? runtimeView.Definition.MaxCachedInstancesOverride
                    : settings.MaxCachedInstancesPerView;

                if (pool.Count < maxCount)
                {
                    runtimeView.View.MarkRecycled();
                    runtimeView.View.gameObject.SetActive(false);
                    pool.Enqueue(runtimeView.View);
                    return;
                }
            }

            viewLoader.Release(runtimeView.View, runtimeView.Definition);
        }

        private ViewBase SpawnFromPool(Type viewType, ViewDefinition definition, Transform parent)
        {
            if (!settings.EnablePooling || !definition.CacheOnClose)
            {
                return null;
            }

            if (!pooledViewsByType.TryGetValue(viewType, out Queue<ViewBase> pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                ViewBase view = pool.Dequeue();
                if (view == null)
                {
                    continue;
                }

                view.transform.SetParent(parent, false);
                return view;
            }

            return null;
        }

        private Queue<ViewBase> GetOrCreatePool(Type viewType)
        {
            if (!pooledViewsByType.TryGetValue(viewType, out Queue<ViewBase> pool))
            {
                pool = new Queue<ViewBase>();
                pooledViewsByType.Add(viewType, pool);
            }

            return pool;
        }

        private IReadOnlyList<ViewDiagnostics> BuildViewDiagnostics()
        {
            List<ViewDiagnostics> diagnostics = new List<ViewDiagnostics>();
            foreach (KeyValuePair<string, RuntimeView> pair in openedViewsByInstance)
            {
                RuntimeView runtimeView = pair.Value;
                string layerName = string.Empty;
                if (layersByType.TryGetValue(runtimeView.Definition.Layer, out LayerRuntime layer) && layer.Root != null)
                {
                    layerName = layer.Root.name;
                }

                diagnostics.Add(new ViewDiagnostics(
                    runtimeView.InstanceId,
                    runtimeView.Definition.Id,
                    runtimeView.ViewType.Name,
                    runtimeView.Definition.Kind,
                    runtimeView.View != null ? runtimeView.View.Phase : ViewRuntimePhase.None,
                    layerName,
                    runtimeView.View != null && runtimeView.View.InputActive,
                    runtimeView.View != null && runtimeView.View.BlocksRaycasts));
            }

            return diagnostics;
        }

        private IReadOnlyList<PoolDiagnostics> BuildPoolDiagnostics()
        {
            List<PoolDiagnostics> diagnostics = new List<PoolDiagnostics>();
            foreach (KeyValuePair<Type, Queue<ViewBase>> pair in pooledViewsByType)
            {
                ViewDefinition definition = null;
                catalog.TryFindByType(pair.Key, out definition);

                int maxCount = definition != null && definition.MaxCachedInstancesOverride >= 0
                    ? definition.MaxCachedInstancesOverride
                    : settings.MaxCachedInstancesPerView;

                diagnostics.Add(new PoolDiagnostics(
                    pair.Key.Name,
                    definition != null ? definition.Id : string.Empty,
                    pair.Value != null ? pair.Value.Count : 0,
                    maxCount));
            }

            return diagnostics;
        }

        private string CreateInstanceId()
        {
            string prefix = settings != null ? settings.InstanceIdPrefix : "ui_";
            return $"{prefix}{Guid.NewGuid():N}";
        }

        private void MovePageToTop(RuntimeView runtimeView)
        {
            if (!pageStack.Remove(runtimeView))
            {
                return;
            }

            pageStack.Add(runtimeView);
            runtimeView.View.transform.SetAsLastSibling();
        }

        private void ApplyPageInputState()
        {
            RuntimeView topPage = pageStack.Count > 0 ? pageStack[pageStack.Count - 1] : null;
            for (int i = 0; i < pageStack.Count; i++)
            {
                RuntimeView page = pageStack[i];
                bool isTop = ReferenceEquals(page, topPage);
                page.View.ApplyInputState(isTop, isTop);
            }
        }

        private ViewDefinition ResolveDefinition(Type viewType, ViewKind expectedKind)
        {
            if (viewType == null)
            {
                throw new ArgumentNullException(nameof(viewType));
            }

            if (!catalog.TryFindByType(viewType, out ViewDefinition definition))
            {
                throw new KeyNotFoundException($"UIManager failed: view type '{viewType.FullName}' is not registered in ViewCatalog.");
            }

            if (definition.Kind != expectedKind)
            {
                throw new InvalidOperationException($"UIManager failed: view type '{viewType.FullName}' is registered as '{definition.Kind}', expected '{expectedKind}'.");
            }

            return definition;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
            }
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

        private enum PageOpenMode
        {
            Push,
            ReplaceTop,
            Reset
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

        private sealed class RuntimeView
        {
            public RuntimeView(
                string instanceId,
                ViewDefinition definition,
                Type viewType,
                ViewBase view,
                ViewHandle handle,
                UniTaskCompletionSource closedSource)
            {
                InstanceId = instanceId;
                Definition = definition;
                ViewType = viewType;
                View = view;
                Handle = handle;
                ClosedSource = closedSource;
            }

            public string InstanceId { get; }
            public ViewDefinition Definition { get; }
            public Type ViewType { get; }
            public ViewBase View { get; }
            public ViewHandle Handle { get; }
            public UniTaskCompletionSource ClosedSource { get; }
            public bool Closing { get; set; }
        }
    }
}
