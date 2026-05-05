using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及暂停栏内容面板的切换动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件，关闭时先等待内容面板收起。
/// </summary>
public class GamePauseMenu : PageBase
{
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
        InventoryUiBinder.WarmUp(this, ref inventoryUI);
        statusPanel = new PauseMenuPanelBinding("status panel", statusButton, statusSidebar, statusPanelCanvasGroup);
        settingsPanel = new PauseMenuPanelBinding("settings panel", settingsButton, settingsSidebar, settingsPanelCanvasGroup);
        InitContentPanels();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<PauseMenuContext>()
            ?? throw new InvalidOperationException($"{nameof(GamePauseMenu)} requires {nameof(PauseMenuContext)} payload.");
        InventoryUiBinder.Bind(this, ref inventoryUI, currentContext, OwnerUIManager);
        BindButtonEvents();
        HideAllContentPanelsImmediately();
        currentPanel = null;
        panelSwitchVersion = 0;
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancelPanelSwitches();
        return PlayHideAllContentPanelsAsync(cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButtonEvents();
        CancelPanelSwitches();
        HideAllContentPanelsImmediately();
        currentPanel = null;
        InventoryUiBinder.Release(inventoryUI);
        currentContext?.Dispose();
        currentContext = null;
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
        ForEachContentPanel(panel => panel.RefreshDefaults());
    }

    private void HideAllContentPanelsImmediately()
    {
        ForEachContentPanel(panel => panel.SetHiddenImmediate());
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

    private UniTask PlayHideAllContentPanelsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
        CancellationTokenRegistration registration = default;
        bool completed = false;

        void CompleteOnce()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            registration.Dispose();
            completionSource.TrySetResult();
        }

        void CancelOnce()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            KillContentPanelTweens();
            registration.Dispose();
            completionSource.TrySetCanceled(cancellationToken);
        }

        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(CancelOnce);
        }

        PlayHideAllContentPanels(CompleteOnce);
        return completionSource.Task;
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
