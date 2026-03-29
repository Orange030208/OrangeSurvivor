using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("设置")]
    [SerializeField]
    private int maxHealth = 10;
    private int health;

    public static event Action<int, int> OnHealthChanged;
    public int CurrentHealth => health;
    public int MaxHealth => maxHealth;

    private void Start()
    {
        health = maxHealth;
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        int realDamage = Math.Min(damage, health);
        health -= realDamage;

        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            PassAway();
        }
    }

    private void PassAway()
    {
        Debug.Log("玩家挂了");
        GameManager.Instance.GameOver();
    }
}