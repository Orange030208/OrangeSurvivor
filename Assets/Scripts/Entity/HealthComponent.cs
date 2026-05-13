using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 通用生命组件：
/// - 维护当前生命值与最大生命值；
/// - 处理已结算 hit 的应用、治疗、死亡、回血；
/// - 与 PropertiesManager 同步防御、吸血、恢复等属性；
/// - 对外广播生命变化和受伤事件。
/// </summary>
public class HealthComponent : EntityComponentBase
{
    private const float LIFE_STEAL_HEAL_RATE_PER_RATIO = 0.1f;

    [Header("检视面板")]
    [Tooltip("没有属性管理器时使用的默认最大生命值。")]
    [SerializeField]
    private float defaultMaxHealth = 1f;

    private float maxHealth;
    private float health;
    private float lifeStealRatio;
    private float healthRecoveryPerSecond;
    private float recoveryBuffer;
    private Entity owner;
    private PropertiesManager propertiesManager;
    private bool isDeathSequenceRunning;
    private Entity lastDamageSource;

    public event Action<float, float> OnHealthChanged;
    public event Action<HitResult> OnDamaged;
    public event Action OnDeathSequenceStarted;
    public event Func<IEnumerator> OnDeathSequenceRequested;
    public event Action OnDeathSequenceCompleted;
    public event Action<Vector2> OnDamageDodged;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;
    public bool IsDeathSequenceRunning => isDeathSequenceRunning;
    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        propertiesManager = owner.GetComponent<PropertiesManager>();

        InitializeRuntimeState();
        SubscribeEvents();
    }

    private void InitializeRuntimeState()
    {
        recoveryBuffer = 0f;

        if (propertiesManager != null)
        {
            UpdateAllProperties();
            return;
        }

        lifeStealRatio = 0f;
        healthRecoveryPerSecond = 0f;
        SetMaxHealth(defaultMaxHealth, false);
    }

    private void SubscribeEvents()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged += UpdateAllProperties;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    public override void OnDisableComponent()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged -= UpdateAllProperties;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }

    public override void OnTick(float deltaTime)
    {
        if (health < maxHealth)
        {
            RecoveryHealth(deltaTime);
        }
    }

    public void ApplyHitResult(HitResult result)
    {
        if (health <= 0f || result.Target != owner)
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

        if (result.IsBlocked)
        {
            return;
        }

        //暂时不屏蔽超出伤害了，后续有需要再修改
        float realDamage = result.FinalDamage;
        if (realDamage <= 0f)
        {
            return;
        }

        health -= realDamage;

        HitResult appliedResult = result.WithFinalDamage(realDamage);
        lastDamageSource = appliedResult.Source;
        OnDamaged?.Invoke(appliedResult);
        GameEventBus.Publish(new EntityDamagedEvent(owner, appliedResult));
        ApplyLifeStealToSource(appliedResult);

        PublishHealthChanged();

        if (health <= 0f)
        {
            HandleDeath(EntityDeathReason.Combat);
        }
    }

    public bool ForceDeath(Entity source, EntityDeathReason deathReason)
    {
        if (isDeathSequenceRunning || health <= 0f)
        {
            return false;
        }

        lastDamageSource = source;
        health = 0f;
        PublishHealthChanged();
        HandleDeath(deathReason);
        return true;
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

    private void RecoveryHealth(float deltaTime)
    {
        if (healthRecoveryPerSecond <= 0f)
        {
            return;
        }

        recoveryBuffer += healthRecoveryPerSecond * deltaTime;
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

    private void HandleDeath(EntityDeathReason deathReason)
    {
        if (isDeathSequenceRunning)
        {
            return;
        }

        isDeathSequenceRunning = true;
        owner.DisableRuntime();
        OnDeathSequenceStarted?.Invoke();
        GameEventBus.Publish(new EntityDiedEvent(owner, transform.position, lastDamageSource, deathReason));
        StartCoroutine(RunDeathSequence());
    }

    private IEnumerator RunDeathSequence()
    {
        Delegate[] deathSequenceListeners = OnDeathSequenceRequested?.GetInvocationList();
        if (deathSequenceListeners != null)
        {
            for (int i = 0; i < deathSequenceListeners.Length; i++)
            {
                Func<IEnumerator> listener = deathSequenceListeners[i] as Func<IEnumerator>;
                if (listener == null)
                {
                    continue;
                }

                IEnumerator sequence = null;
                try
                {
                    sequence = listener.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }

                if (sequence != null)
                {
                    yield return sequence;
                }
            }
        }
        
        OnDeathSequenceCompleted?.Invoke();
        
        Destroy(gameObject);
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        switch (propType)
        {
            case PropType.MaxHealth:
                UpdateMaxHealth();
                break;
            case PropType.LifeSteal:
                lifeStealRatio = PropValueUtility.PercentPointsToNonNegativeRatio(newValue);
                break;
            case PropType.HealthRecoverySpeed:
                healthRecoveryPerSecond = ResolveHealthRecoveryPerSecond(newValue);
                break;
        }
    }

    private void UpdateAllProperties()
    {
        UpdateMaxHealth();
        lifeStealRatio = PropValueUtility.PercentPointsToNonNegativeRatio(
            propertiesManager.GetPropValue(PropType.LifeSteal));
        healthRecoveryPerSecond = ResolveHealthRecoveryPerSecond(propertiesManager.GetPropValue(PropType.HealthRecoverySpeed));
    }

    private static float ResolveHealthRecoveryPerSecond(float value)
    {
        return PropValueUtility.HealthRecoveryPointsToEffectiveHealthPerSecond(value);
    }

    private void UpdateMaxHealth()
    {
        SetMaxHealth(propertiesManager.GetPropValue(PropType.MaxHealth), true);
    }

    private void SetMaxHealth(float value, bool preserveRatio)
    {
        float oldMaxHealth = PropValueUtility.ClampEffectiveMaxHealth(maxHealth);
        float healthPercent = oldMaxHealth > 0f ? health / oldMaxHealth : 1f;

        maxHealth = PropValueUtility.ClampEffectiveMaxHealth(value);
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

    private static void ApplyLifeStealToSource(HitResult appliedResult)
    {
        if (appliedResult.Source == null || appliedResult.Source == appliedResult.Target)
        {
            return;
        }

        if (!appliedResult.Source.TryGetComponent(out HealthComponent sourceHealth))
        {
            return;
        }

        sourceHealth.ApplyLifeSteal(appliedResult.FinalDamage);
    }

    private void ApplyLifeSteal(float dealtDamage)
    {
        if (health >= maxHealth || lifeStealRatio <= 0f || dealtDamage <= 0f)
        {
            return;
        }

        float lifeStealValue = dealtDamage * lifeStealRatio * LIFE_STEAL_HEAL_RATE_PER_RATIO;
        Heal(Math.Min(lifeStealValue, maxHealth - health));
    }
}
