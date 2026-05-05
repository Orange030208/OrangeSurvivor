using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : PageBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private CharacterStatusPanel characterStatusPanel;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private MobileJoystick moveJoystick;
    [SerializeField] private BuffBarUI buffBarUI;

    private GamingPageContext currentContext;
    private GamingHudView hudView;
    private GamingInputView inputView;

    public override bool RequiresTick => true;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiBinder.WarmUp(this, ref inventoryUI);
        hudView = new GamingHudView(name, waveText, timerText, currencyText, characterStatusPanel, buffBarUI);
        inputView = new GamingInputView(this, moveJoystick);
        inputView.WarmUp();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamingPageContext>()
            ?? throw new InvalidOperationException($"{nameof(GamingUIPage)} requires {nameof(GamingPageContext)} payload.");

        inputView.WarmUp();
        inputView.Bind(currentContext.Player);
        InventoryUiBinder.Bind(this, ref inventoryUI, currentContext, OwnerUIManager);
        hudView.ConfigureUIManager(OwnerUIManager);
        hudView.Bind(currentContext);
        menuButton.OnClicked += OnPauseClicked;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        inputView.Unbind();
        hudView.Unbind();
        menuButton.OnClicked -= OnPauseClicked;
        InventoryUiBinder.Release(inventoryUI);
        currentContext?.Dispose();
        currentContext = null;
    }

    protected override void OnTick(float deltaTime)
    {
        inputView.PublishCurrentInput();
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

        if (moveJoystick == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing move joystick.");
        }
    }
}
