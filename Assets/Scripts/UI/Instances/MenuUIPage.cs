using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class MenuUIPage : PageBase
{
    [SerializeField] private UIClickTarget startButton;
    [SerializeField] private UIClickTarget settingsButton;
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
        startButton.OnClicked += OnStartButtonOnClicked;
        if (settingsButton != null)
        {
            settingsButton.OnClicked += OnSettingsButtonClicked;
        }

        HideSettingsImmediate();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        return settingsPanel.HideAsync(cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        startButton.OnClicked -= OnStartButtonOnClicked;
        if (settingsButton != null)
        {
            settingsButton.OnClicked -= OnSettingsButtonClicked;
        }

        HideSettingsImmediate();
    }

    private void OnStartButtonOnClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<MenuStartClickedEvent>();
    }

    private void OnSettingsButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
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
