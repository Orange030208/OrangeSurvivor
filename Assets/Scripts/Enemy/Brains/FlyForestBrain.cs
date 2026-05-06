using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class FlyForestBrain : EnemyBrain
{
    public enum FlyForestAIState
    {
        Idle,
        CircleKite,
        RetreatBurst
    }

    private readonly StateMachine<FlyForestAIState> stateMachine = new();

    private EnemyAttackController attackController;
    private FlyForestEnemySO enemyData;
    private IMoveStrategy currentMoveStrategy;
    private IMoveStrategy normalMovementStrategy;
    private IMoveStrategy retreatMovementStrategy;
    private IAttackStrategy normalAttackStrategy;

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
        RegisterStates();
        stateMachine.ChangeState(FlyForestAIState.CircleKite);
    }

    protected override void OnBrainUpdate()
    {
        stateMachine.Update();
    }

    protected override void OnBrainFixedUpdate()
    {
        stateMachine.FixedUpdate();
    }

    private void RegisterStates()
    {
        stateMachine.RegisterState(new IdleState(this));
        stateMachine.RegisterState(new CircleKiteState(this));
        stateMachine.RegisterState(new RetreatBurstState(this));
    }

    private void BuildRuntimeStrategies()
    {
        normalMovementStrategy = new CircleKiteMoveStrategy(owner, currentMovable, propertiesManager, enemyData.normalMovement);
        retreatMovementStrategy = new RetreatMoveStrategy(owner, currentMovable, enemyData.retreatMovement);
        IRangeDetectionStrategy detectionStrategy = new DistanceRangeDetectionStrategy(
            owner,
            propertiesManager,
            enemyData.normalAttackConfig.detection);
        normalAttackStrategy = new ProjectileAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            enemyData.normalAttackConfig.timing,
            detectionStrategy,
            enemyData.normalAttackConfig.projectileDefinition);
    }

    private void SetMoveStrategy(IMoveStrategy strategy)
    {
        currentMoveStrategy = strategy;
    }

    private bool IsLowHealth()
    {
        return healthComponent.CurrentHealth / healthComponent.MaxHealth * 100f <= enemyData.lowHpPercent;
    }

    private sealed class IdleState : StateBase<FlyForestAIState>
    {
        private readonly FlyForestBrain brain;

        public IdleState(FlyForestBrain brain) : base(FlyForestAIState.Idle)
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

            if (brain.target != null)
            {
                brain.stateMachine.ChangeState(FlyForestAIState.CircleKite);
            }
        }
    }

    private sealed class CircleKiteState : StateBase<FlyForestAIState>
    {
        private readonly FlyForestBrain brain;

        public CircleKiteState(FlyForestBrain brain) : base(FlyForestAIState.CircleKite)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.normalMovementStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
            brain.FaceTarget();

            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(FlyForestAIState.Idle);
                return;
            }

            if (brain.IsLowHealth())
            {
                brain.stateMachine.ChangeState(FlyForestAIState.RetreatBurst);
            }
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.currentMoveStrategy.ExecuteMove(brain.target);
            brain.FaceTarget();
            brain.normalAttackStrategy.TryExecute(brain.target);
        }
    }

    private sealed class RetreatBurstState : StateBase<FlyForestAIState>
    {
        private const string RETREAT_BURST_MODIFIER_SOURCE = "MageBrain_RetreatBurst";
        private readonly FlyForestBrain brain;

        public RetreatBurstState(FlyForestBrain brain) : base(FlyForestAIState.RetreatBurst)
        {
            this.brain = brain;
        }

        public override void OnEnter()
        {
            brain.SetMoveStrategy(brain.retreatMovementStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
            brain.propertiesManager.AddModifiers(RETREAT_BURST_MODIFIER_SOURCE,brain.enemyData.fastBurstModifierData);
        }

        public override void OnUpdate()
        {
            if (brain.target == null)
            {
                brain.stateMachine.ChangeState(FlyForestAIState.Idle);
                return;
            }

            if (!brain.IsLowHealth())
            {
                brain.stateMachine.ChangeState(FlyForestAIState.CircleKite);
            }

            brain.FaceMoveDirection();
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.currentMoveStrategy.ExecuteMove(brain.target);
            brain.FaceMoveDirection();
            brain.normalAttackStrategy.TryExecute(brain.target);
        }

        public override void OnExit()
        {
            brain.propertiesManager.RemoveModifiers(RETREAT_BURST_MODIFIER_SOURCE);
        }
    }
}
