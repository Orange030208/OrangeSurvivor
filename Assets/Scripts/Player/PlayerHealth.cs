using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("设置")] 
    [SerializeField] 
    private int maxHealth = 10;
    private int health;
    
    [Header("UI")]
    [SerializeField]private Slider healthSlider;
    [SerializeField]private TextMeshProUGUI healthText;
    private void Start()
    {
        health = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        int realDamage = Math.Min(damage, health);
        health -= realDamage;

        UpdateUI();
        
        if (health <= 0)
        {
            PassAway();
        }
    }

    private void UpdateUI()
    {
        healthSlider.value = (float)health / maxHealth;
        healthText.text = $"{health} / {maxHealth}";
    }

    private void PassAway()
    {
        Debug.Log("玩家挂了");
        SceneManager.LoadScene(0);
    }
}