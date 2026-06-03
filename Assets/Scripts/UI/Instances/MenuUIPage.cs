using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuUIPage : PageBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button codexButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;

    private UIMotionPlayer[] menuEntryMotions;
    private ViewHandle<SettingsPanelManager> settingsPanelHandle;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        startButton.onClick.AddListener(OnStartButtonOnClicked);
        AddOptionalButtonListener(continueButton, OnContinueButtonClicked);
        AddOptionalButtonListener(codexButton, OnCodexButtonClicked);
        AddOptionalButtonListener(quitButton, OnQuitButtonClicked);
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        ResetMenuEntryMotions();
        EventSystem.current?.SetSelectedGameObject(null);
        return UniTask.CompletedTask;
    }

    protected override async UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        ResetMenuEntryMotions();
        EventSystem.current?.SetSelectedGameObject(null);
        await CloseSettingsPanelAsync(CloseReason.Cancel, cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        startButton.onClick.RemoveListener(OnStartButtonOnClicked);
        RemoveOptionalButtonListener(continueButton, OnContinueButtonClicked);
        RemoveOptionalButtonListener(codexButton, OnCodexButtonClicked);
        RemoveOptionalButtonListener(quitButton, OnQuitButtonClicked);
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        }

        settingsPanelHandle = default;
        ResetMenuEntryMotions();
    }

    private void OnStartButtonOnClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<MenuStartClickedEvent>();
    }

    private void OnContinueButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Debug.Log($"{nameof(MenuUIPage)} continue entry is visible but save loading is not connected yet.");
    }

    private void OnCodexButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Debug.Log($"{nameof(MenuUIPage)} codex entry is visible but the codex page is not connected yet.");
    }

    private void OnQuitButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSettingsButtonClicked()
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
        ResetMenuEntryMotions();
        settingsPanelHandle = await OwnerManager.ShowPopupAsync<SettingsPanelManager>(
            new SettingsPanelManager.Context(OwnerManager),
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
            showBackdrop: true,
            groupId: "settings",
            replaceSameGroup: true,
            trackInStack: true);
    }

    private void ResetMenuEntryMotions()
    {
        ResolveMenuEntryMotions();
        for (int i = 0; i < menuEntryMotions.Length; i++)
        {
            UIMotionPlayer motion = menuEntryMotions[i];
            if (motion == null)
            {
                continue;
            }

            motion.Kill();
            motion.SetImmediate(UIMotionClipIds.HOVER_OUT);
        }
    }

    private void ResolveMenuEntryMotions()
    {
        if (menuEntryMotions != null)
        {
            return;
        }

        menuEntryMotions = new[]
        {
            ResolveButtonMotion(startButton),
            ResolveButtonMotion(continueButton),
            ResolveButtonMotion(codexButton),
            ResolveButtonMotion(quitButton),
            ResolveButtonMotion(settingsButton)
        };
    }

    private static UIMotionPlayer ResolveButtonMotion(Button button)
    {
        return button != null ? button.GetComponent<UIMotionPlayer>() : null;
    }

    private static void AddOptionalButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button != null)
        {
            button.onClick.AddListener(listener);
        }
    }

    private static void RemoveOptionalButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(listener);
        }
    }

    private void ValidateConfiguration()
    {
        if (startButton == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing start button.");
        }

        if (settingsButton == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing settings button.");
        }
    }
}
