using TMPro;
using UniversalUI.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalUI.Instances
{
    public class GamingUIPage : UIPageBase
    {
        [Header("Wave UI")] [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Player Health UI")] [SerializeField]
        private Slider healthSlider;

        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Player Level UI")] [SerializeField]
        private Slider xpBar;

        [SerializeField] private TextMeshProUGUI levelText;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            // 主动拉取当前数据进行初始化，防止 UI 晚于逻辑实例化导致丢失初始状态
            FetchAndApplyInitialData();
            
            WaveManager.OnWaveStarted += UpdateWaveText;
            WaveManager.OnAllWavesCompleted += ShowAllWavesCompleted;
            WaveManager.OnWaveProgress += UpdateTimerText;
            PlayerHealth.OnHealthChanged += UpdateHealthUI;
            PlayerLevel.OnLevelChanged += UpdateLevelUI;
            PlayerLevel.OnXPChanged += UpdateXPUI;
        }

        private void FetchAndApplyInitialData()
        {
            UpdateWaveText(WaveManager.Instance.CurrentWave, WaveManager.Instance.TotalWaves);

            //TODO:暂时这样写，框架修改时一起重写
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();
            UpdateLevelUI(playerLevel.CurrentLevel);
            UpdateXPUI(playerLevel.CurrentXP, playerLevel.RequiredXP);
        }

        protected override void OnPageClosed()
        {
            WaveManager.OnWaveStarted -= UpdateWaveText;
            WaveManager.OnAllWavesCompleted -= ShowAllWavesCompleted;
            WaveManager.OnWaveProgress -= UpdateTimerText;
            PlayerHealth.OnHealthChanged -= UpdateHealthUI;
            PlayerLevel.OnLevelChanged -= UpdateLevelUI;
            PlayerLevel.OnXPChanged -= UpdateXPUI;
        }

        private void UpdateWaveText(int currentWave, int totalWaves)
        {
            if (waveText == null) return;
            waveText.text = $"波次 {currentWave}/{totalWaves}";
        }

        private void ShowAllWavesCompleted()
        {
            if (waveText == null) return;
            waveText.text = "所有波次已完成!";
            if (timerText != null) timerText.text = "";
        }

        private void UpdateTimerText(float remainingTime, float totalTime)
        {
            if (timerText == null) return;
            timerText.text = $"{Mathf.RoundToInt(remainingTime)}s / {Mathf.RoundToInt(totalTime)}s";
        }

        private void UpdateHealthUI(float currentHealth, float maxHealth)
        {
            if (healthSlider != null)
                healthSlider.value = currentHealth / maxHealth;
            if (healthText != null)
                healthText.text = $"{(int)currentHealth} / {(int)maxHealth}";
        }

        private void UpdateLevelUI(int currentLevel)
        {
            if (levelText != null)
                levelText.text = "lvl" + currentLevel;
        }

        private void UpdateXPUI(int currentXP, int requiredXP)
        {
            if (xpBar != null)
                xpBar.value = (float)currentXP / requiredXP;
        }
    }
}