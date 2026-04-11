using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 运行时总调度器：负责页面实例化、打开关闭、层级挂载、焦点切换以及池化回收。
/// 这个类不关心页面内部具体表现，只通过统一生命周期接口驱动各页面。
/// </summary>
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
    private readonly HashSet<string> closingInstanceIds = new HashSet<string>();

    public event EventHandler<UIPageEventArgs> PageOpened;
    public event EventHandler<UIPageEventArgs> PageClosed;
    public event EventHandler<UIPageEventArgs> PageActivationChanged;

    public IReadOnlyList<UIPrefabEntry> RegisteredEntries => catalog != null ? catalog.Entries : Array.Empty<UIPrefabEntry>();

    private void Awake()
    {
        // 启动时完成运行时字典构建、根节点创建、层级创建与对象池预热。
        ValidateSettings();
        BuildEntryMap();
        EnsureRootCanvas();
        BuildLayerRoots();
        WarmupPools();
    }

    private void Update()
    {
        float deltaTime = settings.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        foreach (RuntimePage runtimePage in openedByInstance.Values)
        {
            runtimePage.Page.HandleTick(deltaTime);
        }
    }

    public TPage OpenPage<TPage>(object payload = null) where TPage : UIPageBase
    {
        IUIPage page = OpenPageByType(typeof(TPage), payload);
        return (TPage)page;
    }

    public bool OpenPageByCatalogIndex(int catalogIndex, object payload = null)
    {
        IReadOnlyList<UIPrefabEntry> entries = RegisteredEntries;
        if (catalogIndex < 0 || catalogIndex >= entries.Count)
        {
            return false;
        }

        UIPrefabEntry entry = entries[catalogIndex];
        if (entry == null || entry.prefab == null)
        {
            return false;
        }

        UIPageBase page = entry.prefab.GetComponent<UIPageBase>();
        if (page == null)
        {
            return false;
        }

        OpenPageByType(page.GetType(), payload);
        return true;
    }

    private IUIPage OpenPageByType(Type pageType, object payload)
    {
        // 打开流程：校验类型 -> 查配置 -> 复用单例/取实例 -> 注册运行时状态 -> 调页面生命周期。
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
        page.PlayOpenTransition(ResolveOpenTransition(entry), settings.UseUnscaledTime);
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
            ResolveCloseTransition(runtimePage.Entry),
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
            if (page == null)
            {
                continue;
            }

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
                if (pooled != null)
                {
                    AttachToLayer(pooled.transform, entry.layerType);
                    return pooled;
                }
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
    }

    private UIPageTransitionSettings ResolveOpenTransition(UIPrefabEntry entry)
    {
        return entry.useCustomTransition ? entry.customOpenTransition : settings.DefaultOpenTransition;
    }

    private UIPageTransitionSettings ResolveCloseTransition(UIPrefabEntry entry)
    {
        return entry.useCustomTransition ? entry.customCloseTransition : settings.DefaultCloseTransition;
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

        if (!openedByInstance.TryGetValue(instanceId, out RuntimePage runtimePage))
        {
            return;
        }

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
