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

    [SerializeField] private Button menuButton;

    private HealthComponent playerHealthComponent;
    
    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Subscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Subscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);

        Player player = FindObjectOfType<Player>();
        playerHealthComponent = player != null ? player.GetComponent<HealthComponent>() : null;
        if (playerHealthComponent != null)
        {
            playerHealthComponent.OnHealthChanged += OnPlayerHealthChanged;
            OnPlayerHealthChanged(playerHealthComponent.CurrentHealth, playerHealthComponent.MaxHealth);
        }

        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
        menuButton?.onClick.AddListener(() => GameEventBus.Publish(new PauseGameRequestedEvent()));
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Unsubscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Unsubscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);

        if (playerHealthComponent != null)
        {
            playerHealthComponent.OnHealthChanged -= OnPlayerHealthChanged;
            playerHealthComponent = null;
        }
        
        menuButton?.onClick.RemoveAllListeners();
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
