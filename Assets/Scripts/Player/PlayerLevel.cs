using System;
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [Header("经验")]
    private int requiredXP;
    private int currentXP;
    private int currentLevel = 1;
    private int levelOnWaveStart;

    public bool IsLevelUpInCurrentWave => currentLevel > levelOnWaveStart;

    public int LevelUpValue => currentLevel - levelOnWaveStart;
    public int CurrentLevel => currentLevel;    
    public int CurrentXP  => currentXP;
    public int RequiredXP => requiredXP;

    public static event Action<int> OnLevelChanged;
    public static event Action<int, int> OnXPChanged;

    private void OnEnable()
    {
        Candy.onCollected += CandyCollectedCallback;
        WaveManager.OnWaveStarted += OnWaveStart;
    }

    private void OnDisable()
    {
        Candy.onCollected -= CandyCollectedCallback;
        WaveManager.OnWaveStarted -= OnWaveStart;
    }

    private void Start()
    {
        RecaclRequiredXP();
        OnXPChanged?.Invoke(currentXP, requiredXP);
        OnLevelChanged?.Invoke(currentLevel);
    }

    private void RecaclRequiredXP()
    {
        requiredXP = currentLevel * 5;
    }

    private void CandyCollectedCallback(Candy candy)
    {
        currentXP++;
        OnXPChanged?.Invoke(currentXP, requiredXP);
        if (currentXP >= requiredXP)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        OnLevelChanged?.Invoke(currentLevel);
        currentXP = 0;
        OnXPChanged?.Invoke(currentXP, requiredXP);
        RecaclRequiredXP();
    }

    private void OnWaveStart(int waveCount, int totalWaves)
    {
        levelOnWaveStart = currentLevel;
    }
}
