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
    private IPlayerMoveInputReceiver moveInputReceiver;
    private PlayerLevel playerLevel;
    private bool hudEventsBound;

    public override bool RequiresTick => true;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        ResolveViewParts();
        inventoryUI?.WarmUp();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamingPageContext>()
            ?? throw new InvalidOperationException($"{nameof(GamingUIPage)} requires {nameof(GamingPageContext)} payload.");

        BindInput(currentContext.Player);
        inventoryUI?.ConfigureSession(currentContext.InventoryOperateManager, OwnerUIManager);
        BindHud(currentContext);
        menuButton.OnClicked += OnPauseClicked;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindInput();
        UnbindHud();
        menuButton.OnClicked -= OnPauseClicked;
        inventoryUI?.ReleaseSession();
        currentContext = null;
    }

    protected override void OnTick(float deltaTime)
    {
        moveInputReceiver?.SetMoveInput(ReadMoveDirection());
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new PauseGameRequestedEvent());
    }

    private void BindInput(Player player)
    {
        moveInputReceiver = player != null ? player.GetComponent<IPlayerMoveInputReceiver>() : null;
        moveInputReceiver?.SetMoveInput(Vector2.zero);
    }

    private void UnbindInput()
    {
        moveInputReceiver?.SetMoveInput(Vector2.zero);
        moveInputReceiver = null;
    }

    private Vector2 ReadMoveDirection()
    {
        return moveJoystick != null ? moveJoystick.GetMoveDirection() : Vector2.zero;
    }

    private void BindHud(GamingPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        UnbindHud();
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        hudEventsBound = true;

        BindPlayerHud(context.Player);
        RefreshCurrencyDisplay(context.CurrencyWallet);

        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
    }

    private void UnbindHud()
    {
        if (hudEventsBound)
        {
            GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
            GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
            GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            hudEventsBound = false;
        }

        UnbindPlayerLevel();
        characterStatusPanel.Unbind();
        buffBarUI.EndSession();
    }

    private void BindPlayerHud(Player player)
    {
        UnbindPlayerLevel();
        characterStatusPanel.BindPlayer(player);
        buffBarUI.BeginSession(player, OwnerUIManager);

        playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
        if (playerLevel == null)
        {
            return;
        }

        playerLevel.SnapshotChanged += OnPlayerLevelSnapshotChanged;
        OnPlayerLevelSnapshotChanged(playerLevel.CreateSnapshot());
    }

    private void UnbindPlayerLevel()
    {
        if (playerLevel == null)
        {
            return;
        }

        playerLevel.SnapshotChanged -= OnPlayerLevelSnapshotChanged;
        playerLevel = null;
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshCurrencyDisplay(eventData.Wallet);
    }

    private void RefreshCurrencyDisplay(CurrencyWallet wallet)
    {
        currencyText.text = wallet != null ? wallet.CurrentAmount.ToString() : "0";
    }

    private void OnPlayerLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        characterStatusPanel.SetLevel(snapshot.CurrentLevel);
        characterStatusPanel.SetXp(snapshot.CurrentXP, snapshot.RequiredXP);
        characterStatusPanel.SetUpgradePoint(snapshot.UnspentUpgradePoints);
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        waveText.text = $"波次 {eventData.CurrentWave}/{eventData.TotalWaves}";
    }

    private void OnAllWavesCompleted()
    {
        waveText.text = "所有波次已完成!";
        timerText.text = string.Empty;
    }

    private void OnWaveProgress(WaveProgressEvent eventData)
    {
        timerText.text = $"{Mathf.RoundToInt(eventData.RemainingTime)}s / {Mathf.RoundToInt(eventData.TotalTime)}s";
    }

    private void ValidateConfiguration()
    {
        ResolveViewParts();

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

    private void ResolveViewParts()
    {
        if (inventoryUI == null)
        {
            inventoryUI = GetComponentInChildren<InventoryUI>(true);
        }
    }
}
