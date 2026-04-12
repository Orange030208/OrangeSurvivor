using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 通用生命组件：
/// - 维护当前生命值与最大生命值；
/// - 处理受伤、治疗、死亡、闪避、回血；
/// - 与 PropertiesManager 同步防御、吸血、恢复等属性；
/// - 对外广播生命变化和受伤事件。
/// 当前它既承担基础生命逻辑，也承担一部分事件桥接职责；
/// 如果以后战斗系统继续复杂化，可以再把部分责任拆到更细的系统中。
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Inspector")]
    [Tooltip("没有 PropertiesManager 时使用的默认最大生命值。")]
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

    /// <summary>
    /// 统一处理一次伤害：闪避、减伤、事件广播、死亡判定都在这里完成。
    /// </summary>
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

    /// <summary>
    /// 按每秒恢复值累积回血，使用 buffer 处理小数恢复量。
    /// </summary>
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

    /// <summary>
    /// 监听别的实体受伤事件，用于实现吸血。
    /// 当前逻辑比较直接：自己只要没满血，就按 lifeStealRatio 从造成的伤害里吸回生命。
    /// 如果以后要区分来源、队伍或伤害类型，建议在事件层补充更多上下文。
    /// </summary>
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

    /// <summary>
    /// 更新最大生命值，并决定是否按当前血量比例保留生命。
    /// </summary>
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
