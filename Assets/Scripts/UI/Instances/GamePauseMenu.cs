using DG.Tweening;
using UnityEngine;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及 sidebar 特殊结构动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件并接入基类关闭等待管线。
/// </summary>
public class GamePauseMenu : UIPageBase, IInventoryUiFacadeHost
{
    private const float DEFAULT_SLIDE_DURATION = 0.25f;

    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("属性面板(左)")] [SerializeField] private UISidebarRevealMotion propertiesSidebar;

    [Header("背包面板(右)")] [SerializeField] private UISidebarRevealMotion inventorySidebar;

    [SerializeField] private float slideDuration = DEFAULT_SLIDE_DURATION;

    private PauseMenuContext currentContext;
    private SidebarRegionMotionGroup sidebarMotionGroup;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiHostBinding.WarmUp(this, ref inventoryUI);
        SidebarRegionMotion propertiesRegionMotion = new SidebarRegionMotion(nameof(GamePauseMenu), name, "properties sidebar", propertiesSidebar);
        SidebarRegionMotion inventoryRegionMotion = new SidebarRegionMotion(nameof(GamePauseMenu), name, "inventory sidebar", inventorySidebar);
        sidebarMotionGroup = new SidebarRegionMotionGroup(propertiesRegionMotion, inventoryRegionMotion);
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        currentContext = PageContextBinding.Resolve<PauseMenuContext>(context, () => UIPageContextFactory.CreatePauseMenuContext());
        InventoryUiHostBinding.Bind(this, ref inventoryUI, currentContext);
        BindButtonEvents();
        HideAllSidebarPanelsImmediately();
        ShowAllSidebarPanels();
    }

    protected override void OnPageClosed()
    {
        UnbindButtonEvents();
        KillSidebarTweens();
        HideAllSidebarPanelsImmediately();
        InventoryUiHostBinding.Release(inventoryUI);
        PageContextBinding.Release(ref currentContext);
    }

    protected override bool HasAdditionalCloseWaitActions()
    {
        return true;
    }

    protected override void PlayAdditionalCloseWaitActions(bool useUnscaledTime, System.Action onCompleted)
    {
        KillSidebarTweens();
        sidebarMotionGroup.PlayHideAll(onCompleted);
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
        sidebarMotionGroup.RefreshDefaults();
    }

    private void ShowAllSidebarPanels()
    {
        ApplySlideDuration();
        sidebarMotionGroup.SetVisible(true);
    }

    private void HideAllSidebarPanelsImmediately()
    {
        sidebarMotionGroup.SetHiddenImmediate();
    }

    private void ApplySlideDuration()
    {
        sidebarMotionGroup.ConfigureTimings(slideDuration, Ease.OutCubic, slideDuration, Ease.InCubic);
    }

    private void KillSidebarTweens()
    {
        sidebarMotionGroup.Kill();
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
