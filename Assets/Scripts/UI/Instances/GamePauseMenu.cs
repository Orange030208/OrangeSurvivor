using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及暂停栏内容面板的切换动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件，关闭时先等待内容面板收起。
/// </summary>
public class GamePauseMenu : PageBase
{
    [Header("暂停栏按钮")]
    [SerializeField] private Button statusButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;

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
        settingsPanel.Bind(new SettingsPanelManager.Context(OwnerUIManager));
        BindButtonEvents();
        HideSettingsImmediate();
        SelectDefaultControl();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        return settingsPanel.HideAsync(cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButtonEvents();
        settingsPanel.Unbind();
        HideSettingsImmediate();
    }

    private void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

        continueButton.onClick.AddListener(OnContinueClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
        if (statusButton != null)
        {
            statusButton.onClick.AddListener(OnStatusClicked);
        }

        settingsButton.onClick.AddListener(OnSettingsClicked);
        buttonEventsBound = true;
    }

    private void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

        continueButton.onClick.RemoveListener(OnContinueClicked);
        menuButton.onClick.RemoveListener(OnMenuClicked);
        if (statusButton != null)
        {
            statusButton.onClick.RemoveListener(OnStatusClicked);
        }

        settingsButton.onClick.RemoveListener(OnSettingsClicked);
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

    private void SelectDefaultControl()
    {
        if (continueButton == null)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
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
