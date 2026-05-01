using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : UIPageBase, IInventoryUiFacadeHost
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private CharacterStatusPanel characterStatusPanel;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private MobileJoystick moveJoystick;
    [SerializeField] private BuffBarUI buffBarUI;
    [SerializeField] private UITooltipPresenter tooltipPresenter;

    private GamingPageContext currentContext;
    private GamingHudRegionHost hudRegionHost;
    private GamingInputRegionHost inputRegionHost;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiHostBinding.WarmUp(this, ref inventoryUI);
        hudRegionHost = new GamingHudRegionHost(name, waveText, timerText, currencyText, characterStatusPanel, buffBarUI, tooltipPresenter);
        inputRegionHost = new GamingInputRegionHost(this, moveJoystick);
        inputRegionHost.WarmUp();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        currentContext = PageContextBinding.Resolve<GamingPageContext>(context, () => UIPageContextFactory.CreateGamingPageContext());

        inputRegionHost.WarmUp();
        inputRegionHost.Bind(currentContext.Player);
        InventoryUiHostBinding.Bind(this, ref inventoryUI, currentContext);
        hudRegionHost.Bind(currentContext);
        menuButton.OnClicked += OnPauseClicked;
    }

    protected override void OnPageClosed()
    {
        inputRegionHost.Unbind();
        hudRegionHost.Unbind();
        menuButton.OnClicked -= OnPauseClicked;
        InventoryUiHostBinding.Release(inventoryUI);
        PageContextBinding.Release(ref currentContext);
    }

    protected override void OnPageTick(float deltaTime)
    {
        inputRegionHost.PublishCurrentInput();
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new PauseGameRequestedEvent());
    }

    private void ValidateConfiguration()
    {
        if (waveText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing wave text.");
        }

        if (timerText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing timer text.");
        }

        if (characterStatusPanel == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing character status panel.");
        }

        if (currencyText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing currency text.");
        }

        if (menuButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing menu button.");
        }

        if (buffBarUI == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing buff bar UI.");
        }

        if (tooltipPresenter == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing tooltip presenter.");
        }

        if (moveJoystick == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing move joystick.");
        }
    }
}
