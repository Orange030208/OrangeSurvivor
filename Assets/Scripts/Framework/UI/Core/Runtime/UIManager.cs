using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI 运行时总调度器：负责页面实例化、打开关闭、层级挂载、焦点切换以及池化回收。
    /// 这个类不关心页面内部具体表现，只通过统一生命周期接口驱动各页面。
    /// 事件语义：
    /// - PageOpened：页面已注册并触发进入流程，不等待 UISequenceDirector 的 enter 完成。
    /// - PageClosed：页面离开流程已完成；若页面启用了 UISequenceDirector，会等待其 exit 完成后再触发。
    /// - PageActivationChanged：页面 VisualActive / InputActive 已被重新计算并应用。
    /// </summary>
    public sealed class UIManager : MonoBehaviour, IUIManager, IUITransitionRunnerHost
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private UIFrameworkSettings settings;
        [SerializeField] private UIPrefabCatalog catalog;
        [SerializeField] private Canvas rootCanvas;

        private readonly Dictionary<Type, UIPrefabEntry> entryMap = new Dictionary<Type, UIPrefabEntry>();
        private readonly Dictionary<UILayerType, Transform> layerRoots = new Dictionary<UILayerType, Transform>();
        private readonly Dictionary<string, RuntimePage> openedByInstance = new Dictionary<string, RuntimePage>();
        private readonly Dictionary<Type, Queue<UIPageBase>> pooledByPageType = new Dictionary<Type, Queue<UIPageBase>>();
        private readonly UIRuntimeState runtimeState = new UIRuntimeState();
        private readonly HashSet<string> closingInstanceIds = new HashSet<string>();

        private UITransitionRunner transitionRunner;

        public event EventHandler<UIPageEventArgs> PageOpened;
        public event EventHandler<UIPageEventArgs> PageClosed;
        public event EventHandler<UIPageEventArgs> PageActivationChanged;

        public IReadOnlyList<UIPrefabEntry> RegisteredEntries => catalog.Entries;

        public bool TryGetLayerRoot(UILayerType layerType, out Transform layerRoot)
        {
            return layerRoots.TryGetValue(layerType, out layerRoot);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transitionRunner = new UITransitionRunner(this);

            // 启动时完成运行时字典构建、根节点创建、层级创建与对象池预热。
            ValidateSettings();
            BuildEntryMap();
            EnsureRootCanvas();
            BuildLayerRoots();
            WarmupPools();
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
            float deltaTime = settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            foreach (RuntimePage runtimePage in openedByInstance.Values)
            {
                runtimePage.Page.HandleTick(deltaTime);
            }
        }

        // 立即打开页面；不等待其他页面的退场完成。
        public TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase
        {
            IUIPage page = OpenPageByType(typeof(TPage), payload);
            return (TPage)page;
        }

        // 用于“当前顶层页面 -> 新页面”的切换语义；内部统一走链式过渡序列。
        public void ReplaceTopPage<TPage>(object payload = null) where TPage : UIPageBase
        {
            BeginTransition()
                .CloseTopPage()
                .OpenPage<TPage>(payload)
                .Play();
        }

        // 用于“清空当前页面集合 -> 打开目标页面”的重置语义；内部统一走链式过渡序列。
        public void ResetToPage<TPage>(object payload = null) where TPage : UIPageBase
        {
            BeginTransition()
                .CloseAllPages()
                .OpenPage<TPage>(payload)
                .Play();
        }

        public IUITransitionSequence BeginTransition()
        {
            return new UITransitionSequence(transitionRunner);
        }

        public bool OpenPageByCatalogIndex(int catalogIndex, object payload = null)
        {
            IReadOnlyList<UIPrefabEntry> entries = RegisteredEntries;
            if (catalogIndex < 0 || catalogIndex >= entries.Count)
            {
                return false;
            }

            UIPrefabEntry entry = entries[catalogIndex];
            UIPageBase page = entry.prefab.GetComponent<UIPageBase>();

            OpenPageByType(page.GetType(), payload);
            return true;
        }

        private IUIPage OpenPageByType(Type pageType, object payload)
        {
            // 打开流程：校验类型 -> 查配置 -> 复用单例/取实例 -> 注册运行时状态 -> 调页面生命周期。
            // 注意：这里只触发页面进入流程，不等待 UISequenceDirector 的 enter 完成；PageOpened 会立即广播。
            ValidatePageType(pageType);
            UIPrefabEntry entry = ResolveEntry(pageType);

            if (entry.singleton && runtimeState.TryGetLastInstance(pageType, out string singletonInstanceId))
            {
                RuntimePage openedSingleton = openedByInstance[singletonInstanceId];
                ApplyPageActivation(openedSingleton);
                return openedSingleton.Page;
            }

            UIPageBase page = CreateOrSpawnPage(pageType, entry);
            string instanceId = CreateInstanceId();
            page.SetupInstance(instanceId);

            RuntimePage runtimePage = new RuntimePage(instanceId, pageType, page, entry);
            openedByInstance.Add(instanceId, runtimePage);
            runtimeState.Register(pageType, instanceId, entry.trackInBackStack);

            UIPageOpenContext context = new UIPageOpenContext(pageType, instanceId, payload);
            page.HandleOpen(context);
            page.PlayOpenTransition(settings.UseUnscaledTime);
            ApplyPageActivation(runtimePage);
            RaisePageOpened(pageType, instanceId);
            return page;
        }

        public bool ClosePage<TPage>() where TPage : UIPageBase
        {
            return ClosePageByType(typeof(TPage));
        }

        private bool ClosePageByType(Type pageType)
        {
            ValidatePageType(pageType);
            if (!runtimeState.TryGetLastInstance(pageType, out string instanceId))
            {
                return false;
            }

            return ClosePageByInstanceId(instanceId);
        }

        private bool ClosePageByInstanceId(string instanceId)
        {
            // 关闭流程只负责发起，不立即销毁页面；真正收尾要等页面自己的关闭管线完成后回调 FinalizeClose。
            // 若页面启用了 UISequenceDirector，这里的关闭完成会等待 director 的 exit 完成。
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("ClosePageByInstanceId failed: instanceId is null or empty.", nameof(instanceId));
            }

            if (closingInstanceIds.Contains(instanceId))
            {
                return false;
            }

            if (!openedByInstance.TryGetValue(instanceId, out RuntimePage runtimePage))
            {
                return false;
            }

            closingInstanceIds.Add(instanceId);
            ApplyActivationForAllPages();
            runtimePage.Page.PlayCloseTransition(
                settings.UseUnscaledTime,
                () => FinalizeClose(runtimePage));
            return true;
        }

        public bool CloseTopPage()
        {
            if (!runtimeState.TryPopTopBackStack(out string instanceId))
            {
                return false;
            }

            return ClosePageByInstanceId(instanceId);
        }

        public int CloseAllPages()
        {
            if (openedByInstance.Count == 0)
            {
                return 0;
            }

            List<string> instanceIds = new List<string>(openedByInstance.Keys);
            int closedCount = 0;
            foreach (string instanceId in instanceIds)
            {
                if (ClosePageByInstanceId(instanceId))
                {
                    closedCount++;
                }
            }

            return closedCount;
        }

        public bool IsPageOpen<TPage>() where TPage : UIPageBase
        {
            return IsPageOpenByType(typeof(TPage));
        }

        private bool IsPageOpenByType(Type pageType)
        {
            ValidatePageType(pageType);
            return runtimeState.TryGetLastInstance(pageType, out _);
        }

        private void ValidateSettings()
        {
            if (settings == null)
            {
                throw new MissingReferenceException($"UIManager '{name}' is missing UIFrameworkSettings.");
            }

            if (catalog == null)
            {
                throw new MissingReferenceException($"UIManager '{name}' is missing UIPrefabCatalog.");
            }
        }

        private void BuildEntryMap()
        {
            entryMap.Clear();
            IReadOnlyList<UIPrefabEntry> entries = catalog.Entries;
            foreach (UIPrefabEntry entry in entries)
            {
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                UIPageBase page = entry.prefab.GetComponent<UIPageBase>();
                Type pageType = page.GetType();
                entryMap[pageType] = entry;
            }
        }

        private void EnsureRootCanvas()
        {
            if (rootCanvas != null)
            {
                return;
            }

            GameObject root = new GameObject(settings.RootName);
            rootCanvas = root.AddComponent<Canvas>();
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            rootCanvas.renderMode = settings.RenderMode;
            rootCanvas.sortingOrder = settings.RootSortingOrder;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = settings.ReferenceResolution;
            scaler.matchWidthOrHeight = settings.MatchWidthOrHeight;

            if (settings.DontDestroyOnLoading)
            {
                DontDestroyOnLoad(root);
            }
        }

        private void BuildLayerRoots()
        {
            layerRoots.Clear();
            IReadOnlyList<UILayerDefinition> layers = settings.Layers;

            foreach (UILayerDefinition layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                GameObject layerRoot = new GameObject(layer.layerType.ToString());
                RectTransform rectTransform = layerRoot.AddComponent<RectTransform>();
                rectTransform.SetParent(rootCanvas.transform, false);
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                Canvas canvas = layerRoot.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = layer.sortingOrder;

                GraphicRaycaster raycaster = layerRoot.AddComponent<GraphicRaycaster>();
                raycaster.enabled = layer.blocksRaycasts;
                layerRoots[layer.layerType] = layerRoot.transform;
            }
        }

        private void WarmupPools()
        {
            if (!settings.EnablePooling)
            {
                return;
            }

            foreach (KeyValuePair<Type, UIPrefabEntry> pair in entryMap)
            {
                UIPrefabEntry entry = pair.Value;
                if (entry.warmupCount <= 0 || !entry.cacheOnClose)
                {
                    continue;
                }

                Queue<UIPageBase> pool = GetOrCreatePool(pair.Key);
                for (int index = 0; index < entry.warmupCount; index++)
                {
                    UIPageBase page = InstantiatePage(entry);
                    page.gameObject.SetActive(false);
                    pool.Enqueue(page);
                }
            }
        }

        private UIPrefabEntry ResolveEntry(Type pageType)
        {
            if (!entryMap.TryGetValue(pageType, out UIPrefabEntry entry))
            {
                throw new KeyNotFoundException($"OpenPage failed: pageType '{pageType.FullName}' is not registered in UIPrefabCatalog.");
            }

            return entry;
        }

        private UIPageBase CreateOrSpawnPage(Type pageType, UIPrefabEntry entry)
        {
            if (settings.EnablePooling && entry.cacheOnClose)
            {
                Queue<UIPageBase> pool = GetOrCreatePool(pageType);
                while (pool.Count > 0)
                {
                    UIPageBase pooled = pool.Dequeue();
                    AttachToLayer(pooled.transform, entry.layerType);
                    return pooled;
                }
            }

            return InstantiatePage(entry);
        }

        private UIPageBase InstantiatePage(UIPrefabEntry entry)
        {
            if (!layerRoots.TryGetValue(entry.layerType, out Transform layerRoot))
            {
                throw new KeyNotFoundException($"InstantiatePage failed: layerType '{entry.layerType}' is not configured in UIFrameworkSettings.");
            }

            GameObject instance = Instantiate(entry.prefab, layerRoot, false);
            UIPageBase page = instance.GetComponent<UIPageBase>();
            if (page == null)
            {
                throw new InvalidOperationException($"InstantiatePage failed: prefab '{entry.prefab.name}' does not contain UIPageBase.");
            }

            return page;
        }

        private void AttachToLayer(Transform target, UILayerType layerType)
        {
            if (!layerRoots.TryGetValue(layerType, out Transform layerRoot))
            {
                throw new KeyNotFoundException($"AttachToLayer failed: layerType '{layerType}' is not configured in UIFrameworkSettings.");
            }

            target.SetParent(layerRoot, false);
            target.SetAsLastSibling();
        }

        private void FinalizeClose(RuntimePage runtimePage)
        {
            string instanceId = runtimePage.InstanceId;
            closingInstanceIds.Remove(instanceId);

            if (!openedByInstance.ContainsKey(instanceId))
            {
                return;
            }

            runtimePage.Page.HandleClose();
            openedByInstance.Remove(instanceId);
            runtimeState.Remove(instanceId);
            RecycleOrDestroy(runtimePage);

            ApplyActivationForAllPages();
            RaisePageClosed(runtimePage.PageType, instanceId);
            if (closingInstanceIds.Count == 0)
            {
                transitionRunner.NotifyTransitionClosuresCompleted();
            }
        }

        [ContextMenu("Log Runtime Diagnostics")]
        public void LogRuntimeDiagnostics()
        {
            string transitionSummary = transitionRunner != null ? transitionRunner.GetDebugSummary() : "transitionRunner=null";
            string[] backStackSnapshot = runtimeState.GetBackStackSnapshot();
            string topOpenInstance = runtimeState.TryGetTopOpenInstance(out string instanceId) ? instanceId : "None";

            List<string> openPageSummaries = new List<string>();
            foreach (KeyValuePair<string, RuntimePage> pair in openedByInstance)
            {
                RuntimePage runtimePage = pair.Value;
                bool isClosing = closingInstanceIds.Contains(runtimePage.InstanceId);
                openPageSummaries.Add($"{runtimePage.PageType.Name}#{runtimePage.InstanceId} (closing={isClosing})");
            }

            string openedPages = openPageSummaries.Count > 0 ? string.Join(", ", openPageSummaries) : "None";
            string closingPages = closingInstanceIds.Count > 0 ? string.Join(", ", closingInstanceIds) : "None";
            string backStackText = backStackSnapshot.Length > 0 ? string.Join(" -> ", backStackSnapshot) : "Empty";

            Debug.Log(
                $"[UIManager] Runtime Diagnostics\n" +
                $"TransitionRunner: {transitionSummary}\n" +
                $"TopOpenInstance: {topOpenInstance}\n" +
                $"ClosingInstanceIds: {closingPages}\n" +
                $"OpenedPages: {openedPages}\n" +
                $"BackStack: {backStackText}",
                this);
        }

        private void ApplyPageActivation(RuntimePage topRuntimePage)
        {
            foreach (RuntimePage page in openedByInstance.Values)
            {
                bool visualActive = true;
                bool inputActive = !closingInstanceIds.Contains(page.InstanceId)
                                   && page.InstanceId == topRuntimePage.InstanceId;
                page.Page.HandleActivationChanged(visualActive, inputActive);
                RaisePageActivationChanged(page.PageType, page.InstanceId);
            }
        }

        private void ApplyActivationForAllPages()
        {
            if (!runtimeState.TryGetTopOpenInstance(out string instanceId))
            {
                return;
            }

            RuntimePage runtimePage = openedByInstance[instanceId];
            ApplyPageActivation(runtimePage);
        }

        private void RecycleOrDestroy(RuntimePage runtimePage)
        {
            UIPrefabEntry entry = runtimePage.Entry;
            UIPageBase page = runtimePage.Page;

            if (!settings.EnablePooling || !entry.cacheOnClose)
            {
                Destroy(page.gameObject);
                return;
            }

            int maxCachedCount = entry.maxCachedInstancesOverride > 0
                ? entry.maxCachedInstancesOverride
                : settings.MaxCachedInstancesPerPage;

            Queue<UIPageBase> pool = GetOrCreatePool(runtimePage.PageType);
            if (pool.Count >= maxCachedCount)
            {
                Destroy(page.gameObject);
                return;
            }

            pool.Enqueue(page);
        }

        private Queue<UIPageBase> GetOrCreatePool(Type pageType)
        {
            if (!pooledByPageType.TryGetValue(pageType, out Queue<UIPageBase> pool))
            {
                pool = new Queue<UIPageBase>();
                pooledByPageType.Add(pageType, pool);
            }

            return pool;
        }

        void IUITransitionRunnerHost.OpenPage(Type pageType, object payload)
        {
            OpenPageByType(pageType, payload);
        }

        bool IUITransitionRunnerHost.ClosePage(Type pageType)
        {
            return ClosePageByType(pageType);
        }

        bool IUITransitionRunnerHost.CloseTopPage()
        {
            return CloseTopPage();
        }

        int IUITransitionRunnerHost.CloseAllPages()
        {
            return CloseAllPages();
        }

        private string CreateInstanceId()
        {
            return $"{settings.InstanceIdPrefix}{Guid.NewGuid():N}";
        }

        private void ValidatePageType(Type pageType)
        {
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType), "UI page operation failed: pageType is null.");
            }

            if (!typeof(UIPageBase).IsAssignableFrom(pageType))
            {
                throw new ArgumentException(
                    $"UI page operation failed: type '{pageType.FullName}' does not inherit from UIPageBase.",
                    nameof(pageType));
            }
        }

        private void RaisePageOpened(Type pageType, string instanceId)
        {
            PageOpened?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
        }

        private void RaisePageClosed(Type pageType, string instanceId)
        {
            PageClosed?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
        }

        private void RaisePageActivationChanged(Type pageType, string instanceId)
        {
            PageActivationChanged?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
        }

        private sealed class RuntimePage
        {
            public RuntimePage(string instanceId, Type pageType, UIPageBase page, UIPrefabEntry entry)
            {
                InstanceId = instanceId;
                PageType = pageType;
                Page = page;
                Entry = entry;
            }

            public string InstanceId { get; }
            public Type PageType { get; }
            public UIPageBase Page { get; }
            public UIPrefabEntry Entry { get; }
        }
    }
}
