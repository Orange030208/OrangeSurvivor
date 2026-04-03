using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerHealth : MonoBehaviour,IPlayerStatusDependency
{
    [Header("设置")] 
    [SerializeField] 
    private int baseMaxHealth;
    private float maxHealth;
    private float health;
    private float armor;
    private float lifeSteal;
    private float dodge;

    private float healthRecoverySpeed;
    private float healthRecoveryTimer;
    private float healthRecoveryDuration;

    public static event Action<float, float> OnHealthChanged;
    public static event Action<Vector2> onAttackDodged;
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    private void OnEnable()
    {
        Enemy.onDamageTaken += EnemyTookDamageCallback;
    }

    private void OnDisable()
    {
        Enemy.onDamageTaken -= EnemyTookDamageCallback;
    }

    private void Update()
    {
        if (health < maxHealth)
        {
            RecoveryHealth();
        }
    }

    private void RecoveryHealth()
    {
        healthRecoveryTimer += Time.deltaTime;

        if (healthRecoveryTimer >= healthRecoveryDuration)
        {
            healthRecoveryTimer = 0;
            float healthToAdd = Mathf.Min(.1f,maxHealth - health);
            health += healthToAdd;
            
            OnHealthChanged?.Invoke(health, maxHealth);
        }
    }


    public void TakeDamage(float damage)
    {
        if (ShouldDodge())
        {
            onAttackDodged?.Invoke(transform.position);
            print("闪避");
            return;
        }
        float realDamage = damage * Mathf.Clamp(1 - (armor / 1000),0,10000);
        realDamage = Mathf.Min(realDamage, health);
        health -= realDamage;

        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            PassAway();
        }
    }

    private bool ShouldDodge()
    {
        return Random.Range(1,101) <= dodge;
    }

    private void PassAway()
    {
        Debug.Log("玩家挂了");
        GameManager.Instance.GameOver();
    }
    
    private void EnemyTookDamageCallback(DamageInfo damageInfo)
    {
        if (health >= maxHealth) return;
        float lifeStyleValue = damageInfo.damage * lifeSteal;
        float healthToAdd = Math.Min(lifeStyleValue, maxHealth - health);
        
        health += healthToAdd;
        OnHealthChanged?.Invoke(health, maxHealth);
    }

    public void UpdateStatus(PropertiesManager propertiesManager)
    {
        float addedHealth = propertiesManager.GetPropValue(PropType.MaxHealth);
        maxHealth = baseMaxHealth + (int)addedHealth;
        maxHealth = Mathf.Max(maxHealth, 1);

        health = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        
        armor = propertiesManager.GetPropValue(PropType.Armor);
        lifeSteal =  propertiesManager.GetPropValue(PropType.LifeSteal)/100;
        dodge = propertiesManager.GetPropValue(PropType.Dodge);

        healthRecoverySpeed = Mathf.Max(propertiesManager.GetPropValue(PropType.HealthRecoverySpeed), 0.00001f);
        healthRecoveryDuration = 1 /  healthRecoverySpeed;
    }
}