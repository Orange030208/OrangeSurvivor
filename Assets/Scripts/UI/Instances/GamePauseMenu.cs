using DG.Tweening;
using UnityEngine;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及 sidebar 特殊结构动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件并接入基类关闭等待管线。
/// </summary>
public class GamePauseMenu : UIPageBase
{
    private const float DEFAULT_SLIDE_DURATION = 0.25f;

    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private UIClickTarget menuButton;

    [Header("属性面板(左)")]
    [SerializeField] private UISidebarRevealMotion propertiesSidebar;
    [SerializeField] private UIPropertiesViewSync propertiesViewSync;

    [Header("背包面板(右)")]
    [SerializeField] private UISidebarRevealMotion inventorySidebar;

    [SerializeField] private float slideDuration = DEFAULT_SLIDE_DURATION;

    protected override void Awake()
    {
        base.Awake();
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        BindButtonEvents();
        InjectPropertiesDependencies();
        propertiesViewSync.StartSync();

        HideAllSidebarPanelsImmediately();
        ShowAllSidebarPanels();
    }

    protected override void OnPageClosed()
    {
        UnbindButtonEvents();
        propertiesViewSync.StopSync();
        KillSidebarTweens();
        HideAllSidebarPanelsImmediately();
    }

    protected override bool HasAdditionalCloseWaitActions()
    {
        return propertiesSidebar != null || inventorySidebar != null;
    }

    protected override void PlayAdditionalCloseWaitActions(bool useUnscaledTime, System.Action onCompleted)
    {
        KillSidebarTweens();

        int pendingCount = 0;

        void MarkCompleted()
        {
            pendingCount--;
            if (pendingCount <= 0)
            {
                onCompleted?.Invoke();
            }
        }

        if (propertiesSidebar != null)
        {
            pendingCount++;
            Tween propertiesTween = propertiesSidebar.Hide();
            propertiesTween?.OnComplete(MarkCompleted);
        }

        if (inventorySidebar != null)
        {
            pendingCount++;
            Tween inventoryTween = inventorySidebar.Hide();
            inventoryTween?.OnComplete(MarkCompleted);
        }

        if (pendingCount == 0)
        {
            onCompleted?.Invoke();
        }
    }

    private void BindButtonEvents()
    {
        continueButton.OnClicked += OnContinueClicked;
        menuButton.OnClicked += OnMenuClicked;
    }

    private void UnbindButtonEvents()
    {
        continueButton.OnClicked -= OnContinueClicked;
        menuButton.OnClicked -= OnMenuClicked;
    }

    private void OnContinueClicked()
    {
        GameEventBus.Publish<PauseMenuContinueClickedEvent>();
    }

    private void OnMenuClicked()
    {
        GameEventBus.Publish<PauseMenuReturnToMenuClickedEvent>();
    }

    private void InitSidebarPanels()
    {
        ApplySlideDuration();
        RefreshSidebarDefaults(propertiesSidebar);
        RefreshSidebarDefaults(inventorySidebar);
    }

    private void ShowAllSidebarPanels()
    {
        ApplySlideDuration();
        propertiesSidebar?.Show();
        inventorySidebar?.Show();
    }

    private void HideAllSidebarPanelsImmediately()
    {
        HideSidebarImmediate(propertiesSidebar);
        HideSidebarImmediate(inventorySidebar);
    }

    private void ApplySlideDuration()
    {
        ApplySidebarTimings(propertiesSidebar);
        ApplySidebarTimings(inventorySidebar);
    }

    private void KillSidebarTweens()
    {
        KillSidebarTween(propertiesSidebar);
        KillSidebarTween(inventorySidebar);
    }

    private void InjectPropertiesDependencies()
    {
        if (propertiesViewSync == null)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        PropertiesManager manager = player != null ? player.GetComponent<PropertiesManager>() : null;
        propertiesViewSync.InjectDependencies(manager);
    }

    private void ApplySidebarTimings(UISidebarRevealMotion sidebar)
    {
        if (sidebar == null)
        {
            return;
        }

        sidebar.ConfigureTimings(slideDuration, Ease.OutCubic, slideDuration, Ease.InCubic);
    }

    private static void RefreshSidebarDefaults(UISidebarRevealMotion sidebar)
    {
        if (sidebar == null)
        {
            return;
        }

        sidebar.RefreshDefaults();
    }

    private static void HideSidebarImmediate(UISidebarRevealMotion sidebar)
    {
        if (sidebar == null)
        {
            return;
        }

        sidebar.SetExitImmediate();
    }

    private static void KillSidebarTween(UISidebarRevealMotion sidebar)
    {
        if (sidebar == null)
        {
            return;
        }

        sidebar.Kill();
    }
}
