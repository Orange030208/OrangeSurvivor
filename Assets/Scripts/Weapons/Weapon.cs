using System;
using UnityEngine;

/// <summary>
/// 武器运行时基类：
/// 1. 负责索敌；
/// 2. 负责根据属性直接维护当前运行时攻击参数；
/// 3. 在冷却完成后触发具体武器的攻击实现。
/// 子类只需要关心“如何攻击”，例如近战开命中窗口、远程发射投射物。
/// </summary>
public abstract class Weapon : Entity
{
    private const int DEFAULT_WEAPON_LEVEL = 1;
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("Components")] [SerializeField]
    private EntityRenderer entityRenderer;

    [Header("Aim")] [Tooltip("平时自动转向目标的插值速度。")] [SerializeField]
    protected float aimLerp = 10f;

    [Tooltip("允许发起攻击前，武器当前朝向与目标朝向之间的最大夹角。超过这个角度时会先继续转向，再等待下一帧攻击。")] [SerializeField]
    private float attackStartAimToleranceDegrees = 8f;

    [Header("Runtime")] [Tooltip("武器攻击会命中的目标层。由武器持有器在初始化时设置；这里只作为运行时查询使用。")] [SerializeField]
    protected LayerMask targetLayerMask;

    public int Level { get; private set; } = DEFAULT_WEAPON_LEVEL;
    public float Damage { get; private set; }
    public float AttackInterval { get; private set; } = 1f;
    public float Range { get; private set; } = 0.1f;
    public float CriticalChance { get; private set; }
    public float CriticalMultiplier { get; private set; } = 1f;
    public bool IsAttacking { get; protected set; }
    public Entity OwnerEntity => ownerEntity;
    public override EntityRenderer EntityRenderer => entityRenderer;

    protected PropertiesManager propertiesManager;
    protected Entity ownerEntity;
    protected Entity currentTarget;
    private float attackCooldownTimer;
    private Vector2 lastAimDirection = Vector2.up;
    private Vector2 lockedAttackDirection = Vector2.up;

    protected virtual void Awake()
    {
        propertiesManager = GetComponentInParent<PropertiesManager>();
        ownerEntity = ResolveOwnerEntity();

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }
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
        ApplyCurrentConfiguration();
        RefreshRuntimeStats();
    }

    protected virtual void Update()
    {
        if (!GameSimulation.IsRunning)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
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
        if (weaponData == null)
        {
            throw new ArgumentNullException(nameof(weaponData),
                $"{nameof(Weapon)} requires a non-null {nameof(WeaponDataSO)}.");
        }

        WeaponData = weaponData;
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
            return;
        }

        Transform visualTransform = EntityRenderer.transform;
        Vector3 localEulerAngles = visualTransform.localEulerAngles;
        localEulerAngles.z = WeaponData != null ? WeaponData.VisualForwardAngle : 0f;
        visualTransform.localEulerAngles = localEulerAngles;
    }

    protected Entity GetCurrentTarget()
    {
        return currentTarget;
    }

    protected Entity ResolveAttackSourceEntity()
    {
        return ownerEntity != null ? ownerEntity : this;
    }

    protected HitSpec BuildHitSpec()
    {
        return new HitSpec(Damage, CriticalChance, CriticalMultiplier);
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

        if (ownerEntity.MoveComponent.MoveDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return ownerEntity.MoveComponent.MoveDirection.normalized;
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
        float occupancy = WeaponData != null ? WeaponData.AttackSequenceOccupancy : 0.85f;
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
        currentTarget = ownerEntity != null
            ? ownerEntity.FindClosestTargetInRange(Range, targetLayerMask)
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
        if (WeaponData == null)
        {
            Damage = 0f;
            AttackInterval = 1f;
            Range = 0.1f;
            CriticalChance = 0f;
            CriticalMultiplier = 1f;
            return;
        }

        var calculatedProps = WeaponData.GetPropsByLevel(Level);

        float weaponAttack = calculatedProps[PropType.Attack];
        float weaponAttackSpeed = Mathf.Max(calculatedProps[PropType.AttackSpeed], 0.01f);
        float weaponCriticalChance = Mathf.Clamp01(calculatedProps[PropType.CriticalChance]);
        float weaponCriticalMultiplier = Mathf.Max(1f, calculatedProps[PropType.CriticalPercent]);
        float weaponRange = calculatedProps[PropType.Range];

        float playerAttack = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Attack) : 0f;
        float playerAttackSpeedMultiplier = propertiesManager != null
            ? Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f)
            : 1f;
        float playerCriticalChance =
            propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalChance) : 0f;
        float playerCriticalBonus =
            propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalPercent) : 0f;
        float playerRange = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Range) : 0f;

        float finalAttackSpeed = Mathf.Max(weaponAttackSpeed * playerAttackSpeedMultiplier, 0.01f);
        Damage = weaponAttack + playerAttack;
        AttackInterval = 1f / finalAttackSpeed;
        CriticalChance = Mathf.Clamp01(weaponCriticalChance + playerCriticalChance);
        CriticalMultiplier = Mathf.Max(1f, weaponCriticalMultiplier + playerCriticalBonus);
        Range = Mathf.Max(0.1f, weaponRange + playerRange);
    }

    private void ApplyCurrentConfiguration()
    {
        if (WeaponData == null)
        {
            return;
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
        if (EntityRenderer == null || WeaponData == null)
        {
            return;
        }

        EntityRenderer.SetSprite(WeaponData.ItemIcon);
    }

    private bool ShouldStopAimingWhenAttackReady()
    {
        return WeaponData == null || WeaponData.StopAimingWhenAttackReady;
    }

    private Entity ResolveOwnerEntity()
    {
        Entity[] entities = GetComponentsInParent<Entity>(true);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity != null && entity != this)
            {
                return entity;
            }
        }

        return null;
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