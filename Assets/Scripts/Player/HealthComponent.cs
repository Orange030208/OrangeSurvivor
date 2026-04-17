using System;
using UnityEngine;

/// <summary>
/// 通用生命组件：
/// - 维护当前生命值与最大生命值；
/// - 处理已结算 hit 的应用、治疗、死亡、回血；
/// - 与 PropertiesManager 同步防御、吸血、恢复等属性；
/// - 对外广播生命变化和受伤事件。
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Inspector")]
    [Tooltip("没有 PropertiesManager 时使用的默认最大生命值。")]
    [SerializeField] private float defaultMaxHealth = 1f;

    private float maxHealth;
    private float health;
    private float lifeStealRatio;
    private float healthRecoveryPerSecond;
    private float recoveryBuffer;

    public event Action<float, float> OnHealthChanged;
    public event Action<HitResult> OnDamaged;
    public event Action OnDied;
    public event Action<Vector2> OnDamageDodged;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;
    public Entity OwnerEntity => ownerEntity;

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
        if (!GameSimulation.IsRunning)
        {
            return;
        }

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

    public void ApplyHitResult(HitResult result)
    {
        if (health <= 0f || result.Target != ownerEntity)
        {
            return;
        }

        if (result.IsCancelled)
        {
            return;
        }

        if (result.IsDodged)
        {
            OnDamageDodged?.Invoke(result.HitPoint);
            return;
        }

        float realDamage = Mathf.Min(result.FinalDamage, health);
        if (realDamage <= 0f)
        {
            return;
        }

        health -= realDamage;

        HitResult appliedResult = result.WithFinalDamage(realDamage);
        OnDamaged?.Invoke(appliedResult);
        if (ownerEntity != null)
        {
            GameEventBus.Publish(new EntityDamagedEvent(ownerEntity, appliedResult));
        }

        PublishHealthChanged();

        if (health <= 0f)
        {
            HandleDeath();
        }
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
        float lifeStealValue = damageEvent.HitResult.FinalDamage * lifeStealRatio * healingPower;
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
            case PropType.LifeSteal:
                lifeStealRatio = Mathf.Max(0f, newValue);
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
        lifeStealRatio = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.LifeSteal));
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
