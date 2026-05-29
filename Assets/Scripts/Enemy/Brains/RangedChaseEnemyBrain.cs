using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class RangedChaseEnemyBrain : EnemyBrain
{
    private enum RangedChaseAIState
    {
        Idle,
        Chase,
        Attack
    }

    private readonly StateMachine<RangedChaseAIState> stateMachine = new();

    [Header("攻击点位")]
    [SerializeField] private Transform shootPointTransform;

    private readonly EnemyActionRunner attackActionRunner = new();

    private EnemyAttackController attackController;
    private FlyForestEnemySO enemyData;
    private IMoveStrategy chaseMoveStrategy;
    private IAttackStrategy attackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);
        attackController = owner.GetComponent<EnemyAttackController>();
        enemyData = this.owner.EnemyData as FlyForestEnemySO;

        if (attackController == null)
        {
            throw new MissingComponentException($"{nameof(RangedChaseEnemyBrain)} requires an {nameof(EnemyAttackController)}.");
        }

        if (enemyData == null)
        {
            throw new MissingReferenceException($"{nameof(RangedChaseEnemyBrain)} requires a {nameof(FlyForestEnemySO)} definition.");
        }
    }

    protected override void OnBrainStart()
    {
        BuildRuntimeStrategies();
        ResetRuntimeState();
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new ChaseState(this));
        stateMachine.RegisterState(new AttackState(this));
        stateMachine.ChangeState(RangedChaseAIState.Chase);
    }

    public override void StopBrain()
    {
        ResetRuntimeState();
        base.StopBrain();
    }

    public override void StartBrain()
    {
        bool shouldResetExistingState = HasBrainStarted;
        ResetRuntimeState();
        base.StartBrain();

        if (shouldResetExistingState && stateMachine.HasState)
        {
            stateMachine.ChangeState(RangedChaseAIState.Chase, true);
        }
    }

    public override void OnDisableComponent()
    {
        ResetRuntimeState();
    }

    protected override void OnBrainUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    private void BuildRuntimeStrategies()
    {
        chaseMoveStrategy = new DirectChaseMoveStrategy(currentMovable);
        attackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            enemyData.NormalAttackAction.ActionId,
            enemyData.normalAttackSpeedBenefitRatio,
            shootPointTransform,
            enemyData.normalAttackProjectileDefinition);
    }

    private void BeginAttack()
    {
        currentMovable.StopMoving();
        attackActionRunner.Begin(enemyData.NormalAttackAction, currentAnimatable);
    }

    private void TickAttackAction()
    {
        currentMovable.StopMoving();
        attackActionRunner.Tick(Time.deltaTime);

        if (attackActionRunner.ShouldCommit)
        {
            attackActionRunner.MarkCommitted();
            attackStrategy.TryExecuteCommitted(target);
        }
    }

    private void ResetRuntimeState()
    {
        currentMovable?.StopMoving();
        attackActionRunner.Cancel();
    }

    private sealed class IdleState : StateBase<RangedChaseAIState>
    {
        private readonly RangedChaseEnemyBrain brain;

        public IdleState(RangedChaseEnemyBrain brain) : base(RangedChaseAIState.Idle)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentMovable.StopMoving();
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.IdleHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                return;
            }

            if (!brain.attackStrategy.IsTargetInRange(brain.target))
            {
                brain.stateMachine.ChangeState(RangedChaseAIState.Chase);
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(RangedChaseAIState.Attack);
            }
        }
    }

    private sealed class ChaseState : StateBase<RangedChaseAIState>
    {
        private readonly RangedChaseEnemyBrain brain;

        public ChaseState(RangedChaseEnemyBrain brain) : base(RangedChaseAIState.Chase)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(RangedChaseAIState.Idle);
                return;
            }

            if (!brain.attackStrategy.IsTargetInRange(brain.target))
            {
                return;
            }

            if (brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.ChangeState(RangedChaseAIState.Attack);
                return;
            }

            brain.stateMachine.ChangeState(RangedChaseAIState.Idle);
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.chaseMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
        }
    }

    private sealed class AttackState : StateBase<RangedChaseAIState>
    {
        private readonly RangedChaseEnemyBrain brain;
        private bool attackCompleted;

        public AttackState(RangedChaseEnemyBrain brain) : base(RangedChaseAIState.Attack)
        {
            this.brain = brain;
        }

        public override bool CanExitTo(RangedChaseAIState nextState, StateChangeMode mode)
        {
            return mode == StateChangeMode.Force || attackCompleted;
        }

        public override void OnEnter()
        {
            attackCompleted = false;
            if (brain.target == null)
            {
                brain.stateMachine.RequestState(RangedChaseAIState.Idle, StateChangeMode.Force);
                return;
            }

            if (!brain.attackStrategy.CanUse(brain.target))
            {
                brain.stateMachine.RequestState(RangedChaseAIState.Chase, StateChangeMode.Force);
                return;
            }

            brain.currentMovable.StopMoving();
            brain.FaceTarget();
            brain.BeginAttack();
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();
            brain.TickAttackAction();

            if (brain.attackActionRunner.IsComplete && !attackCompleted)
            {
                attackCompleted = true;
                brain.stateMachine.RequestState(RangedChaseAIState.Idle);
            }
        }

        public override void OnFixedUpdate()
        {
            brain.currentMovable.StopMoving();
        }

        public override void OnExit()
        {
            attackCompleted = false;
            brain.attackActionRunner.Cancel();
        }
    }
}
