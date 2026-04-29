using UnityEngine;

public class MenuUIPage : UIPageBase
{
    [SerializeField] private UIClickTarget startButton;
    [SerializeField] private UIClickTarget characterSelectButton;
    [SerializeField] private UIClickTarget settingsButton;
    [SerializeField] private UISidebarRevealMotion settingsSidebar;

    private bool settingsVisible;

    protected override void Awake()
    {
        base.Awake();
        HideSettingsImmediate();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        startButton.OnClicked += OnStartButtonOnClicked;
        if (settingsButton != null)
        {
            settingsButton.OnClicked += OnSettingsButtonClicked;
        }

        HideSettingsImmediate();
    }

    protected override void OnPageClosed()
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
        if (settingsSidebar == null)
        {
            settingsVisible = false;
            return;
        }

        settingsVisible = visible;
        settingsSidebar.Play(visible ? UIMotionAction.Show : UIMotionAction.Hide);
        SetSettingsInteractionEnabled(visible);
    }

    private void HideSettingsImmediate()
    {
        settingsVisible = false;
        settingsSidebar?.SetHiddenImmediate();
        SetSettingsInteractionEnabled(false);
    }

    private void SetSettingsInteractionEnabled(bool enabled)
    {
        if (settingsSidebar == null || !settingsSidebar.TryGetComponent(out CanvasGroup canvasGroup))
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }
}
