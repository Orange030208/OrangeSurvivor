using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : PageBase, IInventoryUiFacadeHost
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

    public override bool RequiresTick => true;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiHostBinding.WarmUp(this, ref inventoryUI);
        hudRegionHost = new GamingHudRegionHost(name, waveText, timerText, currencyText, characterStatusPanel, buffBarUI, tooltipPresenter);
        inputRegionHost = new GamingInputRegionHost(this, moveJoystick);
        inputRegionHost.WarmUp();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamingPageContext>()
            ?? throw new InvalidOperationException($"{nameof(GamingUIPage)} requires {nameof(GamingPageContext)} payload.");

        inputRegionHost.WarmUp();
        inputRegionHost.Bind(currentContext.Player);
        InventoryUiHostBinding.Bind(this, ref inventoryUI, currentContext);
        hudRegionHost.Bind(currentContext);
        menuButton.OnClicked += OnPauseClicked;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        inputRegionHost.Unbind();
        hudRegionHost.Unbind();
        menuButton.OnClicked -= OnPauseClicked;
        InventoryUiHostBinding.Release(inventoryUI);
        PageContextBinding.Release(ref currentContext);
    }

    protected override void OnTick(float deltaTime)
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
