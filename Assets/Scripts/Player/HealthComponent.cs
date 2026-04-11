using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float defaultMaxHealth = 1f;

    private float maxHealth;
    private float health;
    private float armor;
    private float lifeStealRatio;
    private float dodgeChance;
    private float healthRecoveryPerSecond;
    private float recoveryBuffer;

    public event Action<float, float> OnHealthChanged;
    public event Action<DamageInfo> OnDamaged;
    public event Action OnDied;
    public event Action<Vector2> OnDamageDodged;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    private PropertiesManager propertiesManager;
    private Entity ownerEntity;

    private void Awake()
    {
        propertiesManager = GetComponent<PropertiesManager>();
        ownerEntity = GetComponent<Entity>();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateAllProperties;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateAllProperties;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    private void Start()
    {
        if (propertiesManager != null)
        {
            UpdateAllProperties();
            return;
        }

        if (maxHealth <= 0f)
        {
            SetMaxHealth(defaultMaxHealth, false);
        }
        else
        {
            PublishHealthChanged();
        }
    }

    private void Update()
    {
        if (health < maxHealth)
        {
            RecoveryHealth();
        }
    }

    public void Initialize(float initialMaxHealth, bool resetCurrentHealth = true)
    {
        SetMaxHealth(initialMaxHealth, !resetCurrentHealth);
        if (resetCurrentHealth)
        {
            health = maxHealth;
            PublishHealthChanged();
        }
    }

    public void TakeDamage(float damage)
    {
        ApplyDamage(new DamageInfo(damage, transform.position, false));
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        ApplyDamage(damageInfo);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || health <= 0f)
        {
            return;
        }

        float actualHeal = Mathf.Min(amount, maxHealth - health);
        if (actualHeal <= 0f)
        {
            return;
        }

        health += actualHeal;
        PublishHealthChanged();
    }

    private void ApplyDamage(DamageInfo damageInfo)
    {
        if (health <= 0f)
        {
            return;
        }

        if (ShouldDodge())
        {
            OnDamageDodged?.Invoke(transform.position);
            return;
        }

        float damageReduction = Mathf.Clamp01(armor + GetDamageReduction());
        float realDamage = Mathf.Min(damageInfo.damage * (1f - damageReduction), health);
        health -= realDamage;

        DamageInfo appliedDamage = new(realDamage, damageInfo.position, damageInfo.isCritical);
        OnDamaged?.Invoke(appliedDamage);
        if (ownerEntity != null)
        {
            GameEventBus.Publish(new EntityDamagedEvent(ownerEntity, appliedDamage));
        }

        PublishHealthChanged();

        if (health <= 0f)
        {
            HandleDeath();
        }
    }

    private float GetDamageReduction()
    {
        return propertiesManager != null ? propertiesManager.GetPropValue(PropType.DamageReduction) : 0f;
    }

    private void RecoveryHealth()
    {
        if (healthRecoveryPerSecond <= 0f)
        {
            return;
        }

        recoveryBuffer += healthRecoveryPerSecond * Time.deltaTime;
        if (recoveryBuffer < 1f)
        {
            return;
        }

        float healAmount = Mathf.Floor(recoveryBuffer);
        float actualHeal = Mathf.Min(healAmount, maxHealth - health);
        if (actualHeal <= 0f)
        {
            return;
        }

        recoveryBuffer -= actualHeal;
        health += actualHeal;
        PublishHealthChanged();
    }

    private bool ShouldDodge()
    {
        return dodgeChance > 0f && Random.value <= dodgeChance;
    }

    private void HandleDeath()
    {
        OnDied?.Invoke();
        if (ownerEntity != null)
        {
            GameEventBus.Publish(new EntityDiedEvent(ownerEntity, transform.position));
        }
    }

    private void OnEntityDamaged(EntityDamagedEvent damageEvent)
    {
        if (damageEvent.Entity == ownerEntity)
        {
            return;
        }

        if (health >= maxHealth || lifeStealRatio <= 0f)
        {
            return;
        }

        float healingPower = propertiesManager != null ? Mathf.Max(0f, propertiesManager.GetPropValue(PropType.HealingPower)) : 1f;
        float lifeStealValue = damageEvent.DamageInfo.damage * lifeStealRatio * healingPower;
        float healthToAdd = Math.Min(lifeStealValue, maxHealth - health);
        if (healthToAdd <= 0f)
        {
            return;
        }

        health += healthToAdd;
        PublishHealthChanged();
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        switch (propType)
        {
            case PropType.MaxHealth:
                UpdateMaxHealth();
                break;
            case PropType.Armor:
                armor = Mathf.Clamp01(newValue);
                break;
            case PropType.LifeSteal:
                lifeStealRatio = Mathf.Max(0f, newValue);
                break;
            case PropType.Dodge:
                dodgeChance = Mathf.Clamp01(newValue);
                break;
            case PropType.HealthRecoverySpeed:
                healthRecoveryPerSecond = Mathf.Max(0f, newValue);
                break;
        }
    }

    private void UpdateAllProperties()
    {
        if (propertiesManager == null)
        {
            return;
        }

        UpdateMaxHealth();
        armor = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.Armor));
        lifeStealRatio = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.LifeSteal));
        dodgeChance = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.Dodge));
        healthRecoveryPerSecond = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.HealthRecoverySpeed));
    }

    private void UpdateMaxHealth()
    {
        if (propertiesManager == null)
        {
            return;
        }

        SetMaxHealth(propertiesManager.GetPropValue(PropType.MaxHealth), true);
    }

    private void SetMaxHealth(float value, bool preserveRatio)
    {
        float oldMaxHealth = Mathf.Max(maxHealth, 1f);
        float healthPercent = oldMaxHealth > 0f ? health / oldMaxHealth : 1f;

        maxHealth = Mathf.Max(value, 1f);
        if (!preserveRatio || health <= 0f)
        {
            health = maxHealth;
        }
        else
        {
            health = Mathf.Clamp(maxHealth * healthPercent, 0f, maxHealth);
            if (health <= 0f)
            {
                health = maxHealth;
            }
        }

        PublishHealthChanged();
    }

    public void PublishHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
