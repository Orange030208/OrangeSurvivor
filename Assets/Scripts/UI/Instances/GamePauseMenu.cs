using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及 sidebar 特殊结构动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件并接入基类关闭等待管线。
/// </summary>
public class GamePauseMenu : UIPageBase
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button menuButton;

    [Header("属性面板(左)")]
    [SerializeField] private SidebarSlider propertiesSidebar;
    [SerializeField] private UIPropertiesViewSync propertiesViewSync;

    [Header("背包面板(右)")]
    [SerializeField] private SidebarSlider inventorySidebar;

    [SerializeField] private float slideDuration = 0.25f;

    protected override void Awake()
    {
        base.Awake();
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        BindButtonEvents();
        InjectPropertiesDependencies();
        propertiesViewSync?.StartSync();

        HideAllSidebarPanelsImmediately();
        ShowAllSidebarPanels();
    }

    protected override void OnPageClosed()
    {
        UnbindButtonEvents();
        propertiesViewSync?.StopSync();
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
            propertiesSidebar.Hide(MarkCompleted);
        }

        if (inventorySidebar != null)
        {
            pendingCount++;
            inventorySidebar.Hide(MarkCompleted);
        }

        if (pendingCount == 0)
        {
            onCompleted?.Invoke();
        }
    }

    private void BindButtonEvents()
    {
        continueButton?.onClick.AddListener(OnContinueClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);
    }

    private void UnbindButtonEvents()
    {
        continueButton?.onClick.RemoveListener(OnContinueClicked);
        menuButton?.onClick.RemoveListener(OnMenuClicked);
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
        propertiesSidebar?.CachePositionsByCurrentState();
        inventorySidebar?.CachePositionsByCurrentState();
    }

    private void ShowAllSidebarPanels()
    {
        ApplySlideDuration();
        propertiesSidebar?.Show();
        inventorySidebar?.Show();
    }

    private void HideAllSidebarPanelsImmediately()
    {
        propertiesSidebar?.HideImmediate();
        inventorySidebar?.HideImmediate();
    }

    private void ApplySlideDuration()
    {
        if (propertiesSidebar != null)
        {
            propertiesSidebar.SlideDuration = slideDuration;
        }

        if (inventorySidebar != null)
        {
            inventorySidebar.SlideDuration = slideDuration;
        }
    }

    private void KillSidebarTweens()
    {
        propertiesSidebar?.KillTween();
        inventorySidebar?.KillTween();
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
}
