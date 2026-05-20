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

    private bool buttonEventsBound;
    private ViewHandle<SettingsPanelManager> settingsPanelHandle;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        BindButtonEvents();
        SelectDefaultControl();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        return CloseSettingsPanelAsync(CloseReason.Cancel, cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButtonEvents();
        settingsPanelHandle = default;
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
        CloseSettingsPanelAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnSettingsClicked()
    {
        ToggleSettingsPanelAsync().Forget();
    }

    private async UniTask ToggleSettingsPanelAsync()
    {
        if (IsSettingsPanelOpen())
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await CloseSettingsPanelAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy());
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        settingsPanelHandle = await OwnerUIManager.ShowPopupAsync<SettingsPanelManager>(
            new SettingsPanelManager.Context(OwnerUIManager),
            CreateSettingsPopupOptions(),
            this.GetCancellationTokenOnDestroy());
        ClearSettingsHandleWhenClosedAsync(settingsPanelHandle).Forget();
    }

    private async UniTask CloseSettingsPanelAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        if (!IsSettingsPanelOpen())
        {
            settingsPanelHandle = default;
            return;
        }

        ViewHandle<SettingsPanelManager> handle = settingsPanelHandle;
        settingsPanelHandle = default;
        await handle.CloseAsync(reason, cancellationToken);
    }

    private bool IsSettingsPanelOpen()
    {
        return settingsPanelHandle.IsValid && settingsPanelHandle.View != null && settingsPanelHandle.View.IsOpen;
    }

    private async UniTaskVoid ClearSettingsHandleWhenClosedAsync(ViewHandle<SettingsPanelManager> handle)
    {
        await handle.ClosedTask;
        if (settingsPanelHandle.IsValid && settingsPanelHandle.InstanceId == handle.InstanceId)
        {
            settingsPanelHandle = default;
        }
    }

    private static PopupOptions CreateSettingsPopupOptions()
    {
        return new PopupOptions(
            closeOnOutsideClick: false,
            groupId: "settings",
            replaceSameGroup: true,
            trackInStack: true);
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
    }
}
