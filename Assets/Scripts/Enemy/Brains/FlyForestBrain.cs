using UnityEngine;

[RequireComponent(typeof(RangeAttackComponent))]
public class FlyForestBrain : EnemyBrain
{
    public enum FlyForestAIState
    {
        Idle,
        CircleKite,
        RetreatBurst
    }

    private readonly StateMachine<FlyForestAIState> stateMachine = new();

    private IAttackable rangeAttack;
    private FlyForestEnemySO enemyData;
    private MovementStrategyBase currentMoveStrategy;
    private AttackStrategyBase currentAttackStrategy;

    protected override void OnInitialize(Entity owner)
    {
        base.OnInitialize(owner);
        rangeAttack = owner.GetComponent<RangeAttackComponent>();
        enemyData = this.owner.EnemyData as FlyForestEnemySO;
    }

    protected override void OnBrainStart()
    {
        RegisterStates();
        SetMoveStrategy(enemyData.normalMovementStrategy);
        SetAttackStrategy(enemyData.normalAttackStrategy);
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

    private void SetMoveStrategy(MovementStrategyBase strategy)
    {
        currentMoveStrategy = strategy;
    }

    private void SetAttackStrategy(AttackStrategyBase strategy)
    {
        currentAttackStrategy = strategy;
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
            brain.SetMoveStrategy(brain.enemyData.normalMovementStrategy);
            brain.SetAttackStrategy(brain.enemyData.normalAttackStrategy);
            brain.currentAnimatable.PlayState(brain.enemyData.AnimConfig.MoveHash);
        }

        public override void OnUpdate()
        {
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

            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            if (brain.rangeAttack.IsInAttackRange(brain.target))
            {
                brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
            }
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
            brain.SetMoveStrategy(brain.enemyData.retreatMovementStrategy);
            brain.SetAttackStrategy(brain.enemyData.normalAttackStrategy);
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
        }

        public override void OnFixedUpdate()
        {
            if (brain.target == null)
            {
                return;
            }

            brain.currentMoveStrategy.ExecuteMove(brain.currentMovable, brain.owner, brain.target, brain.enemyData);
            brain.currentAttackStrategy.ExecuteAttack(brain.rangeAttack, brain.owner, brain.target);
        }

        public override void OnExit()
        {
            brain.propertiesManager.RemoveModifiers(RETREAT_BURST_MODIFIER_SOURCE);
        }
    }
}
