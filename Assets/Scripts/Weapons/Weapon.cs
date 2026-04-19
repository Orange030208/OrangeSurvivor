using System;
using UnityEngine;

/// <summary>
/// 武器运行时基类：
/// 1. 负责索敌；
/// 2. 负责根据属性计算攻击间隔与伤害；
/// 3. 在冷却完成后触发具体武器的攻击实现。
/// 子类只需要关心“如何攻击”，例如近战开命中窗口、远程发射投射物。
/// </summary>
public abstract class Weapon : Entity
{
    private const int DEFAULT_WEAPON_LEVEL = 1;
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private static readonly WeaponRuntimeStats DefaultRuntimeStats = new(0f, 1f, 0.1f, 0f, 1f);

    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("Components")]
    [SerializeField] private EntityRenderer entityRenderer;

    [Header("Aim")]
    [Tooltip("平时自动转向目标的插值速度。")]
    [SerializeField] protected float aimLerp = 10f;
    [Tooltip("允许发起攻击前，武器当前朝向与目标朝向之间的最大夹角。超过这个角度时会先继续转向，再等待下一帧攻击。")]
    [SerializeField] private float attackStartAimToleranceDegrees = 8f;

    [Header("Runtime")]
    [Tooltip("武器攻击会命中的目标层。由武器持有器/挂点在初始化时设置；这里仅作为运行时查询使用。")]
    [SerializeField] protected LayerMask targetLayerMask;

    public int Level { get; private set; } = DEFAULT_WEAPON_LEVEL;
    public WeaponRuntimeStats RuntimeStats { get; private set; } = DefaultRuntimeStats;
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
            throw new ArgumentNullException(nameof(weaponData), $"{nameof(Weapon)} requires a non-null {nameof(WeaponDataSO)}.");
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
        RuntimeStats = BuildRuntimeStats();
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

        if (!HasReachedAttackAimDirection())
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
        HitResult result = HitService.Apply(request);
        return result;
    }

    protected virtual AudioSfxKey ResolveHitSfxKey(WeaponHitEvent hitEvent)
    {
        return WeaponData.HitSfxKey;
    }

    private void PlayHitSfx(WeaponHitEvent hitEvent)
    {
        AudioSfxKey sfxKey = ResolveHitSfxKey(hitEvent);
        if (sfxKey != AudioSfxKey.None)
        {
            AudioSfxBridge.RequestPlay(sfxKey);
        }
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

    protected virtual void TickTargeting(float deltaTime)
    {
        WeaponTargetingSnapshot targetingSnapshot = BuildTargetingSnapshot();
        Entity previousTarget = currentTarget;
        currentTarget = WeaponTargetingLogic.ResolveTarget(targetingSnapshot);

        WeaponAimUpdate aimUpdate = WeaponTargetingLogic.BuildAimUpdate(targetingSnapshot, currentTarget);
        if (!aimUpdate.ShouldApplyAim)
        {
            return;
        }

        lastAimDirection = aimUpdate.LastAimDirection;
        Vector3 targetAimDirection = aimUpdate.AimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE
            ? (Vector3)aimUpdate.AimDirection
            : (previousTarget != null && currentTarget == null ? (Vector3)lastAimDirection : transform.up);
        transform.up = Vector3.Lerp(transform.up, targetAimDirection, deltaTime * aimLerp);
    }

    private WeaponTargetingSnapshot BuildTargetingSnapshot()
    {
        bool stopAimingWhenAttackReady = WeaponData == null || WeaponData.StopAimingWhenAttackReady;
        return new WeaponTargetingSnapshot(
            ownerEntity,
            currentTarget,
            RuntimeStats.Range,
            targetLayerMask,
            transform.position,
            transform.up,
            lastAimDirection,
            attackCooldownTimer,
            RuntimeStats.AttackInterval,
            IsAttacking,
            stopAimingWhenAttackReady,
            attackStartAimToleranceDegrees);
    }

    protected bool HasReachedAttackAimDirection()
    {
        WeaponTargetingSnapshot targetingSnapshot = BuildTargetingSnapshot();
        Vector2 desiredAimDirection = WeaponTargetingLogic.ResolveDesiredAimDirection(targetingSnapshot, currentTarget);
        return WeaponTargetingLogic.HasReachedAttackAimDirection(targetingSnapshot, desiredAimDirection);
    }

    protected Vector2 ResolveAttackDirection(Entity target, Transform origin = null)
    {
        Vector2 originPosition = origin != null ? (Vector2)origin.position : (Vector2)transform.position;
        return WeaponTargetingLogic.ResolveAttackDirection(BuildTargetingSnapshot(), target, originPosition);
    }

    private Vector2 ResolveFallbackAttackDirection()
    {
        return WeaponTargetingLogic.ResolveFallbackAttackDirection(BuildTargetingSnapshot());
    }

    protected HitSpec BuildHitSpec()
    {
        return new HitSpec(RuntimeStats.Damage, RuntimeStats.CriticalChance, RuntimeStats.CriticalMultiplier);
    }

    protected WeaponAttackContext BuildAttackContext(Entity target, Transform origin = null)
    {
        return BuildAttackContext(target, GetLockedAttackDirection(), origin);
    }

    protected WeaponAttackContext BuildAttackContext(Entity target, Vector2 aimDirection, Transform origin = null)
    {
        Transform sourceTransform = origin != null ? origin : transform;
        Vector2 resolvedAimDirection = aimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE
            ? aimDirection.normalized
            : ResolveFallbackAttackDirection();

        Entity sourceEntity = ownerEntity != null ? ownerEntity : this;

        return new WeaponAttackContext(this, sourceEntity, sourceTransform, target, resolvedAimDirection, RuntimeStats, BuildHitSpec());
    }

    protected virtual WeaponRuntimeStats BuildRuntimeStats()
    {
        if (WeaponData == null)
        {
            return DefaultRuntimeStats;
        }

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
