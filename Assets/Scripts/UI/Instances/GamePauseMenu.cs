using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

    private Tween pendingActionTween;

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
        KillTweens();
        HideAllSidebarPanelsImmediately();
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
        PlayExitAnimation(() => GameEventBus.Publish<ResumeGameRequestedEvent>());
    }

    private void OnMenuClicked()
    {
        PlayExitAnimation(() => GameEventBus.Publish<ReturnToMenuRequestedEvent>());
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

    private void HideAllSidebarPanels()
    {
        ApplySlideDuration();
        propertiesSidebar?.Hide();
        inventorySidebar?.Hide();
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

    private void PlayExitAnimation(TweenCallback callback)
    {
        KillTweens();
        HideAllSidebarPanels();

        pendingActionTween = DOVirtual.DelayedCall(slideDuration, callback).SetUpdate(true);
    }

    private void KillTweens()
    {
        pendingActionTween?.Kill();
        pendingActionTween = null;

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
