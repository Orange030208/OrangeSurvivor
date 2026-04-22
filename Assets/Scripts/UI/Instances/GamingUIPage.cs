using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : UIPageBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private CharacterStatusPanel characterStatusPanel;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private MobileJoystick moveJoystick;
    [SerializeField] private BuffBarUI buffBarUI;
    [SerializeField] private UITooltipPresenter tooltipPresenter;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        CacheMoveJoystick();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Subscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Subscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        CacheMoveJoystick();
        BindCharacterStatusPanel(FindFirstObjectByType<Player>());
        RefreshCurrencyDisplay(FindFirstObjectByType<CurrencyWallet>());

        buffBarUI.gameObject.SetActive(true);
        tooltipPresenter.gameObject.SetActive(true);

        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
        GameEventBus.Publish<RequestPlayerLevelSnapshotEvent>();
        menuButton.OnClicked += OnPauseClicked;
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Unsubscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Unsubscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        PublishMoveInput(Vector2.zero);
        characterStatusPanel.Unbind();
        GameEventBus.Publish<HideTooltipRequestedEvent>();
        menuButton.OnClicked -= OnPauseClicked;
    }

    protected override void OnPageTick(float deltaTime)
    {
        PublishMoveInput(moveJoystick.GetMoveDirection());
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new PauseGameRequestedEvent());
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        BindCharacterStatusPanel(eventData.Player);
        RefreshCurrencyDisplay(eventData.Player.GetComponent<CurrencyWallet>());
        GameEventBus.Publish<RequestPlayerLevelSnapshotEvent>();
    }

    private void BindCharacterStatusPanel(Player player)
    {
        characterStatusPanel.BindPlayer(player);
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshCurrencyDisplay(eventData.Wallet);
    }

    private void RefreshCurrencyDisplay(CurrencyWallet wallet)
    {
        currencyText.text = wallet != null ? wallet.CurrentAmount.ToString() : "0";
    }

    private void OnPlayerLevelChanged(PlayerLevelChangedEvent eventData)
    {
        characterStatusPanel.SetLevel(eventData.CurrentLevel);
        characterStatusPanel.SetUpgradePoint(eventData.UnspentUpgradePoints);
    }

    private void OnPlayerXpChanged(PlayerXpChangedEvent eventData)
    {
        characterStatusPanel.SetXp(eventData.CurrentXP, eventData.RequiredXP);
        characterStatusPanel.SetUpgradePoint(eventData.UnspentUpgradePoints);
    }

    private void CacheMoveJoystick()
    {
        if (moveJoystick == null)
        {
            moveJoystick = GetComponentInChildren<MobileJoystick>(true);
        }
    }

    private void PublishMoveInput(Vector2 moveDirection)
    {
        GameEventBus.Publish(new PlayerMoveInputChangedEvent(moveDirection));
    }

    private void OnWaveStarted(WaveStartedEvent e)
    {
        waveText.text = $"波次 {e.CurrentWave}/{e.TotalWaves}";
    }

    private void OnAllWavesCompleted()
    {
        waveText.text = "所有波次已完成!";
        timerText.text = string.Empty;
    }

    private void OnWaveProgress(WaveProgressEvent e)
    {
        timerText.text = $"{Mathf.RoundToInt(e.RemainingTime)}s / {Mathf.RoundToInt(e.TotalTime)}s";
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
    }
}
