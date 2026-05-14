using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuUIPage : PageBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsPanelManager settingsPanel;

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
        startButton.onClick.AddListener(OnStartButtonOnClicked);
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

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
        startButton.onClick.RemoveListener(OnStartButtonOnClicked);
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        }

        settingsPanel.Unbind();
        HideSettingsImmediate();
    }

    private void OnStartButtonOnClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<MenuStartClickedEvent>();
    }

    private void OnSettingsButtonClicked()
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
        if (startButton == null)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(startButton.gameObject);
    }

    private void ResolveViewParts()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GetComponentInChildren<SettingsPanelManager>(true);
        }
    }

    private void ValidateConfiguration()
    {
        ResolveViewParts();

        if (startButton == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing start button.");
        }

        if (settingsButton == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing settings button.");
        }

        if (settingsPanel == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing settings panel.");
        }
    }
}
