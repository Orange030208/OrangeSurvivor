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

        if (buffBarUI != null)
        {
            buffBarUI.gameObject.SetActive(true);
        }

        if (tooltipPresenter != null)
        {
            tooltipPresenter.gameObject.SetActive(true);
        }

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
    }

    private void BindPlayerHealth(Player player)
    {
        UnbindPlayerHealth();
        if (player == null)
        {
            return;
        }

        playerHealthComponent = player.GetComponent<HealthComponent>();
        if (playerHealthComponent == null)
        {
            return;
        }

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
        if (waveText == null) return;
        waveText.text = $"波次 {e.CurrentWave}/{e.TotalWaves}";
    }

    private void OnAllWavesCompleted()
    {
        if (waveText == null) return;
        waveText.text = "所有波次已完成!";
        if (timerText != null) timerText.text = "";
    }

    private void OnWaveProgress(WaveProgressEvent e)
    {
        if (timerText == null) return;
        timerText.text = $"{Mathf.RoundToInt(e.RemainingTime)}s / {Mathf.RoundToInt(e.TotalTime)}s";
    }

    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
            healthSlider.value = maxHealth <= 0 ? 0 : currentHealth / maxHealth;
        if (healthText != null)
            healthText.text = $"{(int)currentHealth} / {(int)maxHealth}";
    }

    private void OnPlayerLevelChanged(PlayerLevelChangedEvent e)
    {
        if (levelText != null)
        {
            levelText.text = "lvl" + e.CurrentLevel;
        }

        UpdateUpgradePointText(e.UnspentUpgradePoints);
    }

    private void OnPlayerXpChanged(PlayerXpChangedEvent e)
    {
        if (xpBar != null)
        {
            xpBar.value = e.RequiredXP <= 0 ? 0 : (float)e.CurrentXP / e.RequiredXP;
        }

        UpdateUpgradePointText(e.UnspentUpgradePoints);
    }

    private void UpdateUpgradePointText(int unspentUpgradePoints)
    {
        if (upgradePointText == null)
        {
            return;
        }

        upgradePointText.text = unspentUpgradePoints > 0
            ? $"UP {unspentUpgradePoints}"
            : string.Empty;
    }
}
