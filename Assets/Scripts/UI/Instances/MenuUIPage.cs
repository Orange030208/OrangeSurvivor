using AXR.Framework.UI;
using UnityEngine;

public class MenuUIPage : UIPageBase
{
    [SerializeField] private UIClickTarget startButton;
    [SerializeField] private UIClickTarget characterSelectButton;
    [SerializeField] private UIClickTarget settingsButton;
    [SerializeField] private MonoBehaviour settingsSidebar;

    private IUIRuntimeMotion settingsMotion;

    private bool settingsVisible;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
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
        IUIRuntimeMotion motion = ResolveSettingsMotion();

        settingsVisible = visible;
        motion.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
        SetSettingsInteractionEnabled(visible);
    }

    private void HideSettingsImmediate()
    {
        settingsVisible = false;
        ResolveSettingsMotion()?.SetImmediate(UIMotionClipIds.HIDE);
        SetSettingsInteractionEnabled(false);
    }

    private void SetSettingsInteractionEnabled(bool enabled)
    {
        if (!settingsSidebar.TryGetComponent(out CanvasGroup canvasGroup))
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private IUIRuntimeMotion ResolveSettingsMotion()
    {
        if (settingsMotion != null)
        {
            return settingsMotion;
        }

        if (settingsSidebar is IUIRuntimeMotion directMotion)
        {
            settingsMotion = directMotion;
            return settingsMotion;
        }

        if (settingsSidebar == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing settings sidebar.");
        }

        throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' settings sidebar must implement {nameof(IUIRuntimeMotion)}.");
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

        if (settingsSidebar == null)
        {
            throw new MissingReferenceException($"{nameof(MenuUIPage)} '{name}' is missing settings sidebar.");
        }
    }
}
