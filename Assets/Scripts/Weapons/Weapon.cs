using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 统一武器运行时：
/// 1. 负责索敌、冷却、朝向和攻击序列播放；
/// 2. 根据序列事件执行近战命中窗口、投射物发射、音效和特效；
/// 3. 通过 WeaponDataSO 的 Spawn Points 统一描述子弹和表现生成点。
/// </summary>
[RequireComponent(typeof(WeaponSequenceBridge))]
public class Weapon : Entity, ILifecycle, IProjectileLauncher, IWaveEndStep, IDamageDealtNotifier
{
    private const int DEFAULT_WEAPON_LEVEL = 1;
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const string HOLDER_LEVEL_MODIFIER_SOURCE_PREFIX = "WEAPON_LEVEL_";
    private static readonly Color HIT_BOX_IDLE_GIZMO_COLOR = new(1f, 0.15f, 0.15f, 0.85f);
    private static readonly Color HIT_BOX_ACTIVE_GIZMO_COLOR = new(1f, 0.65f, 0f, 0.95f);
    private static readonly Color HIT_BOX_SWEEP_GIZMO_COLOR = new(1f, 0.35f, 0f, 0.45f);

    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("序列")]
    [Tooltip("负责驱动武器动作序列并把关键帧事件转发回本类。")]
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    [Header("视觉表现")]
    [Tooltip("接收武器表现朝向角的表现节点。请拖入只负责显示的子变换节点，避免影响武器根节点、发射点和碰撞逻辑。")]
    [SerializeField] private Transform visualForwardTransform;

    [SerializeField] private Transform animationTransform;

    [Header("命中盒")]
    [Tooltip("近战命中盒的独立锚点。为空时退回动画变换节点，再退回武器根节点。不要使用武器生成点位，它们只用于弹射物/特效生成。")]
    [SerializeField] private Transform hitBoxAnchorTransform;

    [Tooltip("在场景视图绘制命中盒实时位置、激活窗口和采样扫掠轨迹。仅用于调试，不影响实际判定。")]
    [SerializeField] private bool drawHitBoxDebugGizmos = true;

    [Tooltip("开启后，在攻击序列触发 PlaySfx 事件时输出事件时间、音效键和近战命中窗口状态，便于校准近战音效时机。")]
    [SerializeField] private bool logSequenceSfxDebug;

    [Header("瞄准")]
    [Tooltip("平时自动转向目标的插值速度。")]
    [SerializeField] protected float aimLerp = 10f;

    [Tooltip("允许发起攻击前，武器当前朝向与目标朝向之间的最大夹角。超过这个角度时会先继续转向，再等待下一帧攻击。")]
    [SerializeField] private float attackStartAimToleranceDegrees = 8f;

    [Header("运行时")]
    [Tooltip("武器攻击会命中的目标层。由武器持有器在初始化时设置；这里只作为运行时查询使用。")]
    [SerializeField] protected LayerMask targetLayerMask;
    private WeaponBenefitData runtimeBenefits = WeaponBenefitData.Zero;

    private readonly Dictionary<int, HashSet<HealthComponent>> hitWindowTargets = new();
    private readonly Dictionary<int, HitBoxDetectionPose> hitWindowLastPoses = new();
    private readonly HashSet<int> activeHitWindows = new();
    private readonly List<HitBoxDebugSample> hitBoxDebugSamples = new();
    private readonly WeaponTargetSelector targetSelector = new();
    private readonly WeaponStatsResolver statsResolver = new();
    private readonly ProjectilePatternEmitter projectilePatternEmitter = new();
    private HitBoxAttackExecutor hitBoxAttackExecutor;
    private AttackSequenceDefinitionSO attackSequence;
    private Vector2 pendingTargetPosition;
    private Entity lockedAttackTarget;
    private float currentAttackStartedAt;
    private float currentAttackSequenceDuration;

    public int Level { get; private set; } = DEFAULT_WEAPON_LEVEL;
    public float Damage { get; private set; }
    public float AttackInterval { get; private set; } = 1f;
    public float Range { get; private set; } = 0.1f;
    public float CriticalChance { get; private set; }
    public float CriticalMultiplier { get; private set; } = 1f;
    public float KnockbackStrength { get; private set; }
    public bool IsAttacking { get; protected set; }
    public WeaponBenefitData Benefits => runtimeBenefits.Validated();
    protected PropertiesManager propertiesManager;
    protected Entity owner;
    protected Entity currentTarget;
    private string activeHolderLevelModifierSourceId;
    private float cooldownRemaining = 1f;
    private int cooldownStartedFrame = -1;
    private Vector2 lastAimDirection = Vector2.up;
    private Vector2 lockedAttackDirection = Vector2.up;

    public Entity Owner => owner;
    public virtual int Priority => EntityComponentBase.PriorityPreset.RelyOthers;
    public Transform VisualForwardTransform => visualForwardTransform;
    public Transform AnimationTransform => animationTransform;
    public Transform HitBoxAnchorTransform => hitBoxAnchorTransform;
    public AttackSequenceDefinitionSO DebugAttackSequence => attackSequence != null ? attackSequence : WeaponData != null ? WeaponData.AttackSequence : null;
    public float DebugCooldownRemaining => cooldownRemaining;
    public int WaveEndPriority => WaveEndPriorities.StopCombat;
    private Vector2 HitBoxSize => WeaponData != null ? WeaponData.HitBoxSize : Vector2.one;

    public LayerMask TargetLayerMask => targetLayerMask;
    public Entity SourceEntity => ResolveAttackSourceEntity();
    public event Action<HitResult> DamageDealt;


    public virtual void OnFixedTick(float deltaTime)
    {
    }

    public virtual void Initialize(Entity owner)
    {
        this.owner = owner;
        propertiesManager = GetComponentInParent<PropertiesManager>();
        sequenceBridge = GetComponent<WeaponSequenceBridge>();
        hitBoxAttackExecutor = new HitBoxAttackExecutor(SpawnHitVfx);
    }

    public virtual void OnEnableComponent()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
            propertiesManager.OnAllPropertiesChanged += RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }

        SubscribeSequenceEvents();
        ApplyCurrentConfiguration();
        RefreshRuntimeStats();
        cooldownRemaining = AttackInterval;
    }

    public virtual void OnDisableComponent()
    {
        RemoveLevelHolderModifiers();
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }

        UnsubscribeSequenceEvents();
        ForceResetAttackState();
    }

    public void StopForWaveCleanup()
    {
        ForceResetAttackState();
    }

    public UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopForWaveCleanup();
        return UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        RemoveLevelHolderModifiers();
        UnsubscribeSequenceEvents();
    }

    public virtual void OnTick(float deltaTime)
    {
        TickTargeting(deltaTime);
        TickWeapon(deltaTime);
    }

    public void SetLevel(int targetLevel, bool playSfx = false)
    {
        int previousLevel = Level;
        Level = Mathf.Max(DEFAULT_WEAPON_LEVEL, targetLevel);
        ApplyLevelHolderModifiers();
        RefreshRuntimeStats();
        cooldownRemaining = Mathf.Min(cooldownRemaining, AttackInterval);
        if (playSfx && Level > previousLevel)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WeaponLevelUp);
        }
    }

    public void SetWeaponData(WeaponDataSO weaponData)
    {
        WeaponData = weaponData ?? throw new ArgumentNullException(nameof(weaponData),
            $"{nameof(Weapon)} requires a non-null {nameof(WeaponDataSO)}.");
        ApplyCurrentConfiguration();
        ApplyLevelHolderModifiers();
        RefreshRuntimeStats();
    }

    public void SetTargetLayerMask(LayerMask layerMask)
    {
        targetLayerMask = layerMask;
    }

    public void SetBenefits(WeaponBenefitData value)
    {
        runtimeBenefits = value.Validated();
        if (WeaponData != null && propertiesManager != null)
        {
            RefreshRuntimeStats();
        }
    }

    protected virtual void OnConfiguredFromData()
    {
        attackSequence = WeaponData.AttackSequence;
        if (attackSequence == null)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponDataSO)} '{WeaponData.name}' requires a configured {nameof(WeaponDataSO.AttackSequence)}.");
        }
    }

    public virtual void RefreshRuntimeStats()
    {
        RecalculateRuntimeStats();
    }

    public void ApplyVisualForwardAngle()
    {
        if (visualForwardTransform == null)
        {
            throw new MissingReferenceException(
                $"{nameof(Weapon)} '{name}' requires {nameof(visualForwardTransform)} to apply " +
                $"{nameof(WeaponDataSO.VisualForwardAngle)}. Assign the visual-only child transform in the inspector.");
        }

        Vector3 localEulerAngles = visualForwardTransform.localEulerAngles;
        localEulerAngles.z = WeaponData.VisualForwardAngle;
        visualForwardTransform.localEulerAngles = localEulerAngles;
    }

    protected Entity ResolveAttackSourceEntity()
    {
        return owner != null ? owner : this;
    }

    private Entity GetCurrentTarget()
    {
        return lockedAttackTarget != null ? lockedAttackTarget : currentTarget;
    }

    protected HitSpec BuildHitSpec()
    {
        return new HitSpec(Damage, CriticalChance, CriticalMultiplier, KnockbackStrength);
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
        return ResolveAttackDirection(targetPosition, originPosition);
    }

    protected Vector2 ResolveAttackDirection(Vector2 targetPosition, Vector3 originPosition)
    {
        Vector2 targetDirection = targetPosition - (Vector2)originPosition;
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
            Vector2 originPosition = transform.position;
            return (ResolveTargetAimPoint(target, originPosition) - originPosition).normalized;
        }

        if (owner != null && owner.MoveComponent.MoveDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return owner.MoveComponent.MoveDirection.normalized;
        }

        return lastAimDirection;
    }

    private Vector2 ResolveTargetAimPoint(Entity target, Vector2 originPosition)
    {
        if (target == null)
        {
            return originPosition;
        }

        Vector2 closestPoint = target.GetClosestPointTo(originPosition);
        if ((closestPoint - originPosition).sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            return closestPoint;
        }

        return target.Center;
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

    public void NotifyDamageDealt(HitResult result)
    {
        DamageDealt?.Invoke(result);
    }

    protected float ResolveAttackSequenceDuration(AttackSequenceDefinitionSO sequence)
    {
        if (sequence == null)
        {
            return 0.01f;
        }

        float sequenceDuration = Mathf.Max(0.01f, sequence.Duration);
        if (WeaponData != null && WeaponData.AttackTimingMode == WeaponAttackTimingMode.FixedSequenceThenCooldown)
        {
            return sequenceDuration;
        }

        float attackInterval = Mathf.Max(0.01f, AttackInterval);
        float occupancy = WeaponData != null ? WeaponData.AttackSequenceOccupancy : 0.85f;
        float reservedWindow = Mathf.Max(0.01f, attackInterval * occupancy);
        return Mathf.Min(sequenceDuration, reservedWindow);
    }

    protected virtual void TickWeapon(float deltaTime)
    {
        TickActiveHitWindows();
        TickCooldown(deltaTime);

        if (currentTarget == null)
        {
            return;
        }

        if (!CanStartAttack())
        {
            return;
        }

        if (cooldownRemaining > 0f)
        {
            return;
        }

        if (!HasReachedAttackAimDirection(ResolveDesiredAimDirection(currentTarget)))
        {
            return;
        }

        BeginAttack(currentTarget);
    }

    private void TickCooldown(float deltaTime)
    {
        if (cooldownRemaining <= 0f)
        {
            return;
        }

        if (WeaponData != null &&
            WeaponData.AttackTimingMode == WeaponAttackTimingMode.FixedSequenceThenCooldown &&
            cooldownStartedFrame == Time.frameCount)
        {
            return;
        }

        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
    }

    protected virtual bool CanStartAttack()
    {
        return !IsAttacking && (sequenceBridge == null || !sequenceBridge.IsPlaying);
    }

    protected virtual void BeginAttack(Entity target)
    {
        IsAttacking = true;
        lockedAttackTarget = target;
        Vector2 originPosition = transform.position;
        Vector2 actualTargetPosition = ResolveTargetAimPoint(target, originPosition);
        LockAttackDirection(ResolveAttackDirection(actualTargetPosition));
        pendingTargetPosition = ResolveSequenceTargetPosition(originPosition, actualTargetPosition);
        projectilePatternEmitter.ResetBurstState();
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        hitBoxDebugSamples.Clear();

        float sequenceDuration = ResolveAttackSequenceDuration(attackSequence);
        currentAttackStartedAt = Time.time;
        currentAttackSequenceDuration = sequenceDuration;
        Vector2 targetLocalOffset = transform.InverseTransformPoint(pendingTargetPosition);
        if (WeaponData == null || WeaponData.AttackTimingMode == WeaponAttackTimingMode.CompressedIntoAttackInterval)
        {
            cooldownRemaining = AttackInterval;
        }

        sequenceBridge.Play(attackSequence, targetLocalOffset, sequenceDuration);
    }

    private Vector2 ResolveSequenceTargetPosition(Vector2 originPosition, Vector2 actualTargetPosition)
    {
        if (attackSequence == null)
        {
            return actualTargetPosition;
        }

        switch (attackSequence.TargetOffsetMode)
        {
            case WeaponSequenceTargetOffsetMode.MaxRangeAlongAimDirection:
                return originPosition + GetLockedAttackDirection() * Range;
            default:
                return actualTargetPosition;
        }
    }

    protected virtual void TickTargeting(float deltaTime)
    {
        if (IsAttacking)
        {
            return;
        }

        Entity previousTarget = currentTarget;
        currentTarget = ResolveCurrentTarget();

        Vector2 desiredAimDirection = ResolveDesiredAimDirection(currentTarget);
        bool holdCurrentAim = IsAttacking ||
                              (ShouldHoldAimWhenAttackReady() &&
                               currentTarget != null &&
                               cooldownRemaining <= 0f &&
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

    private Entity ResolveCurrentTarget()
    {
        if (WeaponData != null && WeaponData.TargetingMode == WeaponTargetingMode.StableLock)
        {
            return targetSelector.SelectTarget(
                currentTarget,
                transform.position,
                transform.up,
                Range,
                targetLayerMask,
                WeaponTargetingMode.StableLock);
        }

        // 远程武器使用动态最近目标逻辑，保持既有手感。
        return this.FindClosestTargetInRange(Range, targetLayerMask);
    }

    private void SubscribeSequenceEvents()
    {
        if (sequenceBridge == null)
        {
            sequenceBridge = GetComponent<WeaponSequenceBridge>();
        }

        if (sequenceBridge == null)
        {
            return;
        }

        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
        sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted += FinishAttackSequence;
    }

    private void UnsubscribeSequenceEvents()
    {
        if (sequenceBridge == null)
        {
            return;
        }

        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventType eventType, int eventKey)
    {
        switch (eventType)
        {
            case WeaponSequenceEventType.OpenHitWindow:
                OpenHitWindow(eventKey);
                break;
            case WeaponSequenceEventType.CloseHitWindow:
                CloseHitWindow(eventKey);
                break;
            case WeaponSequenceEventType.SpawnProjectile:
                FireProjectiles(eventKey);
                break;
            case WeaponSequenceEventType.PlaySfx:
                PlaySequenceSfx(eventKey);
                break;
            case WeaponSequenceEventType.PlayVfx:
                PlaySequenceVfx(eventKey);
                break;
        }
    }

    private void OpenHitWindow(int eventKey)
    {
        if (WeaponData == null || !WeaponData.EnableHitBox)
        {
            return;
        }

        activeHitWindows.Add(eventKey);
        if (!hitWindowTargets.TryGetValue(eventKey, out HashSet<HealthComponent> hitTargets))
        {
            hitTargets = new HashSet<HealthComponent>();
            hitWindowTargets[eventKey] = hitTargets;
        }
        else
        {
            hitTargets.Clear();
        }

        hitWindowLastPoses[eventKey] = CaptureCurrentHitPose();
    }

    private void CloseHitWindow(int eventKey)
    {
        activeHitWindows.Remove(eventKey);
        hitWindowLastPoses.Remove(eventKey);
    }

    private void TickActiveHitWindows()
    {
        if (WeaponData == null || !WeaponData.EnableHitBox || activeHitWindows.Count == 0)
        {
            return;
        }

        foreach (int windowId in activeHitWindows)
        {
            if (!hitWindowTargets.TryGetValue(windowId, out HashSet<HealthComponent> hitTargets))
            {
                continue;
            }

            HitBoxDetectionPose currentPose = CaptureCurrentHitPose();
            if (!hitWindowLastPoses.TryGetValue(windowId, out HitBoxDetectionPose previousPose))
            {
                previousPose = currentPose;
            }

            hitBoxAttackExecutor.ExecuteAttack(
                this,
                ResolveAttackSourceEntity(),
                BuildHitSpec(),
                HitBoxSize,
                hitTargets,
                targetLayerMask,
                previousPose,
                currentPose,
                RecordHitBoxDebugSample);

            hitWindowLastPoses[windowId] = currentPose;
        }
    }

    private HitBoxDetectionPose CaptureCurrentHitPose()
    {
        WeaponSpawnPointPose anchorPose = ResolveHitBoxAnchorPose();
        Vector2 hitOffset = WeaponData != null ? WeaponData.HitBoxOffset : Vector2.zero;
        Vector3 localOffset = new(hitOffset.x, hitOffset.y, 0f);
        Vector3 offsetPosition = anchorPose.Position + anchorPose.Rotation * localOffset;
        return new HitBoxDetectionPose(offsetPosition, anchorPose.Rotation.eulerAngles.z);
    }

    private WeaponSpawnPointPose ResolveHitBoxAnchorPose()
    {
        Transform anchor = hitBoxAnchorTransform != null
            ? hitBoxAnchorTransform
            : animationTransform != null
                ? animationTransform
                : transform;
        return new WeaponSpawnPointPose(anchor.position, anchor.rotation);
    }

    private void RecordHitBoxDebugSample(HitBoxDetectionPose pose)
    {
        if (!drawHitBoxDebugGizmos)
        {
            return;
        }

        hitBoxDebugSamples.Add(new HitBoxDebugSample(pose, HitBoxSize, Time.time));
        const int maxDebugSamples = 48;
        if (hitBoxDebugSamples.Count > maxDebugSamples)
        {
            hitBoxDebugSamples.RemoveRange(0, hitBoxDebugSamples.Count - maxDebugSamples);
        }
    }

    private void FireProjectiles(int eventKey)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceProjectile(eventKey, out WeaponSequenceProjectileDefinition projectileConfig))
        {
            return;
        }

        FireProjectiles(projectileConfig);
    }

    private void FireProjectiles(WeaponSequenceProjectileDefinition projectileConfig)
    {
        projectilePatternEmitter.Emit(projectileConfig, CreateProjectileEmissionContext());
    }

    private ProjectilePatternEmissionContext CreateProjectileEmissionContext()
    {
        return new ProjectilePatternEmissionContext(
            this,
            ResolveAttackSourceEntity,
            BuildHitSpec,
            ResolveSpawnPointPose,
            origin => ResolveAttackDirection(pendingTargetPosition, origin.Position),
            ResolveProjectilePierceCount,
            () => TargetLayerMask,
            () => Range,
            LaunchProjectile,
            StartCoroutine);
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile), $"{nameof(Weapon)} requires a valid {nameof(IProjectile)} instance.");
        }

        // 武器发射音效由攻击序列 PlaySfx 事件控制，避免散射/连发按每颗弹体重复播放。
        projectile.Launch(context);
    }

    private int ResolveProjectilePierceCount()
    {
        return propertiesManager != null
            ? PropValueUtility.FloatPointsToNonNegativeFlooredInt(
                propertiesManager.GetPropValue(PropType.ProjectilePierceCount))
            : 0;
    }

    private void PlaySequenceSfx(int eventKey)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceSfx(eventKey, out WeaponSequenceSfxDefinition definition))
        {
            return;
        }

        if (definition.SfxKey != AudioSfxKey.None)
        {
            LogSequenceSfxDebug(eventKey, definition.SfxKey);
            AudioSfxBridge.RequestPlay(definition.SfxKey);
        }
    }

    private void LogSequenceSfxDebug(int eventKey, AudioSfxKey sfxKey)
    {
        if (!logSequenceSfxDebug)
        {
            return;
        }

        float configuredNormalizedTime = ResolveSequenceEventNormalizedTime(WeaponSequenceEventType.PlaySfx, eventKey);
        float expectedSeconds = configuredNormalizedTime >= 0f
            ? configuredNormalizedTime * currentAttackSequenceDuration
            : -1f;
        float elapsedSeconds = Mathf.Max(0f, Time.time - currentAttackStartedAt);
        string configuredTimeText = configuredNormalizedTime >= 0f
            ? $"{configuredNormalizedTime:0.###} ({expectedSeconds:0.###}s)"
            : "not-found";

        Debug.Log(
            $"[WeaponSfxDebug] weapon='{name}', data='{(WeaponData != null ? WeaponData.name : "null")}', " +
            $"sequence='{(attackSequence != null ? attackSequence.name : "null")}', eventKey={eventKey}, " +
            $"sfx={sfxKey}, configuredTime={configuredTimeText}, elapsed={elapsedSeconds:0.###}s, " +
            $"sequenceDuration={currentAttackSequenceDuration:0.###}s, frame={Time.frameCount}, " +
            $"isMeleeHitBox={WeaponData != null && WeaponData.EnableHitBox}, activeHitWindows={activeHitWindows.Count}.",
            this);
    }

    private float ResolveSequenceEventNormalizedTime(WeaponSequenceEventType eventType, int eventKey)
    {
        if (attackSequence == null || attackSequence.EventKeyframes == null)
        {
            return -1f;
        }

        IReadOnlyList<WeaponSequenceEventKeyframe> events = attackSequence.EventKeyframes;
        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe keyframe = events[i];
            if (keyframe.eventType == eventType && keyframe.eventKey == eventKey)
            {
                return keyframe.normalizedTime;
            }
        }

        return -1f;
    }

    private void PlaySequenceVfx(int eventKey)
    {
        if (WeaponData == null || !WeaponData.TryGetSequenceVfx(eventKey, out WeaponSequenceVfxDefinition definition))
        {
            return;
        }

        if (definition.VfxPrefab == null)
        {
            return;
        }

        WeaponSpawnPointPose spawnAnchor = ResolveSpawnPointPose(definition.SpawnPointIndex);
        Vector3 spawnPosition = spawnAnchor.Position + spawnAnchor.Rotation * definition.LocalOffset;
        Quaternion spawnRotation = spawnAnchor.Rotation * Quaternion.Euler(definition.LocalEulerAngles);
        RuntimeVfx.Spawn(definition.VfxPrefab, spawnPosition, spawnRotation, null);
    }

    private void SpawnHitVfx(Vector2 hitPoint)
    {
        if (WeaponData == null || !WeaponData.EnableHitBox || WeaponData.HitVfxPrefab == null)
        {
            return;
        }

        Quaternion spawnRotation = ResolveHitBoxAnchorPose().Rotation;
        RuntimeVfx.Spawn(WeaponData.HitVfxPrefab, hitPoint, spawnRotation, null);
    }

    private WeaponSpawnPointPose ResolveSpawnPointPose(int spawnPointIndex)
    {
        if (WeaponData != null && WeaponData.TryGetSpawnPointPose(spawnPointIndex, transform, out WeaponSpawnPointPose configuredPose))
        {
            return configuredPose;
        }

        return ResolveRootSpawnPointPose();
    }

    private WeaponSpawnPointPose ResolveRootSpawnPointPose()
    {
        return new WeaponSpawnPointPose(transform.position, transform.rotation);
    }

    private void FinishAttackSequence()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        hitBoxDebugSamples.Clear();
        pendingTargetPosition = Vector2.zero;
        lockedAttackTarget = null;
        projectilePatternEmitter.ResetBurstState();
        if (WeaponData != null && WeaponData.AttackTimingMode == WeaponAttackTimingMode.FixedSequenceThenCooldown)
        {
            cooldownRemaining = AttackInterval;
            cooldownStartedFrame = Time.frameCount;
        }

        CompleteAttackCycle();
    }

    private void ForceResetAttackState()
    {
        activeHitWindows.Clear();
        hitWindowTargets.Clear();
        hitWindowLastPoses.Clear();
        hitBoxDebugSamples.Clear();
        pendingTargetPosition = Vector2.zero;
        lockedAttackTarget = null;
        projectilePatternEmitter.ResetBurstState();
        cooldownRemaining = 0f;
        cooldownStartedFrame = -1;
        CompleteAttackCycle();
        sequenceBridge?.Stop(true);
        StopAllCoroutines();
    }

    protected virtual void RecalculateRuntimeStats()
    {
        if (propertiesManager == null)
        {
            throw new MissingComponentException(
                $"{nameof(propertiesManager)} is null on {name}. Cannot recalculate runtime stats. " +
                $"Ensure the weapon is a child of an entity with a {nameof(PropertiesManager)} component.");
        }

        float previousAttackInterval = AttackInterval;
        WeaponStats stats = statsResolver.Resolve(new WeaponStatsRequest(
            WeaponData,
            Level,
            propertiesManager,
            Benefits));

        Damage = stats.Damage;
        AttackInterval = stats.AttackInterval;
        RefreshCooldownForAttackIntervalChange(previousAttackInterval);
        CriticalChance = stats.CriticalChance;
        CriticalMultiplier = stats.CriticalMultiplier;
        Range = stats.Range;
        KnockbackStrength = stats.KnockbackStrength;
    }

    private void RefreshCooldownForAttackIntervalChange(float previousAttackInterval)
    {
        if (cooldownRemaining <= 0f || previousAttackInterval <= 0.0001f)
        {
            return;
        }

        float cooldownProgress = Mathf.Clamp01(1f - cooldownRemaining / previousAttackInterval);
        cooldownRemaining = Mathf.Max(0f, AttackInterval * (1f - cooldownProgress));
    }

    private void ApplyLevelHolderModifiers()
    {
        RemoveLevelHolderModifiers();
        if (WeaponData == null || propertiesManager == null)
        {
            return;
        }

        WeaponLevelStatData weaponStats = WeaponData.GetLevelStats(Level);
        IReadOnlyList<PropModifierData> holderModifiers = weaponStats.HolderModifiers;
        if (holderModifiers == null || holderModifiers.Count == 0)
        {
            return;
        }

        activeHolderLevelModifierSourceId = BuildHolderLevelModifierSourceId();
        propertiesManager.AddModifiers(activeHolderLevelModifierSourceId, holderModifiers);
    }

    private void RemoveLevelHolderModifiers()
    {
        if (propertiesManager == null || string.IsNullOrWhiteSpace(activeHolderLevelModifierSourceId))
        {
            activeHolderLevelModifierSourceId = null;
            return;
        }

        propertiesManager.RemoveModifiers(activeHolderLevelModifierSourceId);
        activeHolderLevelModifierSourceId = null;
    }

    private string BuildHolderLevelModifierSourceId()
    {
        return $"{HOLDER_LEVEL_MODIFIER_SOURCE_PREFIX}{RuntimeId}";
    }

    private void ApplyCurrentConfiguration()
    {
        if (WeaponData == null)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponData)} is null on {name}. Cannot apply weapon configuration. " +
                $"Ensure {nameof(WeaponData)} is assigned before the weapon starts.");
        }

        ApplyDefaultConfiguration();
        OnConfiguredFromData();
    }

    private void ApplyDefaultConfiguration()
    {
        ApplyDataIcon();
        ApplyVisualForwardAngle();
        CacheSequenceDefaultPose();
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

    private void CacheSequenceDefaultPose()
    {
        if (sequenceBridge == null)
        {
            sequenceBridge = GetComponent<WeaponSequenceBridge>();
        }

        sequenceBridge?.CacheDefaultPose();
    }

    private bool ShouldHoldAimWhenAttackReady()
    {
        return WeaponData.HoldAimWhenAttackReady;
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Damage ||
            propType == PropType.MeleeAttack ||
            propType == PropType.RangedAttack ||
            propType == PropType.MagicAttack ||
            propType == PropType.SummonAttack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.CriticalChance ||
            propType == PropType.CriticalPercent ||
            propType == PropType.AttackRange ||
            propType == PropType.KnockbackStrength)
        {
            RefreshRuntimeStats();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawSpawnPointGizmos();
        DrawHitBoxGizmo();
        DrawProjectilePatternGizmos();
    }

    private void DrawSpawnPointGizmos()
    {
        if (WeaponData == null || WeaponData.SpawnPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < WeaponData.SpawnPoints.Count; i++)
        {
            if (!WeaponData.TryGetSpawnPointPose(i, transform, out WeaponSpawnPointPose origin))
            {
                continue;
            }

            Gizmos.DrawWireSphere(origin.Position, 0.06f);
            Gizmos.DrawRay(origin.Position, origin.Forward * 0.8f);
        }
    }

    private void DrawHitBoxGizmo()
    {
        if (!drawHitBoxDebugGizmos || WeaponData == null || !WeaponData.EnableHitBox)
        {
            return;
        }

        HitBoxDetectionPose previewPose = CaptureCurrentHitPose();
        DrawHitBoxAnchorGizmo();
        DrawHitBoxPoseGizmo(previewPose, HitBoxSize, activeHitWindows.Count > 0 ? HIT_BOX_ACTIVE_GIZMO_COLOR : HIT_BOX_IDLE_GIZMO_COLOR);
        DrawHitBoxDebugSamples();
    }

    private void DrawHitBoxAnchorGizmo()
    {
        WeaponSpawnPointPose anchorPose = ResolveHitBoxAnchorPose();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(anchorPose.Position, 0.05f);
        Gizmos.DrawRay(anchorPose.Position, anchorPose.Forward * 0.45f);
    }

    private void DrawHitBoxDebugSamples()
    {
        if (!Application.isPlaying || hitBoxDebugSamples.Count == 0)
        {
            return;
        }

        for (int i = 0; i < hitBoxDebugSamples.Count; i++)
        {
            HitBoxDebugSample sample = hitBoxDebugSamples[i];
            float age = Mathf.Max(0f, Time.time - sample.Time);
            float alpha = Mathf.Clamp01(1f - age / 0.35f) * HIT_BOX_SWEEP_GIZMO_COLOR.a;
            if (alpha <= 0.01f)
            {
                continue;
            }

            Color color = HIT_BOX_SWEEP_GIZMO_COLOR;
            color.a = alpha;
            DrawHitBoxPoseGizmo(sample.Pose, sample.Size, color);
        }
    }

    private static void DrawHitBoxPoseGizmo(in HitBoxDetectionPose pose, Vector2 size, Color color)
    {
        Gizmos.color = color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(pose.Position, Quaternion.Euler(0f, 0f, pose.RotationZ), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = previousMatrix;
    }

    private void DrawProjectilePatternGizmos()
    {
        AttackSequenceDefinitionSO sequence = DebugAttackSequence;
        if (WeaponData == null || sequence == null)
        {
            return;
        }

        var events = sequence.EventKeyframes;
        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe keyframe = events[i];
            if (keyframe.eventType != WeaponSequenceEventType.SpawnProjectile)
            {
                continue;
            }

            if (!WeaponData.TryGetSequenceProjectile(keyframe.eventKey, out WeaponSequenceProjectileDefinition projectileConfig))
            {
                continue;
            }

            WeaponSpawnPointPose origin = ResolveSpawnPointPose(projectileConfig.SpawnPointIndex);
            DrawProjectilePatternGizmo(origin, projectileConfig);
        }
    }

    private void DrawProjectilePatternGizmo(WeaponSpawnPointPose origin, WeaponSequenceProjectileDefinition projectileConfig)
    {
        Gizmos.color = projectileConfig.ProjectileDefinition != null ? projectileConfig.ProjectileDefinition.DebugColor : Color.cyan;

        switch (projectileConfig.FiringMode)
        {
            case ProjectileFiringMode.Spread:
                DrawSpreadPattern(origin, projectileConfig.PatternConfig.SpreadCount, projectileConfig.PatternConfig.SpreadAngle);
                break;
            case ProjectileFiringMode.Nova:
                DrawNovaPattern(origin, projectileConfig.PatternConfig.NovaCount);
                break;
            default:
                Gizmos.DrawRay(origin.Position, origin.Forward * 1.1f);
                break;
        }
    }

    private void DrawSpreadPattern(WeaponSpawnPointPose origin, int count, float halfAngle)
    {
        if (count <= 1)
        {
            Gizmos.DrawRay(origin.Position, origin.Forward * 1.1f);
            return;
        }

        float step = (halfAngle * 2f) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float angle = -halfAngle + (step * i);
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * origin.Forward;
            Gizmos.DrawRay(origin.Position, direction * 1.1f);
        }
    }

    private void DrawNovaPattern(WeaponSpawnPointPose origin, int count)
    {
        count = Mathf.Max(1, count);
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Gizmos.DrawRay(origin.Position, direction * 1f);
        }
    }
}
