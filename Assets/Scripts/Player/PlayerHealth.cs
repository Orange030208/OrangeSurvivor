using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerHealth : MonoBehaviour
{
    private float maxHealth;
    private float health;
    private float armor;
    private float lifeSteal;
    private float dodge;

    private float healthRecoverySpeed;
    private float healthRecoveryTimer;
    private float healthRecoveryDuration;

    public static event Action<Vector2> onAttackDodged;
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    private PropertiesManager propertiesManager;

    private void Awake()
    {
        propertiesManager = GetComponent<PropertiesManager>();
    }

    private void OnEnable()
    {
        Enemy.onDamageTaken += EnemyTookDamageCallback;
        GameEventBus.Subscribe<RequestPlayerHudSnapshotEvent>(PublishSnapshot);

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateAllProperties;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    private void OnDisable()
    {
        Enemy.onDamageTaken -= EnemyTookDamageCallback;
        GameEventBus.Unsubscribe<RequestPlayerHudSnapshotEvent>(PublishSnapshot);

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateAllProperties;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    private void Start()
    {
        UpdateAllProperties();
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
            float healthToAdd = Mathf.Min(.1f, maxHealth - health);
            health += healthToAdd;

            GameEventBus.Publish(new PlayerHealthChangedEvent(health, maxHealth));
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

        float realDamage = damage * Mathf.Clamp(1 - (armor / 1000), 0, 10000);
        realDamage = Mathf.Min(realDamage, health);
        health -= realDamage;

        GameEventBus.Publish(new PlayerHealthChangedEvent(health, maxHealth));

        if (health <= 0)
        {
            PassAway();
        }
    }

    private bool ShouldDodge()
    {
        return Random.Range(1, 101) <= dodge;
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
        GameEventBus.Publish(new PlayerHealthChangedEvent(health, maxHealth));
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        switch (propType)
        {
            case PropType.MaxHealth:
                UpdateMaxHealth();
                break;
            case PropType.Armor:
                armor = newValue;
                break;
            case PropType.LifeSteal:
                lifeSteal = newValue / 100;
                break;
            case PropType.Dodge:
                dodge = newValue;
                break;
            case PropType.HealthRecoverySpeed:
                UpdateHealthRecovery();
                break;
        }
    }

    private void UpdateAllProperties()
    {
        if (propertiesManager == null) return;

        UpdateMaxHealth();
        armor = propertiesManager.GetPropValue(PropType.Armor);
        lifeSteal = propertiesManager.GetPropValue(PropType.LifeSteal) / 100;
        dodge = propertiesManager.GetPropValue(PropType.Dodge);
        UpdateHealthRecovery();
    }

    private void UpdateMaxHealth()
    {
        if (propertiesManager == null) return;

        maxHealth = propertiesManager.GetPropValue(PropType.MaxHealth);
        maxHealth = Mathf.Max(maxHealth, 1);

        health = maxHealth;
        GameEventBus.Publish(new PlayerHealthChangedEvent(CurrentHealth, maxHealth));
    }

    private void UpdateHealthRecovery()
    {
        if (propertiesManager == null) return;

        healthRecoverySpeed = Mathf.Max(propertiesManager.GetPropValue(PropType.HealthRecoverySpeed), 0.00001f);
        healthRecoveryDuration = 1 / healthRecoverySpeed;
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
    }
}
