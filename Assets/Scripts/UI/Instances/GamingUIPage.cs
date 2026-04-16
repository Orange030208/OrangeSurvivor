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

    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private MobileJoystick moveJoystick;

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

        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
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
            levelText.text = "lvl" + e.currentLevel;
    }

    private void OnPlayerXpChanged(PlayerXpChangedEvent e)
    {
        if (xpBar != null)
            xpBar.value = e.requiredXP <= 0 ? 0 : (float)e.currentXP / e.requiredXP;
    }
}
