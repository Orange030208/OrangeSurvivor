using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class FlyForestBrain : EnemyBrain
{
    [Header("攻击点位")]
    [SerializeField] private Transform shootPointTransform;

    private readonly EnemyActionRunner attackActionRunner = new();

    private EnemyAttackController attackController;
    private FlyForestEnemySO enemyData;
    private IMoveStrategy normalMovementStrategy;
    private IAttackStrategy normalAttackStrategy;
    private bool isAttackCompleting;
    private bool isIdleVisualActive;
    private bool isMoveVisualActive;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);
        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as FlyForestEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(FlyForestBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new MissingReferenceException($"{nameof(FlyForestBrain)} requires a {nameof(FlyForestEnemySO)} definition.");
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        ResetRuntimeState();
    }

    public override void StopBrain()
    {
        ResetRuntimeState();
        base.StopBrain();
    }

    public override void OnDisableComponent()
    {
        ResetRuntimeState();
    }

    protected override void OnBrainUpdate()
    {
        if (target == null)
        {
            StopMovementAndShowIdle();
            return;
        }

        if (attackActionRunner.IsRunning || attackActionRunner.IsComplete)
        {
            FaceTarget();
            TickAttackAction();
            return;
        }

        FaceTarget();
        EnsureMoveVisual();
        if (normalAttackStrategy.CanUse(target))
        {
            BeginAttack();
        }
    }

    protected override void OnBrainFixedUpdate()
    {
        if (target == null)
        {
            currentMovable.StopMoving();
            return;
        }

        if (attackActionRunner.IsRunning || attackActionRunner.IsComplete)
        {
            currentMovable.StopMoving();
            return;
        }

        normalMovementStrategy.ExecuteMove(target);
        FaceTarget();
    }

    private void BuildRuntimeStrategies()
    {
        normalMovementStrategy = new CircleKiteMoveStrategy(owner, currentMovable, propertiesManager, enemyData.normalMovement);
        normalAttackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            FlyForestEnemySO.NORMAL_ATTACK_ACTION_ID,
            enemyData.normalAttackSpeedBenefitRatio,
            shootPointTransform,
            enemyData.normalAttackProjectileDefinition);
    }

    private void BeginAttack()
    {
        currentMovable.StopMoving();
        attackActionRunner.Begin(enemyData.NormalAttackAction, currentAnimatable);
        isAttackCompleting = false;
        isIdleVisualActive = false;
        isMoveVisualActive = false;
    }

    private void TickAttackAction()
    {
        currentMovable.StopMoving();
        attackActionRunner.Tick(Time.deltaTime);

        if (attackActionRunner.ShouldCommit)
        {
            attackActionRunner.MarkCommitted();
            normalAttackStrategy.TryExecuteCommitted(target);
        }

        if (!attackActionRunner.IsComplete || isAttackCompleting)
        {
            return;
        }

        isAttackCompleting = true;
        attackActionRunner.Cancel();
        EnsureMoveVisual();
    }

    private void StopMovementAndShowIdle()
    {
        currentMovable.StopMoving();
        attackActionRunner.Cancel();
        isAttackCompleting = false;
        if (isIdleVisualActive)
        {
            return;
        }

        currentAnimatable.PlayState(enemyData.AnimConfig.IdleHash);
        isIdleVisualActive = true;
        isMoveVisualActive = false;
    }

    private void EnsureMoveVisual()
    {
        if (isMoveVisualActive)
        {
            return;
        }

        currentAnimatable.PlayState(enemyData.AnimConfig.MoveHash);
        isMoveVisualActive = true;
        isIdleVisualActive = false;
    }

    private void ResetRuntimeState()
    {
        currentMovable?.StopMoving();
        attackActionRunner.Cancel();
        isAttackCompleting = false;
        isIdleVisualActive = false;
        isMoveVisualActive = false;
    }
}
