using System;
using UnityEngine;

/// <summary>
/// 武器运行时基类：
/// 1. 负责索敌；
/// 2. 负责根据属性直接维护当前运行时攻击参数；
/// 3. 在冷却完成后触发具体武器的攻击实现。
/// 子类只需要关心“如何攻击”，例如近战开命中窗口、远程发射投射物。
/// </summary>
public abstract class Weapon : Entity, ILifecycle
{
    private const int DEFAULT_WEAPON_LEVEL = 1;
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("Aim")]
    [Tooltip("平时自动转向目标的插值速度。")]
    [SerializeField]
    protected float aimLerp = 10f;

    [Tooltip("允许发起攻击前，武器当前朝向与目标朝向之间的最大夹角。超过这个角度时会先继续转向，再等待下一帧攻击。")]
    [SerializeField]
    private float attackStartAimToleranceDegrees = 8f;

    [Header("Runtime")]
    [Tooltip("武器攻击会命中的目标层。由武器持有器在初始化时设置；这里只作为运行时查询使用。")]
    [SerializeField]
    protected LayerMask targetLayerMask;

    public int Level { get; private set; } = DEFAULT_WEAPON_LEVEL;
    public float Damage { get; private set; }
    public float AttackInterval { get; private set; } = 1f;
    public float Range { get; private set; } = 0.1f;
    public float CriticalChance { get; private set; }
    public float CriticalMultiplier { get; private set; } = 1f;
    public float KnockbackForce { get; private set; }
    public bool IsAttacking { get; protected set; }
    protected PropertiesManager propertiesManager;
    protected Entity owner;
    protected Entity currentTarget;
    private float attackCooldownTimer;
    private Vector2 lastAimDirection = Vector2.up;
    private Vector2 lockedAttackDirection = Vector2.up;

    public Entity Owner => owner;
    public virtual int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public virtual void OnFixedTick(float deltaTime)
    {

    }

    public virtual void Initialize(Entity owner)
    {
        this.owner = owner;
        propertiesManager = GetComponentInParent<PropertiesManager>();
    }

    public virtual void OnEnableComponent()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }

        ApplyCurrentConfiguration();
        RefreshRuntimeStats();
    }

    public virtual void OnDisableComponent()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    public virtual void OnTick(float deltaTime)
    {
        TickTargeting(deltaTime);
        TickWeapon(deltaTime);
    }

    public void SetLevel(int targetLevel)
    {
        Level = Mathf.Max(DEFAULT_WEAPON_LEVEL, targetLevel);
        RefreshRuntimeStats();
    }

    public void SetWeaponData(WeaponDataSO weaponData)
    {
        WeaponData = weaponData ?? throw new ArgumentNullException(nameof(weaponData),
                $"{nameof(Weapon)} requires a non-null {nameof(WeaponDataSO)}.");
        ApplyCurrentConfiguration();
        RefreshRuntimeStats();
    }

    public void SetTargetLayerMask(LayerMask layerMask)
    {
        targetLayerMask = layerMask;
    }

    public LayerMask TargetLayerMask => targetLayerMask;

    protected virtual void OnConfiguredFromData()
    {
    }

    public virtual void RefreshRuntimeStats()
    {
        RecalculateRuntimeStats();
    }

    public void ApplyVisualForwardAngle()
    {
        if (EntityRenderer == null)
        {
            throw new MissingComponentException(
                $"{nameof(EntityRenderer)} is null on {name} when applying visual forward angle. " +
                $"Ensure {nameof(EntityRenderer)} is assigned in the inspector.");
        }

        Transform visualTransform = EntityRenderer.transform;
        Vector3 localEulerAngles = visualTransform.localEulerAngles;
        localEulerAngles.z = WeaponData.VisualForwardAngle;
        visualTransform.localEulerAngles = localEulerAngles;
    }

    protected Entity ResolveAttackSourceEntity()
    {
        return owner != null ? owner : this;
    }

    protected HitSpec BuildHitSpec()
    {
        return new HitSpec(Damage, CriticalChance, CriticalMultiplier, KnockbackForce);
    }

    protected Vector2 ResolveFallbackAttackDirection()
    {
        if (transform.up.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return ((Vector2)transform.up).normalized;
        }

        if (lastAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return lastAimDirection.normalized;
        }

        return Vector2.up;
    }

    protected Vector2 ResolveAttackDirection(Vector2 targetPosition, Transform origin = null)
    {
        Vector2 originPosition = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
        Vector2 targetDirection = targetPosition - originPosition;
        if (targetDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return targetDirection.normalized;
        }

        return ResolveFallbackAttackDirection();
    }

    protected Vector2 ResolveDesiredAimDirection(Entity target)
    {
        if (target != null)
        {
            return (target.Center - (Vector2)transform.position).normalized;
        }

        if (owner.MoveComponent.MoveDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return owner.MoveComponent.MoveDirection.normalized;
        }

        return lastAimDirection;
    }

    protected bool HasReachedAttackAimDirection(Vector2 desiredAimDirection)
    {
        if (desiredAimDirection.sqrMagnitude <= MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return true;
        }

        Vector2 currentAimDirection = transform.up;
        if (currentAimDirection.sqrMagnitude <= MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return true;
        }

        float angle = Vector2.Angle(currentAimDirection, desiredAimDirection.normalized);
        return angle <= attackStartAimToleranceDegrees;
    }

    protected void LockAttackDirection(Vector2 attackDirection)
    {
        if (attackDirection.sqrMagnitude <= MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            lockedAttackDirection = ResolveFallbackAttackDirection();
            return;
        }

        lockedAttackDirection = attackDirection.normalized;
    }

    protected Vector2 GetLockedAttackDirection()
    {
        if (lockedAttackDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return lockedAttackDirection;
        }

        return ResolveFallbackAttackDirection();
    }

    protected void CompleteAttackCycle()
    {
        IsAttacking = false;
    }

    public HitResult ApplyHit(HitRequest request)
    {
        return HitService.Apply(request);
    }

    protected float ResolveAttackSequenceDuration(AttackSequenceDefinitionSO sequence)
    {
        if (sequence == null)
        {
            return 0.01f;
        }

        float sequenceDuration = Mathf.Max(0.01f, sequence.Duration);
        float attackInterval = Mathf.Max(0.01f, AttackInterval);
        float occupancy = WeaponData.AttackSequenceOccupancy;
        float reservedWindow = Mathf.Max(0.01f, attackInterval * occupancy);
        return Mathf.Min(sequenceDuration, reservedWindow);
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

        if (attackCooldownTimer < AttackInterval)
        {
            return;
        }

        if (!HasReachedAttackAimDirection(ResolveDesiredAimDirection(currentTarget)))
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

    protected virtual void TickTargeting(float deltaTime)
    {
        Entity previousTarget = currentTarget;
        currentTarget = owner != null
            ? owner.FindClosestTargetInRange(Range, targetLayerMask)
            : null;

        Vector2 desiredAimDirection = ResolveDesiredAimDirection(currentTarget);
        bool holdCurrentAim = IsAttacking ||
                              (ShouldStopAimingWhenAttackReady() &&
                               currentTarget != null &&
                               attackCooldownTimer >= AttackInterval &&
                               HasReachedAttackAimDirection(desiredAimDirection));
        if (holdCurrentAim)
        {
            return;
        }

        Vector2 nextAimDirection = transform.up;
        if (desiredAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            nextAimDirection = desiredAimDirection.normalized;
            lastAimDirection = nextAimDirection;
        }
        else if (previousTarget != null && currentTarget == null)
        {
            nextAimDirection = lastAimDirection;
            lastAimDirection = nextAimDirection;
        }

        Vector3 targetAimDirection = nextAimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE
            ? (Vector3)nextAimDirection
            : transform.up;
        transform.up = Vector3.Lerp(transform.up, targetAimDirection, deltaTime * aimLerp);
    }

    protected virtual void RecalculateRuntimeStats()
    {
        if (propertiesManager == null)
        {
            throw new MissingComponentException(
                $"{nameof(propertiesManager)} is null on {name}. Cannot recalculate runtime stats. " +
                $"Ensure the weapon is a child of an entity with a {nameof(PropertiesManager)} component.");
        }

        WeaponLevelStatData weaponStats = WeaponData.GetLevelStats(Level);

        float weaponAttack = weaponStats.Attack;
        float weaponAttackSpeed = weaponStats.AttackSpeed;
        float weaponCriticalChance = weaponStats.CriticalChance;
        float weaponCriticalMultiplier = weaponStats.CriticalPercent;
        float weaponRange = weaponStats.Range;
        float weaponKnockbackForce = weaponStats.KnockbackForce;

        float playerCriticalChance = propertiesManager.GetPropValue(PropType.CriticalChance);
        float playerCriticalBonus = propertiesManager.GetPropValue(PropType.CriticalPercent);

        float finalAttackSpeed = Mathf.Max(
            propertiesManager.GetPropValueWithAdditionalBase(PropType.AttackSpeed, weaponAttackSpeed),
            0.01f);
        Damage = Mathf.Max(0f, propertiesManager.GetPropValueWithAdditionalBase(PropType.Attack, weaponAttack));
        AttackInterval = 1f / finalAttackSpeed;
        CriticalChance = Mathf.Clamp01(weaponCriticalChance + playerCriticalChance);
        CriticalMultiplier = Mathf.Max(1f, weaponCriticalMultiplier + playerCriticalBonus);
        Range = Mathf.Max(0.1f, propertiesManager.GetPropValueWithAdditionalBase(PropType.AttackRange, weaponRange));
        KnockbackForce = Mathf.Max(0f, propertiesManager.GetPropValueWithAdditionalBase(PropType.KnockbackForce, weaponKnockbackForce));
    }

    private void ApplyCurrentConfiguration()
    {
        if (WeaponData == null)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponData)} is null on {name}. Cannot apply weapon configuration. " +
                $"Ensure {nameof(WeaponData)} is assigned before the weapon starts.");
        }

        switch (WeaponData.ConstructionScheme)
        {
            case WeaponConstructionScheme.Default:
            default:
                ApplyDefaultConstructionScheme();
                break;
        }

        OnConfiguredFromData();
    }

    private void ApplyDefaultConstructionScheme()
    {
        ApplyDataIcon();
        ApplyVisualForwardAngle();
    }

    private void ApplyDataIcon()
    {
        if (EntityRenderer == null)
        {
            throw new MissingComponentException(
                $"{nameof(EntityRenderer)} is null on {name} when applying weapon icon. " +
                $"Ensure {nameof(EntityRenderer)} is assigned in the inspector.");
        }

        EntityRenderer.SetSprite(WeaponData.ItemIcon);
    }

    private bool ShouldStopAimingWhenAttackReady()
    {
        return WeaponData.StopAimingWhenAttackReady;
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.CriticalChance ||
            propType == PropType.CriticalPercent ||
            propType == PropType.AttackRange ||
            propType == PropType.KnockbackForce)
        {
            RefreshRuntimeStats();
        }
    }
}
