using AXR.Framework.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及暂停栏内容面板的切换动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件并接入基类关闭等待管线。
/// </summary>
public class GamePauseMenu : UIPageBase, IInventoryUiFacadeHost
{
    private const float DEFAULT_SLIDE_DURATION = 0.25f;

    [Header("暂停栏按钮")]
    [SerializeField] private UIClickTarget statusButton;
    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private UIClickTarget settingsButton;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("状态面板")]
    [FormerlySerializedAs("propertiesSidebar")]
    [SerializeField] private MonoBehaviour statusSidebar;
    [SerializeField] private CanvasGroup statusPanelCanvasGroup;

    [Header("设置面板")]
    [FormerlySerializedAs("inventorySidebar")]
    [SerializeField] private MonoBehaviour settingsSidebar;
    [SerializeField] private CanvasGroup settingsPanelCanvasGroup;

    [SerializeField] private float slideDuration = DEFAULT_SLIDE_DURATION;

    private PauseMenuContext currentContext;
    private PauseMenuPanelBinding statusPanel;
    private PauseMenuPanelBinding settingsPanel;
    private PauseMenuPanelBinding currentPanel;
    private int panelSwitchVersion;
    private bool buttonEventsBound;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiHostBinding.WarmUp(this, ref inventoryUI);
        statusPanel = new PauseMenuPanelBinding("status panel", statusButton, statusSidebar, statusPanelCanvasGroup);
        settingsPanel = new PauseMenuPanelBinding("settings panel", settingsButton, settingsSidebar, settingsPanelCanvasGroup);
        InitContentPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        currentContext = PageContextBinding.Resolve<PauseMenuContext>(context, () => UIPageContextFactory.CreatePauseMenuContext());
        InventoryUiHostBinding.Bind(this, ref inventoryUI, currentContext);
        BindButtonEvents();
        HideAllContentPanelsImmediately();
        currentPanel = null;
        panelSwitchVersion = 0;
    }

    protected override void OnPageClosed()
    {
        UnbindButtonEvents();
        CancelPanelSwitches();
        HideAllContentPanelsImmediately();
        currentPanel = null;
        InventoryUiHostBinding.Release(inventoryUI);
        PageContextBinding.Release(ref currentContext);
    }

    protected override bool HasAdditionalCloseWaitActions()
    {
        return true;
    }

    protected override void PlayAdditionalCloseWaitActions(bool useUnscaledTime, System.Action onCompleted)
    {
        CancelPanelSwitches();
        PlayHideAllContentPanels(onCompleted);
    }

    private void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

        continueButton.OnClicked += OnContinueClicked;
        menuButton.OnClicked += OnMenuClicked;
        statusPanel.Bind(OnContentPanelRequested);
        settingsPanel.Bind(OnContentPanelRequested);
        buttonEventsBound = true;
    }

    private void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

        continueButton.OnClicked -= OnContinueClicked;
        menuButton.OnClicked -= OnMenuClicked;
        statusPanel.Unbind();
        settingsPanel.Unbind();
        buttonEventsBound = false;
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

    private void OnContentPanelRequested(PauseMenuPanelBinding requestedPanel)
    {
        if (requestedPanel == null)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        if (!requestedPanel.IsConfigured)
        {
            HideCurrentContentPanel();
            return;
        }

        SwitchContentPanel(requestedPanel);
    }

    private void SwitchContentPanel(PauseMenuPanelBinding requestedPanel)
    {
        if (ReferenceEquals(currentPanel, requestedPanel))
        {
            return;
        }

        panelSwitchVersion++;
        int currentSwitchVersion = panelSwitchVersion;
        PauseMenuPanelBinding previousPanel = currentPanel;
        currentPanel = requestedPanel;

        KillContentPanelTweens();

        if (previousPanel == null || !previousPanel.IsConfigured)
        {
            ShowContentPanel(requestedPanel, currentSwitchVersion);
            return;
        }

        Tween hideTween = previousPanel.PlayHide();
        if (hideTween == null)
        {
            ShowContentPanel(requestedPanel, currentSwitchVersion);
            return;
        }

        AppendLifecycleCallback(hideTween, () => ShowContentPanel(requestedPanel, currentSwitchVersion));
    }

    private void ShowContentPanel(PauseMenuPanelBinding panel, int switchVersion)
    {
        if (switchVersion != panelSwitchVersion || panel == null || !panel.IsConfigured)
        {
            return;
        }

        panel.PlayShow();
    }

    private void HideCurrentContentPanel()
    {
        if (currentPanel == null)
        {
            return;
        }

        panelSwitchVersion++;
        PauseMenuPanelBinding previousPanel = currentPanel;
        currentPanel = null;
        KillContentPanelTweens();
        previousPanel.PlayHide();
    }

    private void InitContentPanels()
    {
        ApplySlideDuration();
        ForEachContentPanel(panel => panel.RefreshDefaults());
    }

    private void HideAllContentPanelsImmediately()
    {
        ApplySlideDuration();
        ForEachContentPanel(panel => panel.SetHiddenImmediate());
    }

    private void ApplySlideDuration()
    {
        ForEachContentPanel(panel => panel.ConfigureTimings(slideDuration, Ease.OutCubic, slideDuration, Ease.InCubic));
    }

    private void CancelPanelSwitches()
    {
        panelSwitchVersion++;
        KillContentPanelTweens();
    }

    private void KillContentPanelTweens()
    {
        ForEachContentPanel(panel => panel.Kill());
    }

    private void PlayHideAllContentPanels(System.Action onCompleted)
    {
        currentPanel = null;
        int pendingCount = 0;
        bool completionInvoked = false;

        void MarkCompleted()
        {
            if (completionInvoked)
            {
                return;
            }

            pendingCount--;
            if (pendingCount <= 0)
            {
                completionInvoked = true;
                onCompleted?.Invoke();
            }
        }

        ForEachContentPanel(panel =>
        {
            Tween tween = panel.PlayHide();
            if (tween == null)
            {
                return;
            }

            pendingCount++;
            AppendLifecycleCallback(tween, MarkCompleted);
        });

        if (pendingCount == 0)
        {
            completionInvoked = true;
            onCompleted?.Invoke();
        }
    }

    private void ForEachContentPanel(System.Action<PauseMenuPanelBinding> action)
    {
        if (action == null)
        {
            return;
        }

        action(statusPanel);
        action(settingsPanel);
    }

    private static void AppendLifecycleCallback(Tween tween, System.Action callback)
    {
        if (tween == null || callback == null)
        {
            return;
        }

        bool invoked = false;

        void InvokeOnce()
        {
            if (invoked)
            {
                return;
            }

            invoked = true;
            callback();
        }

        TweenCallback previousOnComplete = tween.onComplete;
        tween.onComplete = () =>
        {
            previousOnComplete?.Invoke();
            InvokeOnce();
        };

        TweenCallback previousOnKill = tween.onKill;
        tween.onKill = () =>
        {
            previousOnKill?.Invoke();
            InvokeOnce();
        };
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

        ValidateOptionalPanelPair(statusButton, statusSidebar, statusPanelCanvasGroup, "status button", "status sidebar", "status panel canvas group");
        ValidateOptionalPanelPair(settingsButton, settingsSidebar, settingsPanelCanvasGroup, "settings button", "settings sidebar", "settings panel canvas group");
    }

    private void ValidateOptionalPanelPair(
        UIClickTarget button,
        MonoBehaviour sidebar,
        CanvasGroup panelCanvasGroup,
        string buttonFieldName,
        string sidebarFieldName,
        string panelCanvasGroupFieldName)
    {
        if (button == null && sidebar == null && panelCanvasGroup == null)
        {
            return;
        }

        if (button == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' has {sidebarFieldName} but no {buttonFieldName}.");
        }

        if (sidebar == null && panelCanvasGroup == null)
        {
            return;
        }

        if (sidebar == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' has {panelCanvasGroupFieldName} but no {sidebarFieldName}.");
        }

        if (panelCanvasGroup == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' has {sidebarFieldName} but no {panelCanvasGroupFieldName}.");
        }

        if (sidebar is not IUIRuntimeMotion)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' {sidebarFieldName} must implement {nameof(IUIRuntimeMotion)}.");
        }
    }

    private void OnValidate()
    {
        slideDuration = Mathf.Max(0f, slideDuration);
    }

    private sealed class PauseMenuPanelBinding
    {
        private readonly string panelName;
        private readonly UIClickTarget button;
        private readonly MonoBehaviour sidebar;
        private readonly IUIRuntimeMotion motion;
        private readonly CanvasGroup canvasGroup;

        private UnityAction clickHandler;

        public PauseMenuPanelBinding(string panelName, UIClickTarget button, MonoBehaviour sidebar, CanvasGroup canvasGroup)
        {
            this.panelName = string.IsNullOrWhiteSpace(panelName) ? "content panel" : panelName;
            this.button = button;
            this.sidebar = sidebar;
            this.canvasGroup = canvasGroup;
            motion = sidebar as IUIRuntimeMotion;
        }

        public bool IsConfigured => button != null && motion != null && canvasGroup != null;

        private bool HasButton => button != null;

        public void Bind(System.Action<PauseMenuPanelBinding> onRequested)
        {
            if (!HasButton)
            {
                return;
            }

            Unbind();
            clickHandler = () => onRequested?.Invoke(this);
            button.OnClicked += clickHandler;
        }

        public void Unbind()
        {
            if (button == null || clickHandler == null)
            {
                return;
            }

            button.OnClicked -= clickHandler;
            clickHandler = null;
        }

        public Tween PlayShow()
        {
            SetInteractionEnabled(true);
            return motion?.Play(UIMotionClipIds.SHOW);
        }

        public Tween PlayHide()
        {
            SetInteractionEnabled(false);
            return motion?.Play(UIMotionClipIds.HIDE);
        }

        public void SetHiddenImmediate()
        {
            motion?.SetImmediate(UIMotionClipIds.HIDE);
            SetInteractionEnabled(false);
        }

        public void RefreshDefaults()
        {
            motion?.RefreshDefaults();
        }

        public void ConfigureTimings(float showDuration, Ease showEase, float hideDuration, Ease hideEase)
        {
        }

        public void Kill()
        {
            motion?.Kill();
        }

        private void SetInteractionEnabled(bool enabled)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        public override string ToString()
        {
            return panelName;
        }
    }
}
