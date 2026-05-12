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
    [SerializeField] private Button menuButton;
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
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamingPageContext>()
            ?? throw new InvalidOperationException($"{nameof(GamingUIPage)} requires {nameof(GamingPageContext)} payload.");

        BindInput(currentContext.Player);
        BindHud(currentContext);
        GameInputService inputService = GameInputService.Instance;
        if (inputService != null)
        {
            inputService.PausePerformed += OnPauseClicked;
        }
        menuButton.onClick.AddListener(OnPauseClicked);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindInput();
        UnbindHud();
        GameInputService inputService = GameInputService.Instance;
        if (inputService != null)
        {
            inputService.PausePerformed -= OnPauseClicked;
        }

        menuButton.onClick.RemoveListener(OnPauseClicked);
        currentContext = null;
    }

    protected override void OnTick(float deltaTime)
    {
        moveInputReceiver?.SetMoveInput(ReadMoveDirection());
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
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
        GameInputService inputService = GameInputService.Instance;
        Vector2 inputMove = inputService != null ? inputService.Move : Vector2.zero;
        if (inputMove.sqrMagnitude > 0.0001f)
        {
            return Vector2.ClampMagnitude(inputMove, 1f);
        }

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
        ApplyWaveHud(context.WaveHudViewData);
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
        ApplyWaveTimer(eventData.ShowTimer, eventData.RemainingTime, eventData.TotalTime);
    }

    private void ApplyWaveHud(WaveHudViewData waveHudViewData)
    {
        if (waveHudViewData.HasStarted)
        {
            waveText.text = $"波次 {waveHudViewData.CurrentWave}/{waveHudViewData.TotalWaves}";
            ApplyWaveTimer(waveHudViewData.ShowTimer, waveHudViewData.RemainingTime, waveHudViewData.TotalTime);
            return;
        }

        waveText.text = "准备开始";
        timerText.text = string.Empty;
    }

    private void ApplyWaveTimer(bool showTimer, float remainingTime, float totalTime)
    {
        timerText.text = showTimer
            ? $"{Mathf.RoundToInt(remainingTime)}s / {Mathf.RoundToInt(totalTime)}s"
            : string.Empty;
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

        // Mobile joystick is optional on PC builds; GameInputService is the primary input source.
    }
}
