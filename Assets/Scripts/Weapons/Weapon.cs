using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 武器运行时基类：
/// 1. 负责索敌；
/// 2. 负责根据属性计算攻击间隔与伤害；
/// 3. 在冷却完成后触发具体武器的攻击实现。
/// 子类只需要关心“如何攻击”，例如近战开命中窗口、远程发射投射物。
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("Aim")]
    [Tooltip("平时自动转向目标的插值速度。")]
    [SerializeField] protected float aimLerp = 10f;

    [Header("Runtime")]
    [Tooltip("武器攻击会命中的目标层。由武器持有器/挂点在初始化时设置；这里仅作为运行时查询使用。")]
    [SerializeField] protected LayerMask targetLayerMask;

    public int Level { get; private set; }
    public WeaponRuntimeStats RuntimeStats { get; private set; }
    public bool IsAttacking { get; protected set; }

    protected PropertiesManager propertiesManager;
    protected Entity ownerEntity;
    protected Entity currentTarget;
    private float attackCooldownTimer;
    private Vector2 lastAimDirection = Vector2.up;

    protected virtual void Awake()
    {
        propertiesManager = GetComponentInParent<PropertiesManager>();
        ownerEntity = GetComponentInParent<Entity>();
    }

    protected virtual void OnEnable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    protected virtual void OnDisable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    protected virtual void Start()
    {
        RefreshRuntimeStats();
    }

    protected virtual void Update()
    {
        TickTargeting();
        TickWeapon(Time.deltaTime);
    }

    public void SetLevel(int targetLevel)
    {
        Level = Mathf.Max(1, targetLevel);
        RefreshRuntimeStats();
    }

    public void SetTargetLayerMask(LayerMask layerMask)
    {
        targetLayerMask = layerMask;
    }

    public virtual void RefreshRuntimeStats()
    {
        RuntimeStats = BuildRuntimeStats();
    }

    protected virtual void TickWeapon(float deltaTime)
    {
        attackCooldownTimer += deltaTime;

        if (currentTarget == null)
        {
            return;
        }

        if (!CanStartAttack())
        {
            return;
        }

        if (attackCooldownTimer < RuntimeStats.AttackInterval)
        {
            return;
        }

        attackCooldownTimer = 0f;
        BeginAttack(currentTarget);
    }

    protected virtual bool CanStartAttack()
    {
        return !IsAttacking;
    }

    protected abstract void BeginAttack(Entity target);

    protected void CompleteAttackCycle()
    {
        IsAttacking = false;
    }

    protected Entity GetCurrentTarget()
    {
        return currentTarget;
    }

    protected float ResolveAttackSequenceDuration(AttackSequenceDefinitionSO sequence)
    {
        if (sequence == null)
        {
            return 0.01f;
        }

        float sequenceDuration = Mathf.Max(0.01f, sequence.Duration);
        float attackInterval = Mathf.Max(0.01f, RuntimeStats.AttackInterval);
        float occupancy = WeaponData != null ? WeaponData.AttackSequenceOccupancy : 0.85f;
        float reservedWindow = Mathf.Max(0.01f, attackInterval * occupancy);

        // 只在序列长于本次攻击节奏窗口时压缩，短序列维持原时长，
        // 这样既能避免“3 秒动画拖垮 1 秒攻速”，也能保留原本短动作的利落感。
        return Mathf.Min(sequenceDuration, reservedWindow);
    }

    public float GetDebugAttackInterval()
    {
        return RuntimeStats.AttackInterval;
    }

    public float GetDebugSequenceWindowDuration()
    {
        float attackInterval = Mathf.Max(0.01f, RuntimeStats.AttackInterval);
        float occupancy = WeaponData != null ? WeaponData.AttackSequenceOccupancy : 0.85f;
        return attackInterval * occupancy;
    }

    public float GetDebugOriginalSequenceDuration()
    {
        AttackSequenceDefinitionSO sequence = GetEquippedAttackSequence();
        return sequence != null ? sequence.Duration : 0f;
    }

    public float GetDebugEffectiveSequenceDuration()
    {
        AttackSequenceDefinitionSO sequence = GetEquippedAttackSequence();
        return sequence != null ? ResolveAttackSequenceDuration(sequence) : 0f;
    }

    public float GetDebugSequenceCompressionRatio()
    {
        float original = GetDebugOriginalSequenceDuration();
        if (original <= 0.0001f)
        {
            return 1f;
        }

        return GetDebugEffectiveSequenceDuration() / original;
    }

    protected void DrawSharedWeaponDebugGizmos()
    {
        float range = Application.isPlaying ? RuntimeStats.Range : 0.5f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);

        AttackSequenceDefinitionSO sequence = GetEquippedAttackSequence();
        if (sequence == null)
        {
            return;
        }

        float effectiveDuration = Application.isPlaying ? GetDebugEffectiveSequenceDuration() : sequence.Duration;
        float radius = Mathf.Clamp(range * 0.35f, 0.35f, 0.9f);
        var events = sequence.EventKeyframes;
        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe keyframe = events[i];
            float angle = -90f + keyframe.normalizedTime * 360f;
            Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.up * radius;

            Gizmos.color = GetEventDebugColor(keyframe.eventType);
            Gizmos.DrawWireSphere(transform.position + offset, 0.06f);

            if (keyframe.eventType == WeaponSequenceEventType.SpawnProjectile)
            {
                Gizmos.DrawLine(transform.position, transform.position + offset);
            }
        }
    }

    private Color GetEventDebugColor(WeaponSequenceEventType eventType)
    {
        return eventType switch
        {
            WeaponSequenceEventType.OpenHitWindow => Color.green,
            WeaponSequenceEventType.CloseHitWindow => new Color(1f, 0.5f, 0f, 1f),
            WeaponSequenceEventType.SpawnProjectile => Color.cyan,
            WeaponSequenceEventType.PlaySfx => Color.yellow,
            WeaponSequenceEventType.PlayVfx => new Color(0.7f, 0.3f, 1f, 1f),
            _ => Color.white
        };
    }

    protected virtual AttackSequenceDefinitionSO GetEquippedAttackSequence()
    {
        return null;
    }

    protected virtual void TickTargeting()
    {
        Entity previousTarget = currentTarget;
        currentTarget = ownerEntity != null
            ? ownerEntity.FindClosestTargetInRange(RuntimeStats.Range, targetLayerMask)
            : null;

        bool stopAimingWhenAttackReady = WeaponData == null || WeaponData.StopAimingWhenAttackReady;
        bool holdCurrentAim = IsAttacking || (stopAimingWhenAttackReady && currentTarget != null && attackCooldownTimer >= RuntimeStats.AttackInterval);
        if (holdCurrentAim)
        {
            return;
        }

        Vector2 desiredAimDirection = ResolveDesiredAimDirection();
        if (desiredAimDirection.sqrMagnitude > 0.0001f)
        {
            lastAimDirection = desiredAimDirection.normalized;
            transform.up = Vector3.Lerp(transform.up, lastAimDirection, Time.deltaTime * aimLerp);
        }
        else if (previousTarget != null && currentTarget == null)
        {
            transform.up = Vector3.Lerp(transform.up, lastAimDirection, Time.deltaTime * aimLerp);
        }
    }

    protected Vector2 ResolveDesiredAimDirection()
    {
        if (currentTarget != null)
        {
            return (currentTarget.Center - (Vector2)transform.position).normalized;
        }

        if (ownerEntity != null)
        {
            if (ownerEntity.IsMoving && ownerEntity.CurrentFacingDirection.sqrMagnitude > 0.0001f)
            {
                return ownerEntity.CurrentFacingDirection.normalized;
            }

            if (ownerEntity.CurrentFacingDirection.sqrMagnitude > 0.0001f)
            {
                return ownerEntity.CurrentFacingDirection.normalized;
            }
        }

        return lastAimDirection;
    }

    protected ResolvedWeaponHit ResolveHit()
    {
        bool isCritical = Random.value <= RuntimeStats.CriticalChance;
        float damage = isCritical ? RuntimeStats.Damage * RuntimeStats.CriticalMultiplier : RuntimeStats.Damage;
        return new ResolvedWeaponHit(damage, isCritical);
    }

    protected WeaponAttackContext BuildAttackContext(Entity target, Transform origin = null)
    {
        Transform sourceTransform = origin != null ? origin : transform;
        Vector2 aimDirection = target != null
            ? (target.Center - (Vector2)sourceTransform.position).normalized
            : (Vector2)transform.up;

        return new WeaponAttackContext(this, sourceTransform, target, aimDirection, RuntimeStats, ResolveHit());
    }

    protected virtual WeaponRuntimeStats BuildRuntimeStats()
    {
        var calculatedProps = WeaponPropsCalculator.GetProps(WeaponData, Level);

        float weaponAttack = calculatedProps[PropType.Attack];
        float weaponAttackSpeed = Mathf.Max(calculatedProps[PropType.AttackSpeed], 0.01f);
        float weaponCriticalChance = Mathf.Clamp01(calculatedProps[PropType.CriticalChance]);
        float weaponCriticalMultiplier = Mathf.Max(1f, calculatedProps[PropType.CriticalPercent]);
        float weaponRange = calculatedProps[PropType.Range];

        float playerAttack = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Attack) : 0f;
        float playerAttackSpeedMultiplier = propertiesManager != null ? Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f) : 1f;
        float playerCriticalChance = propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalChance) : 0f;
        float playerCriticalBonus = propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalPercent) : 0f;
        float playerRange = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Range) : 0f;

        float damage = weaponAttack + playerAttack;
        float finalAttackSpeed = Mathf.Max(weaponAttackSpeed * playerAttackSpeedMultiplier, 0.01f);
        float attackInterval = 1f / finalAttackSpeed;
        float criticalChance = Mathf.Clamp01(weaponCriticalChance + playerCriticalChance);
        float criticalMultiplier = Mathf.Max(1f, weaponCriticalMultiplier + playerCriticalBonus);
        float range = Mathf.Max(0.1f, weaponRange + playerRange);

        return new WeaponRuntimeStats(damage, attackInterval, range, criticalChance, criticalMultiplier);
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.CriticalChance ||
            propType == PropType.CriticalPercent ||
            propType == PropType.Range)
        {
            RefreshRuntimeStats();
        }
    }
}
