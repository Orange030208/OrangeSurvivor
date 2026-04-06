using System;
using System.Collections.Generic;
using UniversalUI.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalUI.Core.Runtime
{
    public sealed class UIManager : MonoBehaviour, IUIManager
    {
        [SerializeField] private UIFrameworkSettings settings;
        [SerializeField] private UIPrefabCatalog catalog;
        [SerializeField] private Canvas rootCanvas;

        private readonly Dictionary<Type, UIPrefabEntry> entryMap = new Dictionary<Type, UIPrefabEntry>();
        private readonly Dictionary<UILayerType, Transform> layerRoots = new Dictionary<UILayerType, Transform>();
        private readonly Dictionary<string, RuntimePage> openedByInstance = new Dictionary<string, RuntimePage>();
        private readonly Dictionary<Type, Queue<UIPageBase>> pooledByPageType = new Dictionary<Type, Queue<UIPageBase>>();
        private readonly UIRuntimeState runtimeState = new UIRuntimeState();

        public event EventHandler<UIPageEventArgs> PageOpened;
        public event EventHandler<UIPageEventArgs> PageClosed;
        public event EventHandler<UIPageEventArgs> PageFocusChanged;

        /// <summary>
        /// 初始化配置、层级与对象池。
        /// </summary>
        private void Awake()
        {
            ValidateSettings();
            BuildEntryMap();
            EnsureRootCanvas();
            BuildLayerRoots();
            WarmupPools();
        }

        /// <summary>
        /// 每帧驱动当前打开页面的 Tick 生命周期。
        /// </summary>
        private void Update()
        {
            float deltaTime = settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            foreach (RuntimePage runtimePage in openedByInstance.Values)
            {
                runtimePage.Page.HandleTick(deltaTime);
            }
        }

        /// <summary>
        /// 打开指定页面并返回页面实例。
        /// </summary>
        public TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase
        {
            IUIPage page = OpenPageByType(typeof(TPage), payload);
            return (TPage)page;
        }

        private IUIPage OpenPageByType(Type pageType, object payload)
        {
            ValidatePageType(pageType);
            UIPrefabEntry entry = ResolveEntry(pageType);

            if (entry.singleton && runtimeState.TryGetLastInstance(pageType, out string singletonInstanceId))
            {
                RuntimePage openedSingleton = openedByInstance[singletonInstanceId];
                FocusPage(openedSingleton);
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
            FocusPage(runtimePage);
            RaisePageOpened(pageType, instanceId);
            return page;
        }

        /// <summary>
        /// 关闭指定页面的最新打开实例。
        /// </summary>
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

        /// <summary>
        /// 按实例 ID 关闭页面。
        /// </summary>
        private bool ClosePageByInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("ClosePageByInstanceId failed: instanceId is null or empty.", nameof(instanceId));
            }

            if (!openedByInstance.TryGetValue(instanceId, out RuntimePage runtimePage))
            {
                return false;
            }

            runtimePage.Page.HandleClose();
            openedByInstance.Remove(instanceId);
            runtimeState.Remove(instanceId);
            RecycleOrDestroy(runtimePage);
            RaisePageClosed(runtimePage.PageType, instanceId);
            return true;
        }

        /// <summary>
        /// 关闭返回栈顶部页面。
        /// </summary>
        public bool CloseTopPage()
        {
            if (!runtimeState.TryPopTopBackStack(out string instanceId))
            {
                return false;
            }

            return ClosePageByInstanceId(instanceId);
        }

        /// <summary>
        /// 关闭所有打开中的页面。
        /// </summary>
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

        /// <summary>
        /// 校验管理器关键配置引用。
        /// </summary>
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

        /// <summary>
        /// 构建页面配置映射表。
        /// </summary>
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
                if (page == null)
                {
                    continue;
                }

                Type pageType = page.GetType();
                entryMap[pageType] = entry;
            }
        }

        /// <summary>
        /// 创建或复用根 Canvas。
        /// </summary>
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

        /// <summary>
        /// 基于配置生成运行时层级根节点。
        /// </summary>
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

        /// <summary>
        /// 根据配置预热可缓存页面实例。
        /// </summary>
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

        /// <summary>
        /// 解析页面注册配置。
        /// </summary>
        private UIPrefabEntry ResolveEntry(Type pageType)
        {
            if (!entryMap.TryGetValue(pageType, out UIPrefabEntry entry))
            {
                throw new KeyNotFoundException($"OpenPage failed: pageType '{pageType.FullName}' is not registered in UIPrefabCatalog.");
            }

            return entry;
        }

        /// <summary>
        /// 创建新页面或从对象池取出页面。
        /// </summary>
        private UIPageBase CreateOrSpawnPage(Type pageType, UIPrefabEntry entry)
        {
            if (settings.EnablePooling && entry.cacheOnClose)
            {
                Queue<UIPageBase> pool = GetOrCreatePool(pageType);
                while (pool.Count > 0)
                {
                    UIPageBase pooled = pool.Dequeue();
                    if (pooled != null)
                    {
                        AttachToLayer(pooled.transform, entry.layerType);
                        return pooled;
                    }
                }
            }

            return InstantiatePage(entry);
        }

        /// <summary>
        /// 实例化页面预制体并校验必需组件。
        /// </summary>
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

        /// <summary>
        /// 将页面挂到指定 UI 层级并置顶显示。
        /// </summary>
        private void AttachToLayer(Transform target, UILayerType layerType)
        {
            if (!layerRoots.TryGetValue(layerType, out Transform layerRoot))
            {
                throw new KeyNotFoundException($"AttachToLayer failed: layerType '{layerType}' is not configured in UIFrameworkSettings.");
            }

            target.SetParent(layerRoot, false);
            target.SetAsLastSibling();
        }

        /// <summary>
        /// 切换焦点页面，其他页面降为非焦点。
        /// </summary>
        private void FocusPage(RuntimePage runtimePage)
        {
            foreach (RuntimePage page in openedByInstance.Values)
            {
                bool hasFocus = page.InstanceId == runtimePage.InstanceId;
                page.Page.HandleFocusChanged(hasFocus);
                RaisePageFocusChanged(page.PageType, page.InstanceId);
            }
        }

        /// <summary>
        /// 页面关闭后进入对象池或直接销毁。
        /// </summary>
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

        /// <summary>
        /// 获取页面对象池，不存在则创建。
        /// </summary>
        private Queue<UIPageBase> GetOrCreatePool(Type pageType)
        {
            if (!pooledByPageType.TryGetValue(pageType, out Queue<UIPageBase> pool))
            {
                pool = new Queue<UIPageBase>();
                pooledByPageType.Add(pageType, pool);
            }

            return pool;
        }

        /// <summary>
        /// 生成符合配置规则的页面实例 ID。
        /// </summary>
        private string CreateInstanceId()
        {
            return $"{settings.InstanceIdPrefix}{Guid.NewGuid():N}";
        }

        /// <summary>
        /// 校验页面 ID 格式与前缀规则。
        /// </summary>
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

        /// <summary>
        /// 广播页面已打开事件。
        /// </summary>
        private void RaisePageOpened(Type pageType, string instanceId)
        {
            PageOpened?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
        }

        /// <summary>
        /// 广播页面已关闭事件。
        /// </summary>
        private void RaisePageClosed(Type pageType, string instanceId)
        {
            PageClosed?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
        }

        /// <summary>
        /// 广播页面焦点变化事件。
        /// </summary>
        private void RaisePageFocusChanged(Type pageType, string instanceId)
        {
            PageFocusChanged?.Invoke(this, new UIPageEventArgs(pageType, instanceId));
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
