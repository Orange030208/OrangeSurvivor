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
        private readonly Dictionary<string, RuntimeView> trackedViewsByInstance = new Dictionary<string, RuntimeView>();
        private readonly Dictionary<string, RuntimeView> openedViewsByInstance = new Dictionary<string, RuntimeView>();
        private readonly Dictionary<Type, RuntimeView> singletonViewsByType = new Dictionary<Type, RuntimeView>();
        private readonly Dictionary<Type, Queue<ViewBase>> pooledViewsByType = new Dictionary<Type, Queue<ViewBase>>();
        private readonly List<RuntimeView> pageStack = new List<RuntimeView>();
        private readonly List<RuntimeView> popupStack = new List<RuntimeView>();
        private readonly List<RuntimeView> modalStack = new List<RuntimeView>();
        private readonly List<ViewBase> tickingViews = new List<ViewBase>();
        private readonly SemaphoreSlim pageOperationSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim popupOperationSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim modalOperationSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim tooltipOperationSemaphore = new SemaphoreSlim(1, 1);
        private Canvas rootCanvas;
        private CanvasScaler rootCanvasScaler;
        private GraphicRaycaster rootGraphicRaycaster;
        private RectTransform layersRoot;
        private RuntimeView currentTooltip;
        private RectTransform modalMaskRoot;
        private Image modalMaskImage;
        private Button modalMaskButton;
        private RectTransform popupOutsideClickBlockerRoot;
        private Image popupOutsideClickBlockerImage;
        private Button popupOutsideClickBlockerButton;
        private IViewLoader viewLoader;
        private IFloatingViewPositioner floatingViewPositioner;
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
            BuildFrameworkBlockers();
            viewLoader = new PrefabViewLoader();
            floatingViewPositioner = new FloatingViewPositioner();
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
                currentTooltip != null ? currentTooltip.InstanceId : string.Empty,
                rootCanvas != null ? rootCanvas.name : string.Empty,
                rootCanvas != null && rootCanvas.gameObject.activeInHierarchy,
                layerDiagnostics,
                BuildStackDiagnostics(pageStack),
                BuildStackDiagnostics(popupStack),
                BuildStackDiagnostics(modalStack),
                BuildTooltipDiagnostics(),
                BuildOperationDiagnostics(),
                BuildModalMaskDiagnostics(),
                BuildPopupOutsideClickBlockerDiagnostics(),
                BuildInputDiagnostics());
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
            builder.AppendLine($"CurrentTooltip: {(string.IsNullOrWhiteSpace(diagnostics.CurrentTooltipInstanceId) ? "None" : diagnostics.CurrentTooltipInstanceId)}");
            builder.AppendLine($"Layers: {diagnostics.Layers.Count}");
            builder.AppendLine($"PageStack: {diagnostics.PageStack.Count}");
            builder.AppendLine($"PopupStack: {diagnostics.PopupStack.Count}");
            builder.AppendLine($"ModalStack: {diagnostics.ModalStack.Count}");
            builder.AppendLine($"OpenViews: {diagnostics.OpenViews.Count} (live tracked views)");
            builder.AppendLine($"Pools: {diagnostics.Pools.Count}");
            builder.AppendLine($"Operations: pageBusy={diagnostics.Operations.PageOperationBusy}, popupBusy={diagnostics.Operations.PopupOperationBusy}, modalBusy={diagnostics.Operations.ModalOperationBusy}, tooltipBusy={diagnostics.Operations.TooltipOperationBusy}, tracked={diagnostics.Operations.TrackedViewCount}, opening={diagnostics.Operations.OpeningViewCount}, closing={diagnostics.Operations.ClosingViewCount}, failed={diagnostics.Operations.FailedViewCount}");
            builder.AppendLine($"Input: topPage={FormatId(diagnostics.Input.TopPageInstanceId)}, topPopup={FormatId(diagnostics.Input.TopPopupInstanceId)}, topModal={FormatId(diagnostics.Input.TopModalInstanceId)}, modalBlocks={diagnostics.Input.ModalBlocksUnderlyingInput}, inputActive={diagnostics.Input.InputActiveViewCount}, raycastBlocking={diagnostics.Input.RaycastBlockingViewCount}, tooltipRaycast={diagnostics.Input.TooltipBlocksRaycasts}");
            AppendBlockerDiagnostics(builder, "ModalMask", diagnostics.ModalMask);
            AppendBlockerDiagnostics(builder, "PopupOutsideClickBlocker", diagnostics.PopupOutsideClickBlocker);

            for (int i = 0; i < diagnostics.Layers.Count; i++)
            {
                LayerDiagnostics layer = diagnostics.Layers[i];
                builder.AppendLine($"- {layer.Layer}: name={layer.LayerName}, sorting={layer.SortingOrder}, raycast={layer.BlocksRaycasts}, active={layer.Active}");
            }

            AppendStackDiagnostics(builder, "PageStack", diagnostics.PageStack);
            AppendStackDiagnostics(builder, "PopupStack", diagnostics.PopupStack);
            AppendStackDiagnostics(builder, "ModalStack", diagnostics.ModalStack);
            AppendTooltipDiagnostics(builder, diagnostics.Tooltip);

            for (int i = 0; i < diagnostics.OpenViews.Count; i++)
            {
                ViewDiagnostics view = diagnostics.OpenViews[i];
                builder.AppendLine($"- View {view.ViewTypeName}: id={view.ViewId}, instance={view.InstanceId}, kind={view.Kind}, phase={view.Phase}, request={view.RequestVersion}, layer={view.LayerName}, input={view.InputActive}, raycast={view.BlocksRaycasts}");
                if (view.HasPlacement)
                {
                    builder.AppendLine($"  Placement: requested={view.RequestedPosition}, position={view.AnchoredPosition}, anchor={view.ResolvedAnchor}, flipped={view.PlacementWasFlipped}, clamped={view.PlacementWasClamped}, rect={view.LocalRect}, bounds={view.BoundsRect}");
                }
            }

            for (int i = 0; i < diagnostics.Pools.Count; i++)
            {
                PoolDiagnostics pool = diagnostics.Pools[i];
                builder.AppendLine($"- Pool {pool.ViewTypeName}: id={pool.ViewId}, cached={pool.CachedCount}/{pool.MaxCachedCount}");
            }

            Debug.Log(builder.ToString(), this);
        }

        private static void AppendStackDiagnostics(
            StringBuilder builder,
            string title,
            IReadOnlyList<ViewStackDiagnostics> stack)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                ViewStackDiagnostics view = stack[i];
                builder.AppendLine($"- {title}[{view.Index}] {view.ViewTypeName}: id={view.ViewId}, instance={view.InstanceId}, kind={view.Kind}, phase={view.Phase}, top={view.IsTop}, request={view.RequestVersion}, input={view.InputActive}, raycast={view.BlocksRaycasts}, closing={view.Closing}");
                if (view.Kind == ViewKind.Popup)
                {
                    builder.AppendLine($"  PopupOptions: group={FormatId(view.PopupGroupId)}, outsideClick={view.CloseOnOutsideClick}, trackInStack={view.PopupTrackInStack}");
                }
                else if (view.Kind == ViewKind.Modal)
                {
                    builder.AppendLine($"  ModalOptions: closeOnBackgroundClick={view.CloseOnBackgroundClick}");
                }
            }
        }

        private static void AppendTooltipDiagnostics(StringBuilder builder, TooltipDiagnostics tooltip)
        {
            if (!tooltip.HasTooltip)
            {
                builder.AppendLine("- Tooltip: None");
                return;
            }

            builder.AppendLine($"- Tooltip {tooltip.ViewTypeName}: id={tooltip.ViewId}, instance={tooltip.InstanceId}, phase={tooltip.Phase}, followPointer={tooltip.FollowPointer}, input={tooltip.InputActive}, raycast={tooltip.BlocksRaycasts}");
            if (tooltip.HasPlacement)
            {
                builder.AppendLine($"  Placement: position={tooltip.AnchoredPosition}, anchor={tooltip.ResolvedAnchor}, flipped={tooltip.PlacementWasFlipped}, clamped={tooltip.PlacementWasClamped}");
            }
        }

        private static void AppendBlockerDiagnostics(
            StringBuilder builder,
            string label,
            UIBlockerDiagnostics blocker)
        {
            builder.AppendLine($"{label}: exists={blocker.Exists}, active={blocker.Active}, raycast={blocker.BlocksRaycasts}, button={blocker.ButtonEnabled}, top={FormatId(blocker.TopViewInstanceId)}, closeTop={blocker.ClickCanCloseTopView}, sibling={blocker.SiblingIndex}");
        }

        private static string FormatId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
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
            return CloseTopPageInternalAsync(cancellationToken);
        }

        public UniTask CloseAllPagesAsync(CancellationToken cancellationToken = default)
        {
            return CloseAllPagesWithGateAsync(cancellationToken);
        }

        public ViewHandle<TPage> OpenPage<TPage>(object payload = null)
            where TPage : PageBase
        {
            return OpenPageAsync<TPage>(payload).GetAwaiter().GetResult();
        }

        public UniTask<bool> ClosePageAsync<TPage>(CancellationToken cancellationToken = default)
            where TPage : PageBase
        {
            return ClosePageWithGateAsync<TPage>(CloseReason.Normal, cancellationToken);
        }

        public UniTask<ViewHandle<TPopup>> ShowPopupAsync<TPopup>(
            object payload = null,
            PopupOptions options = default,
            CancellationToken cancellationToken = default)
            where TPopup : PopupBase
        {
            return ShowPopupInternalAsync<TPopup>(payload, options, cancellationToken);
        }

        public UniTask<ModalResult<TResult>> ShowModalAsync<TModal, TResult>(
            object payload = null,
            CancellationToken cancellationToken = default)
            where TModal : ModalBase<TResult>
        {
            return ShowModalInternalAsync<TModal, TResult>(payload, cancellationToken);
        }

        public UniTask<ViewHandle<TTooltip>> ShowTooltipAsync<TTooltip>(
            object payload,
            TooltipOptions options,
            CancellationToken cancellationToken = default)
            where TTooltip : TooltipBase
        {
            return ShowTooltipInternalAsync<TTooltip>(payload, options, cancellationToken);
        }

        public void UpdateTooltipPosition(Vector2 screenPosition)
        {
            if (currentTooltip == null || currentTooltip.View == null)
            {
                return;
            }

            TooltipOptions currentOptions = currentTooltip.TooltipOptions;
            if (!currentOptions.FollowPointer)
            {
                return;
            }

            currentTooltip.TooltipOptions = new TooltipOptions(
                currentOptions.Anchor,
                screenPosition,
                currentOptions.Offset,
                followPointer: true,
                margin: currentOptions.Margin,
                preferredAnchor: currentOptions.PreferredAnchor,
                useScreenPosition: true);
            ApplyTooltipPosition(currentTooltip);
        }

        public void HideTooltip()
        {
            HideTooltipWithGateAsync(CloseReason.Normal, CancellationToken.None).Forget();
        }

        public bool IsOpen<TView>() where TView : ViewBase
        {
            Type viewType = typeof(TView);
            foreach (KeyValuePair<string, RuntimeView> pair in trackedViewsByInstance)
            {
                RuntimeView runtimeView = pair.Value;
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
            int currentRequestVersion = ++requestVersion;

            await pageOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await OpenPageLockedAsync<TPage>(payload, mode, currentRequestVersion, cancellationToken);
            }
            finally
            {
                pageOperationSemaphore.Release();
            }
        }

        private async UniTask<ViewHandle<TPage>> OpenPageLockedAsync<TPage>(
            object payload,
            PageOpenMode mode,
            int currentRequestVersion,
            CancellationToken cancellationToken)
            where TPage : PageBase
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfStaleTransition(mode, currentRequestVersion, cancellationToken);

            Type pageType = typeof(TPage);
            ViewDefinition definition = ResolveDefinition(pageType, ViewKind.Page);

            if (mode == PageOpenMode.ReplaceTop && pageStack.Count > 0)
            {
                RuntimeView topPage = pageStack[pageStack.Count - 1];
                await CloseRuntimeViewAsync(topPage, CloseReason.Replace, CancellationToken.None);
                ThrowIfStaleTransition(mode, currentRequestVersion, cancellationToken);
            }
            else if (mode == PageOpenMode.Reset)
            {
                await CloseAllPagesInternalAsync(CloseReason.Reset, CancellationToken.None);
                ThrowIfStaleTransition(mode, currentRequestVersion, cancellationToken);
            }

            if (definition.Singleton && singletonViewsByType.TryGetValue(pageType, out RuntimeView openedSingleton))
            {
                if (openedSingleton.Closing)
                {
                    await openedSingleton.CloseTask.AttachExternalCancellation(cancellationToken);
                    ThrowIfStaleTransition(mode, currentRequestVersion, cancellationToken);
                }
                else
                {
                    MovePageToTop(openedSingleton);
                    RefreshInputState();
                    return new ViewHandle<TPage>(openedSingleton.Handle, (TPage)openedSingleton.View);
                }
            }

            RuntimeView runtimeView = await CreateRuntimeViewAsync(definition, pageType, currentRequestVersion, cancellationToken);
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
                if (IsStaleTransition(mode, currentRequestVersion))
                {
                    throw CreateStaleOperationException(cancellationToken);
                }

                RegisterOpenedPage(runtimeView);
                RefreshInputState();
                return new ViewHandle<TPage>(runtimeView.Handle, (TPage)runtimeView.View);
            }
            catch (Exception exception)
            {
                await CleanupFailedOpenAsync(runtimeView, exception);
                throw;
            }
        }

        private async UniTask CloseTopPageInternalAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ++requestVersion;

            await pageOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pageStack.Count == 0)
                {
                    return;
                }

                RuntimeView topPage = pageStack[pageStack.Count - 1];
                await CloseRuntimeViewAsync(topPage, CloseReason.Back, CancellationToken.None);
            }
            finally
            {
                pageOperationSemaphore.Release();
            }
        }

        private async UniTask CloseAllPagesWithGateAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ++requestVersion;

            await pageOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CloseAllPagesInternalAsync(CloseReason.Reset, CancellationToken.None);
            }
            finally
            {
                pageOperationSemaphore.Release();
            }
        }

        private async UniTask<bool> ClosePageWithGateAsync<TPage>(
            CloseReason reason,
            CancellationToken cancellationToken)
            where TPage : PageBase
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            ++requestVersion;

            await pageOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int i = pageStack.Count - 1; i >= 0; i--)
                {
                    RuntimeView page = pageStack[i];
                    if (page.ViewType != typeof(TPage))
                    {
                        continue;
                    }

                    await CloseRuntimeViewAsync(page, reason, CancellationToken.None);
                    return true;
                }

                return false;
            }
            finally
            {
                pageOperationSemaphore.Release();
            }
        }

        private async UniTask<ViewHandle<TPopup>> ShowPopupInternalAsync<TPopup>(
            object payload,
            PopupOptions options,
            CancellationToken cancellationToken)
            where TPopup : PopupBase
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            await popupOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                PopupOptions resolvedOptions = options;

                Type popupType = typeof(TPopup);
                ViewDefinition definition = ResolveDefinition(popupType, ViewKind.Popup);

                if (resolvedOptions.ReplaceSameGroup && !string.IsNullOrWhiteSpace(resolvedOptions.GroupId))
                {
                    await ClosePopupGroupAsync(resolvedOptions.GroupId, CloseReason.Replace, CancellationToken.None);
                }

                RuntimeView runtimeView = await OpenRuntimeViewAsync(
                    definition,
                    popupType,
                    ViewKind.Popup,
                    payload,
                    ++requestVersion,
                    cancellationToken);

                runtimeView.PopupOptions = resolvedOptions;
                if (!IsRuntimeViewOpened(runtimeView))
                {
                    RegisterOpenedPopup(runtimeView);
                }

                MovePopupToTop(runtimeView);
                ApplyPopupPosition(runtimeView);
                RefreshInputState();
                return new ViewHandle<TPopup>(runtimeView.Handle, (TPopup)runtimeView.View);
            }
            finally
            {
                popupOperationSemaphore.Release();
            }
        }

        private async UniTask<ModalResult<TResult>> ShowModalInternalAsync<TModal, TResult>(
            object payload,
            CancellationToken cancellationToken)
            where TModal : ModalBase<TResult>
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            await modalOperationSemaphore.WaitAsync(cancellationToken);
            RuntimeView runtimeView = null;
            TModal modal = null;
            try
            {
                Type modalType = typeof(TModal);
                ViewDefinition definition = ResolveDefinition(modalType, ViewKind.Modal);
                runtimeView = await OpenRuntimeViewAsync(
                    definition,
                    modalType,
                    ViewKind.Modal,
                    payload,
                    ++requestVersion,
                    cancellationToken);

                modal = (TModal)runtimeView.View;
                if (!IsRuntimeViewOpened(runtimeView))
                {
                    RegisterOpenedModal(runtimeView);
                }

                MoveModalToTop(runtimeView);
                RefreshInputState();
            }
            finally
            {
                modalOperationSemaphore.Release();
            }

            ModalResult<TResult> result;
            try
            {
                result = await modal.ResultTask.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ((IModalView)modal).CompleteResultIfNeeded(CloseReason.Cancel);
                await CloseRuntimeViewAsync(runtimeView, CloseReason.Cancel, CancellationToken.None);
                throw;
            }

            await CloseRuntimeViewAsync(runtimeView, result.CloseReason, CancellationToken.None);
            return result;
        }

        private async UniTask<ViewHandle<TTooltip>> ShowTooltipInternalAsync<TTooltip>(
            object payload,
            TooltipOptions options,
            CancellationToken cancellationToken)
            where TTooltip : TooltipBase
        {
            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();
            await tooltipOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                await HideTooltipLockedAsync(CloseReason.Replace, CancellationToken.None);

                Type tooltipType = typeof(TTooltip);
                ViewDefinition definition = ResolveDefinition(tooltipType, ViewKind.Tooltip);
                RuntimeView runtimeView = await OpenRuntimeViewAsync(
                    definition,
                    tooltipType,
                    ViewKind.Tooltip,
                    payload,
                    ++requestVersion,
                    cancellationToken);

                runtimeView.TooltipOptions = options;
                if (!IsRuntimeViewOpened(runtimeView))
                {
                    RegisterOpenedTooltip(runtimeView);
                }

                ApplyTooltipPosition(runtimeView);
                RefreshInputState();
                return new ViewHandle<TTooltip>(runtimeView.Handle, (TTooltip)runtimeView.View);
            }
            finally
            {
                tooltipOperationSemaphore.Release();
            }
        }

        private async UniTask<RuntimeView> OpenRuntimeViewAsync(
            ViewDefinition definition,
            Type viewType,
            ViewKind kind,
            object payload,
            int currentRequestVersion,
            CancellationToken cancellationToken)
        {
            if (definition.Singleton && singletonViewsByType.TryGetValue(viewType, out RuntimeView openedSingleton))
            {
                if (openedSingleton.Closing)
                {
                    await openedSingleton.CloseTask.AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    return openedSingleton;
                }
            }

            RuntimeView runtimeView = await CreateRuntimeViewAsync(definition, viewType, currentRequestVersion, cancellationToken);
            OpenContext context = new OpenContext(
                viewType,
                definition.Id,
                runtimeView.InstanceId,
                kind,
                payload,
                currentRequestVersion);

            try
            {
                await runtimeView.View.OpenInternalAsync(context, cancellationToken);
                return runtimeView;
            }
            catch (Exception exception)
            {
                await CleanupFailedOpenAsync(runtimeView, exception);
                throw;
            }
        }

        private async UniTask<RuntimeView> CreateRuntimeViewAsync(
            ViewDefinition definition,
            Type viewType,
            int currentRequestVersion,
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
                (reason, token) => CloseByInstanceIdAsync(instanceId, reason, token),
                view,
                this);

            view.Initialize(handle);
            RuntimeView runtimeView = new RuntimeView(instanceId, definition, viewType, view, handle, closedSource, currentRequestVersion);
            trackedViewsByInstance[instanceId] = runtimeView;
            return runtimeView;
        }

        private void RegisterOpenedPage(RuntimeView runtimeView)
        {
            openedViewsByInstance[runtimeView.InstanceId] = runtimeView;
            pageStack.Add(runtimeView);

            RegisterSharedRuntimeState(runtimeView);
        }

        private void RegisterOpenedPopup(RuntimeView runtimeView)
        {
            openedViewsByInstance[runtimeView.InstanceId] = runtimeView;
            popupStack.Add(runtimeView);
            RegisterSharedRuntimeState(runtimeView);
        }

        private void RegisterOpenedModal(RuntimeView runtimeView)
        {
            openedViewsByInstance[runtimeView.InstanceId] = runtimeView;
            modalStack.Add(runtimeView);
            RegisterSharedRuntimeState(runtimeView);
        }

        private void RegisterOpenedTooltip(RuntimeView runtimeView)
        {
            openedViewsByInstance[runtimeView.InstanceId] = runtimeView;
            currentTooltip = runtimeView;
            RegisterSharedRuntimeState(runtimeView);
        }

        private void RegisterSharedRuntimeState(RuntimeView runtimeView)
        {
            if (runtimeView.Definition.Singleton)
            {
                singletonViewsByType[runtimeView.ViewType] = runtimeView;
            }

            if (runtimeView.View.RequiresTick && !tickingViews.Contains(runtimeView.View))
            {
                tickingViews.Add(runtimeView.View);
            }
        }

        private bool IsRuntimeViewOpened(RuntimeView runtimeView)
        {
            return runtimeView != null &&
                   !string.IsNullOrWhiteSpace(runtimeView.InstanceId) &&
                   openedViewsByInstance.ContainsKey(runtimeView.InstanceId);
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
            if (runtimeView == null)
            {
                return;
            }

            if (runtimeView.Closing)
            {
                await runtimeView.CloseTask.AttachExternalCancellation(cancellationToken);
                return;
            }

            runtimeView.Closing = true;
            runtimeView.CloseSource = new UniTaskCompletionSource();
            try
            {
                // Once close starts it must finish, otherwise the view can be left half-closed and unrecyclable.
                await runtimeView.View.CloseInternalAsync(reason, CancellationToken.None);
                CompleteModalResultIfNeeded(runtimeView, reason);
                UnregisterRuntimeView(runtimeView);
                RecycleOrRelease(runtimeView);
                runtimeView.ClosedSource.TrySetResult();
                runtimeView.CloseSource.TrySetResult();
                RefreshInputState();
            }
            catch (Exception exception)
            {
                runtimeView.ClosedSource.TrySetException(exception);
                runtimeView.CloseSource.TrySetException(exception);
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

        private async UniTask ClosePopupGroupAsync(
            string groupId,
            CloseReason reason,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(groupId) || popupStack.Count == 0)
            {
                return;
            }

            List<RuntimeView> popups = new List<RuntimeView>(popupStack);
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                RuntimeView popup = popups[i];
                if (string.Equals(popup.PopupOptions.GroupId, groupId, StringComparison.Ordinal))
                {
                    await CloseRuntimeViewAsync(popup, reason, cancellationToken);
                }
            }
        }

        private async UniTask HideTooltipWithGateAsync(CloseReason reason, CancellationToken cancellationToken)
        {
            await tooltipOperationSemaphore.WaitAsync(cancellationToken);
            try
            {
                await HideTooltipLockedAsync(reason, cancellationToken);
            }
            finally
            {
                tooltipOperationSemaphore.Release();
            }
        }

        private UniTask HideTooltipLockedAsync(CloseReason reason, CancellationToken cancellationToken)
        {
            if (currentTooltip == null)
            {
                return UniTask.CompletedTask;
            }

            return CloseRuntimeViewAsync(currentTooltip, reason, cancellationToken);
        }

        private void CompleteModalResultIfNeeded(RuntimeView runtimeView, CloseReason reason)
        {
            if (runtimeView == null || runtimeView.View == null || runtimeView.Definition.Kind != ViewKind.Modal)
            {
                return;
            }

            if (runtimeView.View is IModalView modalView)
            {
                modalView.CompleteResultIfNeeded(reason);
            }
        }

        private void UnregisterRuntimeView(RuntimeView runtimeView)
        {
            openedViewsByInstance.Remove(runtimeView.InstanceId);
            pageStack.Remove(runtimeView);
            popupStack.Remove(runtimeView);
            modalStack.Remove(runtimeView);
            tickingViews.Remove(runtimeView.View);

            if (ReferenceEquals(currentTooltip, runtimeView))
            {
                currentTooltip = null;
            }

            if (runtimeView.Definition.Singleton &&
                singletonViewsByType.TryGetValue(runtimeView.ViewType, out RuntimeView singleton) &&
                ReferenceEquals(singleton, runtimeView))
            {
                singletonViewsByType.Remove(runtimeView.ViewType);
            }
        }

        private void ForgetTrackedRuntimeView(RuntimeView runtimeView)
        {
            if (runtimeView == null || string.IsNullOrWhiteSpace(runtimeView.InstanceId))
            {
                return;
            }

            trackedViewsByInstance.Remove(runtimeView.InstanceId);
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
                    ForgetTrackedRuntimeView(runtimeView);
                    return;
                }
            }

            viewLoader.Release(runtimeView.View, runtimeView.Definition);
            ForgetTrackedRuntimeView(runtimeView);
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

                FloatingViewPlacement placement = runtimeView.LastPlacement;
                diagnostics.Add(new ViewDiagnostics(
                    runtimeView.InstanceId,
                    runtimeView.Definition.Id,
                    runtimeView.ViewType.Name,
                    runtimeView.Definition.Kind,
                    runtimeView.View != null ? runtimeView.View.Phase : ViewRuntimePhase.None,
                    runtimeView.RequestVersion,
                    layerName,
                    runtimeView.View != null && runtimeView.View.InputActive,
                    runtimeView.View != null && runtimeView.View.BlocksRaycasts,
                    placement.HasValue,
                    placement.AnchoredPosition,
                    placement.ResolvedAnchor,
                    placement.WasFlipped,
                    placement.WasClamped,
                    placement.RequestedPosition,
                    placement.RequestedAnchor,
                    placement.LocalRect,
                    placement.BoundsRect));
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

        private IReadOnlyList<ViewStackDiagnostics> BuildStackDiagnostics(IReadOnlyList<RuntimeView> stack)
        {
            if (stack == null || stack.Count == 0)
            {
                return Array.Empty<ViewStackDiagnostics>();
            }

            List<ViewStackDiagnostics> diagnostics = new List<ViewStackDiagnostics>(stack.Count);
            for (int i = 0; i < stack.Count; i++)
            {
                RuntimeView runtimeView = stack[i];
                diagnostics.Add(BuildStackDiagnostics(runtimeView, i, i == stack.Count - 1));
            }

            return diagnostics;
        }

        private ViewStackDiagnostics BuildStackDiagnostics(RuntimeView runtimeView, int index, bool isTop)
        {
            if (runtimeView == null)
            {
                return new ViewStackDiagnostics(index, isTop, string.Empty, string.Empty, string.Empty, ViewKind.Part, ViewRuntimePhase.None, 0, false, false, false);
            }

            return new ViewStackDiagnostics(
                index,
                isTop,
                runtimeView.InstanceId,
                runtimeView.Definition.Id,
                runtimeView.ViewType.Name,
                runtimeView.Definition.Kind,
                runtimeView.View != null ? runtimeView.View.Phase : ViewRuntimePhase.None,
                runtimeView.RequestVersion,
                runtimeView.View != null && runtimeView.View.InputActive,
                runtimeView.View != null && runtimeView.View.BlocksRaycasts,
                runtimeView.Closing,
                runtimeView.PopupOptions.GroupId,
                runtimeView.PopupOptions.TrackInStack,
                runtimeView.PopupOptions.CloseOnOutsideClick,
                runtimeView.Definition.CloseOnBackgroundClick);
        }

        private TooltipDiagnostics BuildTooltipDiagnostics()
        {
            if (currentTooltip == null)
            {
                return default;
            }

            FloatingViewPlacement placement = currentTooltip.LastPlacement;
            return new TooltipDiagnostics(
                true,
                currentTooltip.InstanceId,
                currentTooltip.Definition.Id,
                currentTooltip.ViewType.Name,
                currentTooltip.View != null ? currentTooltip.View.Phase : ViewRuntimePhase.None,
                currentTooltip.TooltipOptions.FollowPointer,
                currentTooltip.View != null && currentTooltip.View.InputActive,
                currentTooltip.View != null && currentTooltip.View.BlocksRaycasts,
                placement.HasValue,
                placement.AnchoredPosition,
                placement.ResolvedAnchor,
                placement.WasFlipped,
                placement.WasClamped);
        }

        private UIOperationDiagnostics BuildOperationDiagnostics()
        {
            int openingCount = 0;
            int closingCount = 0;
            int failedCount = 0;
            foreach (KeyValuePair<string, RuntimeView> pair in trackedViewsByInstance)
            {
                RuntimeView runtimeView = pair.Value;
                ViewRuntimePhase phase = runtimeView != null && runtimeView.View != null
                    ? runtimeView.View.Phase
                    : ViewRuntimePhase.None;

                if (phase == ViewRuntimePhase.Opening || phase == ViewRuntimePhase.Loading || phase == ViewRuntimePhase.Loaded)
                {
                    openingCount++;
                }

                if (phase == ViewRuntimePhase.Closing || (runtimeView != null && runtimeView.Closing))
                {
                    closingCount++;
                }

                if (phase == ViewRuntimePhase.Failed)
                {
                    failedCount++;
                }
            }

            return new UIOperationDiagnostics(
                requestVersion,
                pageOperationSemaphore.CurrentCount == 0,
                popupOperationSemaphore.CurrentCount == 0,
                modalOperationSemaphore.CurrentCount == 0,
                tooltipOperationSemaphore.CurrentCount == 0,
                trackedViewsByInstance.Count,
                openingCount,
                closingCount,
                failedCount);
        }

        private UIBlockerDiagnostics BuildModalMaskDiagnostics()
        {
            RuntimeView topModal = modalStack.Count > 0 ? modalStack[modalStack.Count - 1] : null;
            return BuildBlockerDiagnostics(
                modalMaskRoot,
                modalMaskImage,
                modalMaskButton,
                topModal,
                topModal != null && topModal.Definition.CloseOnBackgroundClick);
        }

        private UIBlockerDiagnostics BuildPopupOutsideClickBlockerDiagnostics()
        {
            RuntimeView topPopup = popupStack.Count > 0 ? popupStack[popupStack.Count - 1] : null;
            return BuildBlockerDiagnostics(
                popupOutsideClickBlockerRoot,
                popupOutsideClickBlockerImage,
                popupOutsideClickBlockerButton,
                topPopup,
                topPopup != null && topPopup.PopupOptions.CloseOnOutsideClick);
        }

        private static UIBlockerDiagnostics BuildBlockerDiagnostics(
            RectTransform blockerRoot,
            Image image,
            Button button,
            RuntimeView topView,
            bool clickCanCloseTopView)
        {
            bool exists = blockerRoot != null;
            return new UIBlockerDiagnostics(
                exists ? blockerRoot.name : string.Empty,
                exists,
                exists && blockerRoot.gameObject.activeInHierarchy,
                image != null && image.raycastTarget && image.enabled,
                button != null && button.enabled,
                topView != null ? topView.InstanceId : string.Empty,
                topView != null ? topView.Definition.Id : string.Empty,
                topView != null ? topView.ViewType.Name : string.Empty,
                clickCanCloseTopView,
                exists ? blockerRoot.GetSiblingIndex() : -1);
        }

        private UIInputDiagnostics BuildInputDiagnostics()
        {
            int inputActiveCount = 0;
            int raycastBlockingCount = 0;
            foreach (KeyValuePair<string, RuntimeView> pair in openedViewsByInstance)
            {
                RuntimeView runtimeView = pair.Value;
                if (runtimeView.View == null)
                {
                    continue;
                }

                if (runtimeView.View.InputActive)
                {
                    inputActiveCount++;
                }

                if (runtimeView.View.BlocksRaycasts)
                {
                    raycastBlockingCount++;
                }
            }

            RuntimeView topPage = pageStack.Count > 0 ? pageStack[pageStack.Count - 1] : null;
            RuntimeView topPopup = popupStack.Count > 0 ? popupStack[popupStack.Count - 1] : null;
            RuntimeView topModal = modalStack.Count > 0 ? modalStack[modalStack.Count - 1] : null;
            return new UIInputDiagnostics(
                topPage != null ? topPage.InstanceId : string.Empty,
                topPopup != null ? topPopup.InstanceId : string.Empty,
                topModal != null ? topModal.InstanceId : string.Empty,
                topModal != null,
                inputActiveCount,
                raycastBlockingCount,
                currentTooltip != null && currentTooltip.View != null && currentTooltip.View.BlocksRaycasts);
        }

        private bool IsStaleTransition(PageOpenMode mode, int operationVersion)
        {
            return mode != PageOpenMode.Push && operationVersion != requestVersion;
        }

        private void ThrowIfStaleTransition(
            PageOpenMode mode,
            int operationVersion,
            CancellationToken cancellationToken)
        {
            if (!IsStaleTransition(mode, operationVersion))
            {
                return;
            }

            throw CreateStaleOperationException(cancellationToken);
        }

        private OperationCanceledException CreateStaleOperationException(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested
                ? new OperationCanceledException(cancellationToken)
                : new OperationCanceledException("UIManager page transition was superseded by a newer request.");
        }

        private async UniTask CleanupFailedOpenAsync(RuntimeView runtimeView, Exception exception)
        {
            if (exception is OperationCanceledException operationCanceledException)
            {
                runtimeView.ClosedSource.TrySetCanceled(operationCanceledException.CancellationToken);
            }
            else
            {
                runtimeView.ClosedSource.TrySetException(exception);
            }

            if (runtimeView.View != null &&
                runtimeView.View.Phase != ViewRuntimePhase.Closed &&
                runtimeView.View.Phase != ViewRuntimePhase.Recycled)
            {
                try
                {
                    await runtimeView.View.CloseInternalAsync(CloseReason.Cancel, CancellationToken.None);
                }
                catch (Exception closeException)
                {
                    Debug.LogException(closeException, this);
                }
            }

            if (exception is OperationCanceledException)
            {
                RecycleOrRelease(runtimeView);
            }
            else
            {
                viewLoader.Release(runtimeView.View, runtimeView.Definition);
                ForgetTrackedRuntimeView(runtimeView);
            }
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

        private void MovePopupToTop(RuntimeView runtimeView)
        {
            if (runtimeView == null || !popupStack.Remove(runtimeView))
            {
                return;
            }

            popupStack.Add(runtimeView);
            runtimeView.View.transform.SetAsLastSibling();
        }

        private void MoveModalToTop(RuntimeView runtimeView)
        {
            if (runtimeView == null || !modalStack.Remove(runtimeView))
            {
                return;
            }

            modalStack.Add(runtimeView);
            runtimeView.View.transform.SetAsLastSibling();
        }

        private void RefreshInputState()
        {
            RuntimeView topModal = modalStack.Count > 0 ? modalStack[modalStack.Count - 1] : null;
            RuntimeView topPopup = popupStack.Count > 0 ? popupStack[popupStack.Count - 1] : null;
            RuntimeView topPage = pageStack.Count > 0 ? pageStack[pageStack.Count - 1] : null;

            for (int i = 0; i < pageStack.Count; i++)
            {
                RuntimeView page = pageStack[i];
                bool active = topModal == null && ReferenceEquals(page, topPage);
                page.View.ApplyInputState(active, active);
            }

            for (int i = 0; i < popupStack.Count; i++)
            {
                RuntimeView popup = popupStack[i];
                bool active = topModal == null && ReferenceEquals(popup, topPopup);
                popup.View.ApplyInputState(active, active);
            }

            for (int i = 0; i < modalStack.Count; i++)
            {
                RuntimeView modal = modalStack[i];
                bool active = ReferenceEquals(modal, topModal);
                modal.View.ApplyInputState(active, active);
            }

            if (currentTooltip != null && currentTooltip.View != null)
            {
                currentTooltip.View.ApplyInputState(false, false);
            }

            RefreshModalMask();
            RefreshPopupOutsideClickBlocker();
        }

        private void ApplyPopupPosition(RuntimeView runtimeView)
        {
            if (runtimeView == null || runtimeView.View == null)
            {
                return;
            }

            RectTransform rectTransform = runtimeView.View.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            PopupOptions options = runtimeView.PopupOptions;
            bool hasPlacementOrigin = options.HasAnchor || options.HasScreenPosition;
            runtimeView.LastPlacement = floatingViewPositioner.Place(
                rectTransform,
                rectTransform.parent as RectTransform,
                rootCanvas,
                options.Anchor,
                options.HasScreenPosition,
                options.ScreenPosition,
                options.Offset,
                options.Margin,
                hasPlacementOrigin ? options.PreferredAnchor : FloatingViewAnchor.Center,
                rebuildLayout: true);
        }

        private void ApplyTooltipPosition(RuntimeView runtimeView)
        {
            if (runtimeView == null || runtimeView.View == null)
            {
                return;
            }

            RectTransform rectTransform = runtimeView.View.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            TooltipOptions options = runtimeView.TooltipOptions;
            bool hasPlacementOrigin = options.HasAnchor || options.HasScreenPosition;
            runtimeView.LastPlacement = floatingViewPositioner.Place(
                rectTransform,
                rectTransform.parent as RectTransform,
                rootCanvas,
                options.Anchor,
                options.HasScreenPosition,
                options.ScreenPosition,
                options.Offset,
                options.Margin,
                hasPlacementOrigin ? options.PreferredAnchor : FloatingViewAnchor.Center,
                rebuildLayout: false);
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

        private void BuildFrameworkBlockers()
        {
            modalMaskRoot = CreateBlockingImage(
                ViewLayer.ModalMask,
                "ModalMask",
                new Color(0f, 0f, 0f, 0.55f),
                out modalMaskImage,
                out modalMaskButton);
            modalMaskButton.onClick.AddListener(OnModalMaskClicked);
            modalMaskRoot.gameObject.SetActive(false);

            popupOutsideClickBlockerRoot = CreateBlockingImage(
                ViewLayer.Popup,
                "PopupOutsideClickBlocker",
                new Color(0f, 0f, 0f, 0f),
                out popupOutsideClickBlockerImage,
                out popupOutsideClickBlockerButton);
            popupOutsideClickBlockerButton.onClick.AddListener(OnPopupOutsideClick);
            popupOutsideClickBlockerRoot.gameObject.SetActive(false);
        }

        private RectTransform CreateBlockingImage(
            ViewLayer layer,
            string blockerName,
            Color color,
            out Image image,
            out Button button)
        {
            if (!TryGetLayerRoot(layer, out RectTransform layerRoot))
            {
                throw new KeyNotFoundException($"UIManager failed to create '{blockerName}': layer '{layer}' is not configured.");
            }

            RectTransform blockerRoot = EnsureChildRect(layerRoot, blockerName);
            image = blockerRoot.GetComponent<Image>();
            if (image == null)
            {
                image = blockerRoot.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;

            button = blockerRoot.GetComponent<Button>();
            if (button == null)
            {
                button = blockerRoot.gameObject.AddComponent<Button>();
            }

            button.transition = Selectable.Transition.None;
            return blockerRoot;
        }

        private void RefreshModalMask()
        {
            if (modalMaskRoot == null)
            {
                return;
            }

            bool hasModal = modalStack.Count > 0;
            modalMaskRoot.gameObject.SetActive(hasModal);
            if (!hasModal)
            {
                return;
            }

            modalMaskRoot.SetAsLastSibling();
        }

        private void RefreshPopupOutsideClickBlocker()
        {
            if (popupOutsideClickBlockerRoot == null)
            {
                return;
            }

            RuntimeView topPopup = popupStack.Count > 0 ? popupStack[popupStack.Count - 1] : null;
            bool active = modalStack.Count == 0 && topPopup != null && topPopup.PopupOptions.CloseOnOutsideClick;
            popupOutsideClickBlockerRoot.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            popupOutsideClickBlockerRoot.SetAsLastSibling();
            topPopup.View.transform.SetAsLastSibling();
        }

        private void OnModalMaskClicked()
        {
            if (modalStack.Count == 0)
            {
                return;
            }

            RuntimeView topModal = modalStack[modalStack.Count - 1];
            if (!topModal.Definition.CloseOnBackgroundClick)
            {
                return;
            }

            CloseRuntimeViewAsync(topModal, CloseReason.OutsideClick, CancellationToken.None).Forget();
        }

        private void OnPopupOutsideClick()
        {
            if (popupStack.Count == 0)
            {
                return;
            }

            RuntimeView topPopup = popupStack[popupStack.Count - 1];
            if (!topPopup.PopupOptions.CloseOnOutsideClick)
            {
                return;
            }

            CloseRuntimeViewAsync(topPopup, CloseReason.OutsideClick, CancellationToken.None).Forget();
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
                UniTaskCompletionSource closedSource,
                int requestVersion)
            {
                InstanceId = instanceId;
                Definition = definition;
                ViewType = viewType;
                View = view;
                Handle = handle;
                ClosedSource = closedSource;
                RequestVersion = requestVersion;
            }

            public string InstanceId { get; }
            public ViewDefinition Definition { get; }
            public Type ViewType { get; }
            public ViewBase View { get; }
            public ViewHandle Handle { get; }
            public UniTaskCompletionSource ClosedSource { get; }
            public UniTaskCompletionSource CloseSource { get; set; }
            public UniTask CloseTask => CloseSource != null ? CloseSource.Task : UniTask.CompletedTask;
            public int RequestVersion { get; }
            public PopupOptions PopupOptions { get; set; }
            public TooltipOptions TooltipOptions { get; set; }
            public FloatingViewPlacement LastPlacement { get; set; }
            public bool Closing { get; set; }
        }
    }
}
