using BehaviorDesigner.Runtime;
using UnityEngine;

[RequireComponent(typeof(BehaviorTree))]
[RequireComponent(typeof(EnemyAttackController))]
public sealed class GolemMechaStoneBossBrain : EnemyBrain
{
    private const string OWNER_VARIABLE = "Owner";
    private const string TARGET_VARIABLE = "Target";
    private const string BOSS_DATA_VARIABLE = "BossData";
    private const string CURRENT_PHASE_VARIABLE = "CurrentPhase";

    private BehaviorTree behaviorTree;
    private EnemyAttackController attackController;
    private GolemMechaStoneBossSO bossData;
    private IGolemMechaStoneBossMovementStrategy chaseMovementStrategy;
    private IGolemMechaStoneBossDetectionStrategy meleeDetectionStrategy;
    private IGolemMechaStoneBossDetectionStrategy shootDetectionStrategy;
    private IGolemMechaStoneBossAttackStrategy meleeAttackStrategy;
    private IGolemMechaStoneBossAttackStrategy shootAttackStrategy;

    public IGolemMechaStoneBossMovementStrategy ChaseMovementStrategy => chaseMovementStrategy;
    public IGolemMechaStoneBossDetectionStrategy MeleeDetectionStrategy => meleeDetectionStrategy;
    public IGolemMechaStoneBossDetectionStrategy ShootDetectionStrategy => shootDetectionStrategy;
    public IGolemMechaStoneBossAttackStrategy MeleeAttackStrategy => meleeAttackStrategy;
    public IGolemMechaStoneBossAttackStrategy ShootAttackStrategy => shootAttackStrategy;

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

        BuildRuntimeStrategies();
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
        BindSharedVariables();
        behaviorTree?.EnableBehavior();
    }

    public override void StopBrain()
    {
        behaviorTree?.DisableBehavior(false);
        currentMovable?.StopMoving();
        base.StopBrain();
    }

    public override void OnDisableComponent()
    {
        behaviorTree?.DisableBehavior(false);
        currentMovable?.StopMoving();
    }

    public override void SetTarget(Entity newTarget)
    {
        base.SetTarget(newTarget);
        behaviorTree?.SetVariableValue(TARGET_VARIABLE, newTarget != null ? newTarget.gameObject : null);
    }

    private void BuildRuntimeStrategies()
    {
        chaseMovementStrategy = GolemMechaStoneBossStrategyFactory.CreateChaseStrategy(this.owner, currentMovable, bossData);
        meleeDetectionStrategy = GolemMechaStoneBossStrategyFactory.CreateMeleeDetectionStrategy(this.owner, propertiesManager, bossData);
        shootDetectionStrategy = GolemMechaStoneBossStrategyFactory.CreateShootDetectionStrategy(this.owner, propertiesManager, bossData);
        meleeAttackStrategy = GolemMechaStoneBossStrategyFactory.CreateMeleeAttackStrategy(
            this.owner,
            attackController,
            propertiesManager,
            bossData,
            meleeDetectionStrategy);
        shootAttackStrategy = GolemMechaStoneBossStrategyFactory.CreateShootAttackStrategy(
            this.owner,
            attackController,
            propertiesManager,
            bossData,
            shootDetectionStrategy);
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

        object phaseValue = behaviorTree.GetVariable(CURRENT_PHASE_VARIABLE)?.GetValue();
        if (phaseValue == null || (phaseValue is int currentPhase && currentPhase <= 0))
        {
            behaviorTree.SetVariableValue(CURRENT_PHASE_VARIABLE, 1);
        }
    }
}
