using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

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

    [Header("页面子部件")]
    [SerializeField] private SettingsPanelManager settingsPanel;

    private bool buttonEventsBound;
    private bool settingsVisible;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        BindButtonEvents();
        HideSettingsImmediate();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        return settingsPanel.HideAsync(cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButtonEvents();
        HideSettingsImmediate();
    }

    private void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

        continueButton.OnClicked += OnContinueClicked;
        menuButton.OnClicked += OnMenuClicked;
        if (statusButton != null)
        {
            statusButton.OnClicked += OnStatusClicked;
        }

        settingsButton.OnClicked += OnSettingsClicked;
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
        if (statusButton != null)
        {
            statusButton.OnClicked -= OnStatusClicked;
        }

        settingsButton.OnClicked -= OnSettingsClicked;
        buttonEventsBound = false;
    }

    private void OnContinueClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<PauseMenuContinueClickedEvent>();
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        GameEventBus.Publish<PauseMenuReturnToMenuClickedEvent>();
    }

    private void OnStatusClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        SetSettingsVisible(false);
    }

    private void OnSettingsClicked()
    {
        AudioSfxBridge.RequestPlay(settingsVisible ? AudioSfxKey.UiCancel : AudioSfxKey.UiConfirm);
        SetSettingsVisible(!settingsVisible);
    }

    private void SetSettingsVisible(bool visible)
    {
        settingsVisible = visible;
        settingsPanel.SetVisible(visible);
    }

    private void HideSettingsImmediate()
    {
        settingsVisible = false;
        settingsPanel.SetHiddenImmediate();
    }

    private void ValidateConfiguration()
    {
        ResolveViewParts();

        if (continueButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing continue button.");
        }

        if (menuButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing menu button.");
        }

        if (settingsButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing settings button.");
        }

        if (settingsPanel == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing settings panel.");
        }
    }

    private void ResolveViewParts()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GetComponentInChildren<SettingsPanelManager>(true);
        }
    }
}
