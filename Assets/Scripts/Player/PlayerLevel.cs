using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevel : MonoBehaviour
{
    [Header("经验")] 
    private int requiredXP;
    private int currentXP;
    private int currentLevel = 1;
    
    [Header("UI")]
    [SerializeField]private TextMeshProUGUI levelText;
    [SerializeField]private Slider xpBar;

    private void OnEnable()
    {
        Candy.onCollected += CandyCollectedCallback;
    }

    private void OnDisable()
    {
        Candy.onCollected -= CandyCollectedCallback;
    }

    private void Start()
    {
        RecaclRequiredXP();
        UpdateUI();
    }

    private void RecaclRequiredXP()
    {
        requiredXP = currentLevel * 5;
    }

    private void UpdateUI()
    {
        xpBar.value = (float)currentXP /  requiredXP;
        levelText.text = "lvl" + currentLevel;
    }

    private void CandyCollectedCallback(Candy candy)
    {
        currentXP++;
        if (currentXP >= requiredXP)
        {
            LevelUp();
        }
        UpdateUI();
    }

    private void LevelUp()
    {
        currentLevel++;
        currentXP = 0;
        RecaclRequiredXP();
    }
}
