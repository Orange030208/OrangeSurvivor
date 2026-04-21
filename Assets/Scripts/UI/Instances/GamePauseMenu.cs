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

    [Header("属性面板(左)")] [SerializeField] private UISidebarRevealMotion propertiesSidebar;

    [Header("背包面板(右)")] [SerializeField] private UISidebarRevealMotion inventorySidebar;

    [SerializeField] private float slideDuration = DEFAULT_SLIDE_DURATION;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        BindButtonEvents();
        InjectPropertiesDependencies();
        HideAllSidebarPanelsImmediately();
        ShowAllSidebarPanels();
    }

    protected override void OnPageClosed()
    {
        UnbindButtonEvents();
        KillSidebarTweens();
        HideAllSidebarPanelsImmediately();
    }

    protected override bool HasAdditionalCloseWaitActions()
    {
        return true;
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

        pendingCount++;
        Tween propertiesTween = propertiesSidebar.Play(UIMotionAction.Hide);
        propertiesTween.OnComplete(MarkCompleted);

        pendingCount++;
        Tween inventoryTween = inventorySidebar.Play(UIMotionAction.Hide);
        inventoryTween.OnComplete(MarkCompleted);

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
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<PauseMenuContinueClickedEvent>();
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
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
        propertiesSidebar.Play(UIMotionAction.Show);
        inventorySidebar.Play(UIMotionAction.Show);
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
        Player player = FindFirstObjectByType<Player>();
        PropertiesManager manager = player.GetComponent<PropertiesManager>();
    }

    private void ApplySidebarTimings(UISidebarRevealMotion sidebar)
    {
        sidebar.ConfigureTimings(slideDuration, Ease.OutCubic, slideDuration, Ease.InCubic);
    }

    private static void RefreshSidebarDefaults(UISidebarRevealMotion sidebar)
    {
        sidebar.RefreshDefaults();
    }

    private static void HideSidebarImmediate(UISidebarRevealMotion sidebar)
    {
        sidebar.SetImmediate(UIMotionAction.Hide);
    }

    private static void KillSidebarTween(UISidebarRevealMotion sidebar)
    {
        sidebar.Kill();
    }

    private void ValidateConfiguration()
    {
        if (continueButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing continue button.");
        }

        if (menuButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing menu button.");
        }

        if (propertiesSidebar == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing properties sidebar.");
        }

        if (inventorySidebar == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing inventory sidebar.");
        }
    }
}