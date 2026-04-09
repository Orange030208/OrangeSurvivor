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
    
    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        GameEventBus.Subscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Subscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);

        // 请求快照，避免 UI 打开时错过早先事件
        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
        GameEventBus.Publish<RequestPlayerHudSnapshotEvent>();
        
        menuButton.onClick.AddListener(() => GameEventBus.Publish(new PauseGameRequestedEvent()));
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        GameEventBus.Unsubscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
        GameEventBus.Unsubscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
        
        menuButton.onClick.RemoveAllListeners();
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

    private void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (healthSlider != null)
            healthSlider.value = e.MaxHealth <= 0 ? 0 : e.CurrentHealth / e.MaxHealth;
        if (healthText != null)
            healthText.text = $"{(int)e.CurrentHealth} / {(int)e.MaxHealth}";
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