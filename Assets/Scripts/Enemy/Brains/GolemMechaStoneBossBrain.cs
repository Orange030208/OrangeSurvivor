using BehaviorDesigner.Runtime;
using UnityEngine;

[RequireComponent(typeof(BehaviorTree))]
[RequireComponent(typeof(EnemyAttackController))]
public sealed class GolemMechaStoneBossBrain : EnemyBrain
{
    private const string DEFAULT_LASER_ORIGIN_NAME = "LaserOrigin";
    private const string OWNER_VARIABLE = "Owner";
    private const string TARGET_VARIABLE = "Target";
    private const string BOSS_DATA_VARIABLE = "BossData";

    private BehaviorTree behaviorTree;
    private EnemyAttackController attackController;
    private GolemMechaStoneBossSO bossData;
    [Header("攻击点位")]
    [SerializeField] private Transform meleePointTransform;
    [SerializeField] private Transform shootPointTransform;
    [SerializeField] private Transform laserOriginTransform;
    [SerializeField, Min(1)] private int initialPhase = 1;
    private IMoveStrategy chaseMovementStrategy;
    private IRangeDetectionStrategy laserDetectionStrategy;
    private IRangeDetectionStrategy meleeDetectionStrategy;
    private IRangeDetectionStrategy shootDetectionStrategy;
    private IAttackStrategy meleeAttackStrategy;
    private IAttackStrategy shootAttackStrategy;
    private int currentPhase;
    private int runningActionCount;

    public IMoveStrategy ChaseMovementStrategy => chaseMovementStrategy;
    public IRangeDetectionStrategy LaserDetectionStrategy => laserDetectionStrategy;
    public IRangeDetectionStrategy MeleeDetectionStrategy => meleeDetectionStrategy;
    public IRangeDetectionStrategy ShootDetectionStrategy => shootDetectionStrategy;
    public IAttackStrategy MeleeAttackStrategy => meleeAttackStrategy;
    public IAttackStrategy ShootAttackStrategy => shootAttackStrategy;
    public Transform LaserOriginTransform => laserOriginTransform != null ? laserOriginTransform : owner != null ? owner.transform : null;
    public int CurrentPhase => currentPhase;
    public bool IsActionRunning => runningActionCount > 0;
    public bool CanUseLaser => bossData != null && currentPhase >= bossData.LaserMinPhase;
    public bool CanUseShield => bossData != null && currentPhase >= bossData.ShieldMinPhase;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);

        behaviorTree = this.owner.GetComponent<BehaviorTree>();
        attackController = this.owner.GetComponent<EnemyAttackController>();
        bossData = this.owner.EnemyData as GolemMechaStoneBossSO;

        if (behaviorTree == null)
        {
            throw new MissingComponentException($"{nameof(GolemMechaStoneBossBrain)} requires {nameof(BehaviorTree)}.");
        }

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(GolemMechaStoneBossBrain)} requires {nameof(EnemyAttackController)}.");
        }

        if (bossData == null)
        {
            throw new MissingReferenceException($"{nameof(GolemMechaStoneBossBrain)} requires {nameof(GolemMechaStoneBossSO)}.");
        }

        if (bossData.BossAnimConfig == null)
        {
            throw new MissingReferenceException(
                $"{nameof(GolemMechaStoneBossBrain)} requires {nameof(GolemMechaStoneBossAnimationConfig)} on {nameof(GolemMechaStoneBossSO)}.{nameof(GolemMechaStoneBossSO.AnimConfig)}.");
        }

        if (bossData.ShootProjectileDefinition == null)
        {
            throw new MissingReferenceException(
                $"{nameof(GolemMechaStoneBossBrain)} requires {nameof(GolemMechaStoneBossSO)}.{nameof(GolemMechaStoneBossSO.ShootProjectileDefinition)}.");
        }

        ResetPhase();

        BuildStrategies();
        BindSharedVariables();
    }

    protected override void OnBrainStart()
    {
        BindSharedVariables();
        behaviorTree.EnableBehavior();
    }

    protected override void OnBrainUpdate()
    {
        BindSharedVariables();
    }

    public override void StartBrain()
    {
        base.StartBrain();
        ResetPhase();
        BindSharedVariables();
        behaviorTree?.EnableBehavior();
    }

    public override void StopBrain()
    {
        behaviorTree?.DisableBehavior(false);
        currentMovable?.StopMoving();
        ClearActionLocks();
        base.StopBrain();
    }

    public override void OnDisableComponent()
    {
        behaviorTree?.DisableBehavior(false);
        currentMovable?.StopMoving();
        ClearActionLocks();
    }

    public override void SetTarget(Entity newTarget)
    {
        base.SetTarget(newTarget);
        behaviorTree?.SetVariableValue(TARGET_VARIABLE, newTarget != null ? newTarget.gameObject : null);
    }

    public bool ShouldEnterNextPhase()
    {
        return ResolveNextPhase() > currentPhase;
    }

    public int ResolveNextPhase()
    {
        if (bossData == null)
        {
            return currentPhase;
        }

        float healthRatio = ResolveHealthRatio();
        if (currentPhase < 2 && healthRatio <= bossData.PhaseTwoHealthRatio)
        {
            return 2;
        }

        if (currentPhase < 3 && healthRatio <= bossData.PhaseThreeHealthRatio)
        {
            return 3;
        }

        return currentPhase;
    }

    public void CommitPhase(int phase)
    {
        currentPhase = Mathf.Max(initialPhase, phase);
    }

    public void BeginAction()
    {
        runningActionCount++;
    }

    public void EndAction()
    {
        runningActionCount = Mathf.Max(0, runningActionCount - 1);
    }

    private void BuildStrategies()
    {
        chaseMovementStrategy = new DirectChaseMoveStrategy(currentMovable);
        laserDetectionStrategy = new DistanceRangeDetectionStrategy(
            this.owner,
            propertiesManager,
            bossData.LaserRangeMultiplier);
        meleeDetectionStrategy = new DistanceRangeDetectionStrategy(
            this.owner,
            propertiesManager,
            bossData.MeleeRangeMultiplier);
        shootDetectionStrategy = new DistanceRangeDetectionStrategy(
            this.owner,
            propertiesManager,
            bossData.ShootRangeMultiplier);
        meleeAttackStrategy = new DirectDamageAttackStrategy(
            this.owner,
            attackController,
            propertiesManager,
            GolemMechaStoneBossSO.MELEE_ACTION_ID,
            bossData.MeleeAttackSpeedBenefitRatio,
            meleeDetectionStrategy,
            meleePointTransform,
            bossData.MeleeRangeMultiplier,
            bossData.MeleeHitVfxPrefab);
        shootAttackStrategy = new ProjectileAttackStrategy(
            this.owner,
            attackController,
            propertiesManager,
            GolemMechaStoneBossSO.SHOOT_ACTION_ID,
            bossData.ShootAttackSpeedBenefitRatio,
            shootDetectionStrategy,
            shootPointTransform,
            bossData.ShootProjectileDefinition,
            bossData.ShootRangeMultiplier);
    }

    private void BindSharedVariables()
    {
        if (behaviorTree == null)
        {
            return;
        }

        behaviorTree.SetVariableValue(OWNER_VARIABLE, owner != null ? owner.gameObject : null);
        behaviorTree.SetVariableValue(TARGET_VARIABLE, target != null ? target.gameObject : null);
        behaviorTree.SetVariableValue(BOSS_DATA_VARIABLE, bossData);
    }

    private void ResetPhase()
    {
        currentPhase = Mathf.Max(1, initialPhase);
    }

    private void ClearActionLocks()
    {
        runningActionCount = 0;
    }

    private float ResolveHealthRatio()
    {
        if (healthComponent == null || healthComponent.MaxHealth <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01(healthComponent.CurrentHealth / healthComponent.MaxHealth);
    }
}
