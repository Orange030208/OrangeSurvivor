using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : UIPageBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;

    [SerializeField]
    private Slider healthSlider;

    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField]
    private Slider xpBar;

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI upgradePointText;

    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private MobileJoystick moveJoystick;
    [SerializeField] private BuffBarUI buffBarUI;
    [SerializeField] private UITooltipPresenter tooltipPresenter;

    private HealthComponent playerHealthComponent;

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

        CacheMoveJoystick();
        BindPlayerHealth(FindFirstObjectByType<Player>());

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

        PublishMoveInput(Vector2.zero);
        UnbindPlayerHealth();
        GameEventBus.Publish<HideTooltipRequestedEvent>();
        menuButton.OnClicked -= OnPauseClicked;
    }

    protected override void OnPageTick(float deltaTime)
    {
        PublishMoveInput(moveJoystick.GetMoveDirection());
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        BindPlayerHealth(eventData.Player);
        GameEventBus.Publish<RequestPlayerLevelSnapshotEvent>();
    }

    private void BindPlayerHealth(Player player)
    {
        UnbindPlayerHealth();
        if (player == null)
        {
            return;
        }

        playerHealthComponent = player.GetComponent<HealthComponent>();
        playerHealthComponent.OnHealthChanged += OnPlayerHealthChanged;
        OnPlayerHealthChanged(playerHealthComponent.CurrentHealth, playerHealthComponent.MaxHealth);
    }

    private void UnbindPlayerHealth()
    {
        if (playerHealthComponent == null)
        {
            return;
        }

        playerHealthComponent.OnHealthChanged -= OnPlayerHealthChanged;
        playerHealthComponent = null;
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new PauseGameRequestedEvent());
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

    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        healthSlider.value = maxHealth <= 0 ? 0 : currentHealth / maxHealth;
        healthText.text = $"{(int)currentHealth} / {(int)maxHealth}";
    }

    private void OnPlayerLevelChanged(PlayerLevelChangedEvent e)
    {
        levelText.text = "lvl" + e.CurrentLevel;
        UpdateUpgradePointText(e.UnspentUpgradePoints);
    }

    private void OnPlayerXpChanged(PlayerXpChangedEvent e)
    {
        xpBar.value = e.RequiredXP <= 0 ? 0 : (float)e.CurrentXP / e.RequiredXP;
        UpdateUpgradePointText(e.UnspentUpgradePoints);
    }

    private void UpdateUpgradePointText(int unspentUpgradePoints)
    {
        upgradePointText.text = unspentUpgradePoints > 0
            ? $"UP {unspentUpgradePoints}"
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

        if (healthSlider == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing health slider.");
        }

        if (healthText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing health text.");
        }

        if (xpBar == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing xp bar.");
        }

        if (levelText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing level text.");
        }

        if (upgradePointText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing upgrade point text.");
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
