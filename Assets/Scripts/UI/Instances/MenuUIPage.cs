using System.Threading;
using Orange.UIFramework;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class MenuUIPage : PageBase
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
